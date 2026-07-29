#include "starray_touch_bridge.h"

#include "imgui.h"
#include "imgui_internal.h"

#include <android/input.h>
#include <atomic>
#include <math.h>
#include <pthread.h>

namespace {

constexpr int kEventCapacity = 128;
constexpr int kRectCapacity = 16;
constexpr float kTouchScrollStartPx = 8.0f;
constexpr float kTouchScrollAxisBias = 1.08f;

struct ForwardedMotionEvent {
    int action;
    float x;
    float y;
    int tool_type;
    int button_state;
};

struct OverlayTouchRect {
    float x;
    float y;
    float width;
    float height;
};

pthread_mutex_t g_event_lock = PTHREAD_MUTEX_INITIALIZER;
ForwardedMotionEvent g_events[kEventCapacity];
int g_event_head = 0;
int g_event_count = 0;
bool g_event_overflowed = false;

pthread_mutex_t g_rect_lock = PTHREAD_MUTEX_INITIALIZER;
OverlayTouchRect g_active_rects[kRectCapacity];
OverlayTouchRect g_pending_rects[kRectCapacity];
int g_active_rect_count = 0;
int g_pending_rect_count = 0;
bool g_overlay_gesture_active = false;

std::atomic<bool> g_overlay_visible{false};
std::atomic<bool> g_modal_active{false};
std::atomic<bool> g_modal_close_requested{false};
std::atomic<bool> g_focus_release_requested{false};

bool g_touch_down = false;
bool g_touch_scroll_active = false;
bool g_touch_suppress_up = false;
ImGuiWindow* g_touch_scroll_window = nullptr;
float g_touch_down_x = 0.0f;
float g_touch_down_y = 0.0f;
float g_touch_last_x = 0.0f;
float g_touch_last_y = 0.0f;

void clear_event_queue() {
    pthread_mutex_lock(&g_event_lock);
    g_event_head = 0;
    g_event_count = 0;
    g_event_overflowed = false;
    pthread_mutex_unlock(&g_event_lock);
}

bool point_in_active_rect_locked(float x, float y) {
    for (int i = 0; i < g_active_rect_count; ++i) {
        const OverlayTouchRect& rect = g_active_rects[i];
        if (x >= rect.x && y >= rect.y &&
            x <= rect.x + rect.width && y <= rect.y + rect.height) {
            return true;
        }
    }
    return false;
}

bool overlay_should_consume(int action, float x, float y) {
    pthread_mutex_lock(&g_rect_lock);
    if (!g_overlay_visible.load(std::memory_order_acquire)) {
        g_overlay_gesture_active = false;
        pthread_mutex_unlock(&g_rect_lock);
        return false;
    }

    const bool inside = point_in_active_rect_locked(x, y);
    bool consume = inside || g_overlay_gesture_active;
    switch (action) {
        case AMOTION_EVENT_ACTION_DOWN:
        case AMOTION_EVENT_ACTION_POINTER_DOWN:
            if (inside) {
                g_overlay_gesture_active = true;
                consume = true;
            }
            break;
        case AMOTION_EVENT_ACTION_UP:
        case AMOTION_EVENT_ACTION_POINTER_UP:
        case AMOTION_EVENT_ACTION_CANCEL:
            consume = consume || inside;
            g_overlay_gesture_active = false;
            break;
        default:
            break;
    }

    pthread_mutex_unlock(&g_rect_lock);
    return consume;
}

void enqueue_event(const ForwardedMotionEvent& event) {
    pthread_mutex_lock(&g_event_lock);
    if (g_event_count == kEventCapacity) {
        g_event_head = (g_event_head + 1) % kEventCapacity;
        --g_event_count;
        g_event_overflowed = true;
    }

    const int index = (g_event_head + g_event_count) % kEventCapacity;
    g_events[index] = event;
    ++g_event_count;
    pthread_mutex_unlock(&g_event_lock);
}

void add_mouse_source(ImGuiIO& io, int tool_type) {
    switch (tool_type) {
        case AMOTION_EVENT_TOOL_TYPE_MOUSE:
            io.AddMouseSourceEvent(ImGuiMouseSource_Mouse);
            break;
        case AMOTION_EVENT_TOOL_TYPE_STYLUS:
        case AMOTION_EVENT_TOOL_TYPE_ERASER:
            io.AddMouseSourceEvent(ImGuiMouseSource_Pen);
            break;
        case AMOTION_EVENT_TOOL_TYPE_FINGER:
        default:
            io.AddMouseSourceEvent(ImGuiMouseSource_TouchScreen);
            break;
    }
}

bool is_touch_tool(int tool_type) {
    return tool_type == AMOTION_EVENT_TOOL_TYPE_FINGER ||
           tool_type == AMOTION_EVENT_TOOL_TYPE_UNKNOWN;
}

ImGuiWindow* find_scroll_window_at(float x, float y) {
    if (GImGui == nullptr) {
        return nullptr;
    }

    ImGuiWindow* hovered = nullptr;
    ImGuiWindow* hovered_under_moving = nullptr;
    const ImVec2 position(x, y);
    ImGui::FindHoveredWindowEx(position, true, &hovered, &hovered_under_moving);

    for (ImGuiWindow* window = hovered; window != nullptr; window = window->ParentWindow) {
        if (window->ScrollMax.y <= 0.0f || window->Collapsed || window->SkipItems) {
            continue;
        }
        if ((window->Flags & ImGuiWindowFlags_NoScrollWithMouse) != 0) {
            continue;
        }
        if (window->Rect().Contains(position)) {
            return window;
        }
    }
    return nullptr;
}

void apply_touch_scroll_y(ImGuiWindow* window, float delta_y) {
    if (window == nullptr || delta_y == 0.0f || window->ScrollMax.y <= 0.0f) {
        return;
    }

    float next_y = window->Scroll.y - delta_y;
    if (next_y < 0.0f) {
        next_y = 0.0f;
    } else if (next_y > window->ScrollMax.y) {
        next_y = window->ScrollMax.y;
    }
    window->Scroll.y = next_y;
    ImGui::SetScrollY(window, next_y);
}

void begin_touch(ImGuiIO& io, const ForwardedMotionEvent& event) {
    g_touch_down = is_touch_tool(event.tool_type);
    g_touch_scroll_active = false;
    g_touch_suppress_up = false;
    g_touch_scroll_window = find_scroll_window_at(event.x, event.y);
    g_touch_down_x = event.x;
    g_touch_down_y = event.y;
    g_touch_last_x = event.x;
    g_touch_last_y = event.y;
    io.AddMousePosEvent(event.x, event.y);
    io.AddMouseButtonEvent(0, true);
}

void move_touch(ImGuiIO& io, const ForwardedMotionEvent& event) {
    io.AddMousePosEvent(event.x, event.y);
    if (!g_touch_down || !is_touch_tool(event.tool_type)) {
        g_touch_last_x = event.x;
        g_touch_last_y = event.y;
        return;
    }

    const float total_dx = event.x - g_touch_down_x;
    const float total_dy = event.y - g_touch_down_y;
    const float absolute_dx = fabsf(total_dx);
    const float absolute_dy = fabsf(total_dy);
    if (!g_touch_scroll_active &&
        absolute_dy >= kTouchScrollStartPx &&
        absolute_dy >= absolute_dx * kTouchScrollAxisBias) {
        g_touch_scroll_active = true;
        g_touch_suppress_up = true;
        io.AddMouseButtonEvent(0, false);
        ImGui::ClearActiveID();
    }

    if (g_touch_scroll_active) {
        apply_touch_scroll_y(g_touch_scroll_window, event.y - g_touch_last_y);
    }
    g_touch_last_x = event.x;
    g_touch_last_y = event.y;
}

void end_touch(ImGuiIO& io, const ForwardedMotionEvent& event) {
    io.AddMousePosEvent(event.x, event.y);
    if (!g_touch_suppress_up) {
        io.AddMouseButtonEvent(0, false);
    }
    g_touch_down = false;
    g_touch_scroll_active = false;
    g_touch_suppress_up = false;
    g_touch_scroll_window = nullptr;
}

void cancel_touch(ImGuiIO& io) {
    io.AddMouseButtonEvent(0, false);
    g_touch_down = false;
    g_touch_scroll_active = false;
    g_touch_suppress_up = false;
    g_touch_scroll_window = nullptr;
}

}  // namespace

extern "C" int modmanager_touch_forward_motion_event(
    int action,
    float x,
    float y,
    int tool_type,
    int button_state) {
    if (g_modal_active.load(std::memory_order_acquire) ||
        !g_overlay_visible.load(std::memory_order_acquire)) {
        return 0;
    }

    const bool consume = overlay_should_consume(action, x, y);
    enqueue_event({action, x, y, tool_type, button_state});
    return consume ? 1 : 0;
}

extern "C" int modmanager_imgui_drain_forwarded_motion_events(void) {
    if (ImGui::GetCurrentContext() == nullptr) {
        return 0;
    }

    ForwardedMotionEvent local_events[kEventCapacity];
    int local_count = 0;
    bool overflowed = false;
    pthread_mutex_lock(&g_event_lock);
    local_count = g_event_count;
    for (int i = 0; i < local_count; ++i) {
        local_events[i] = g_events[(g_event_head + i) % kEventCapacity];
    }
    overflowed = g_event_overflowed;
    g_event_head = 0;
    g_event_count = 0;
    g_event_overflowed = false;
    pthread_mutex_unlock(&g_event_lock);

    ImGuiIO& io = ImGui::GetIO();
    if (overflowed || g_focus_release_requested.exchange(false, std::memory_order_acq_rel)) {
        cancel_touch(io);
        ImGui::ClearActiveID();
    }

    for (int i = 0; i < local_count; ++i) {
        const ForwardedMotionEvent& event = local_events[i];
        add_mouse_source(io, event.tool_type);
        switch (event.action) {
            case AMOTION_EVENT_ACTION_DOWN:
            case AMOTION_EVENT_ACTION_POINTER_DOWN:
                begin_touch(io, event);
                break;
            case AMOTION_EVENT_ACTION_MOVE:
            case AMOTION_EVENT_ACTION_HOVER_MOVE:
                move_touch(io, event);
                break;
            case AMOTION_EVENT_ACTION_UP:
            case AMOTION_EVENT_ACTION_POINTER_UP:
                end_touch(io, event);
                break;
            case AMOTION_EVENT_ACTION_CANCEL:
                cancel_touch(io);
                break;
            case AMOTION_EVENT_ACTION_BUTTON_PRESS:
            case AMOTION_EVENT_ACTION_BUTTON_RELEASE:
                io.AddMouseButtonEvent(
                    0,
                    (event.button_state & AMOTION_EVENT_BUTTON_PRIMARY) != 0);
                io.AddMouseButtonEvent(
                    1,
                    (event.button_state & AMOTION_EVENT_BUTTON_SECONDARY) != 0);
                io.AddMouseButtonEvent(
                    2,
                    (event.button_state & AMOTION_EVENT_BUTTON_TERTIARY) != 0);
                break;
            default:
                break;
        }
    }
    return local_count;
}

extern "C" void modmanager_overlay_touch_begin_frame(void) {
    pthread_mutex_lock(&g_rect_lock);
    g_pending_rect_count = 0;
    pthread_mutex_unlock(&g_rect_lock);
}

extern "C" void modmanager_overlay_touch_add_rect(
    float x,
    float y,
    float width,
    float height) {
    if (width <= 0.0f || height <= 0.0f) {
        return;
    }

    pthread_mutex_lock(&g_rect_lock);
    if (g_pending_rect_count < kRectCapacity) {
        g_pending_rects[g_pending_rect_count++] = {x, y, width, height};
    }
    pthread_mutex_unlock(&g_rect_lock);
}

extern "C" void modmanager_overlay_touch_commit_frame(void) {
    pthread_mutex_lock(&g_rect_lock);
    g_active_rect_count = g_pending_rect_count;
    for (int i = 0; i < g_active_rect_count; ++i) {
        g_active_rects[i] = g_pending_rects[i];
    }
    pthread_mutex_unlock(&g_rect_lock);
}

extern "C" int modmanager_overlay_ui_is_visible(void) {
    return g_overlay_visible.load(std::memory_order_acquire) ? 1 : 0;
}

extern "C" void modmanager_overlay_ui_set_visible(int visible) {
    const bool current = visible != 0;
    g_overlay_visible.store(current, std::memory_order_release);
    if (!current) {
        pthread_mutex_lock(&g_rect_lock);
        g_overlay_gesture_active = false;
        g_active_rect_count = 0;
        g_pending_rect_count = 0;
        pthread_mutex_unlock(&g_rect_lock);
        clear_event_queue();
        g_focus_release_requested.store(true, std::memory_order_release);
    }
}

extern "C" void modmanager_overlay_input_request_focus_release(void) {
    g_focus_release_requested.store(true, std::memory_order_release);
}

extern "C" int modmanager_modal_input_is_active(void) {
    return g_modal_active.load(std::memory_order_acquire) ? 1 : 0;
}

extern "C" void modmanager_modal_input_set_active(int active) {
    const bool current = active != 0;
    g_modal_active.store(current, std::memory_order_release);
    g_modal_close_requested.store(false, std::memory_order_release);
    pthread_mutex_lock(&g_rect_lock);
    g_overlay_gesture_active = false;
    pthread_mutex_unlock(&g_rect_lock);
    clear_event_queue();
    if (current) {
        g_focus_release_requested.store(true, std::memory_order_release);
    }
}

extern "C" void modmanager_modal_input_request_close(void) {
    if (g_modal_active.load(std::memory_order_acquire)) {
        g_modal_close_requested.store(true, std::memory_order_release);
    }
}

extern "C" int modmanager_modal_input_take_close_request(void) {
    return g_modal_close_requested.exchange(false, std::memory_order_acq_rel) ? 1 : 0;
}
