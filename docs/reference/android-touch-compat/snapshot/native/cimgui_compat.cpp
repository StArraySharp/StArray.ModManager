#include "imgui.h"
#include "imgui_internal.h"
#include "cimgui.h"
#include "realtime_event_core.h"
#include <android/input.h>
#include <android/log.h>
#include <atomic>
#include <jni.h>
#include <math.h>
#include <pthread.h>

#define STARRAY_INPUT_LOG_TAG "StArrayInputGate"
#define STARRAY_INPUT_LOGI(...) \
    __android_log_print(ANDROID_LOG_INFO, STARRAY_INPUT_LOG_TAG, __VA_ARGS__)

struct ForwardedMotionEvent {
    int action;
    float x;
    float y;
    int tool_type;
    int button_state;
};

static pthread_mutex_t g_forwarded_motion_lock = PTHREAD_MUTEX_INITIALIZER;
static ForwardedMotionEvent g_forwarded_motion_events[128];
static int g_forwarded_motion_head = 0;
static int g_forwarded_motion_count = 0;
static bool g_touch_down = false;
static bool g_touch_scroll_active = false;
static bool g_touch_suppress_up = false;
static ImGuiWindow* g_touch_scroll_window = nullptr;
static float g_touch_down_x = 0.0f;
static float g_touch_down_y = 0.0f;
static float g_touch_last_x = 0.0f;
static float g_touch_last_y = 0.0f;

static constexpr float kTouchScrollStartPx = 8.0f;
static constexpr float kTouchScrollAxisBias = 1.08f;

struct OverlayTouchRect {
    float x;
    float y;
    float w;
    float h;
};

static pthread_mutex_t g_overlay_touch_lock = PTHREAD_MUTEX_INITIALIZER;
static OverlayTouchRect g_overlay_touch_active_rects[16];
static OverlayTouchRect g_overlay_touch_pending_rects[16];
static int g_overlay_touch_active_rect_count = 0;
static int g_overlay_touch_pending_rect_count = 0;
static bool g_overlay_touch_active = false;
static bool g_overlay_ui_visible = false;
static bool g_modal_input_active = false;
static bool g_modal_unity_event_system_blocked = false;
static bool g_modal_close_requested = false;
static std::atomic<bool> g_overlay_focus_release_requested{false};

extern "C" void modmanager_pccompat_observe_touch_input(
    int action,
    int pointer_id,
    int pointer_count,
    int64_t event_time_ms,
    float x,
    float y,
    int viewport_width,
    int viewport_height,
    int source,
    int device_id,
    int android_flags);

extern "C" void modmanager_pccompat_observe_key_input(
    int action,
    int key_code,
    int scan_code,
    int meta_state,
    int device_id,
    int repeat_count,
    int64_t event_time_ms,
    int source,
    int android_flags);

extern "C" void modmanager_pccompat_set_external_input_devices(uint32_t flags);

extern "C" int modmanager_modal_input_is_active(void);

extern "C" int modmanager_overlay_ui_is_visible(void) {
    pthread_mutex_lock(&g_overlay_touch_lock);
    const bool visible = g_overlay_ui_visible;
    pthread_mutex_unlock(&g_overlay_touch_lock);
    return visible ? 1 : 0;
}

extern "C" void modmanager_overlay_ui_set_visible(int visible) {
    pthread_mutex_lock(&g_overlay_touch_lock);
    const bool previous = g_overlay_ui_visible;
    g_overlay_ui_visible = visible != 0;
    if (!g_overlay_ui_visible) {
        g_overlay_touch_active = false;
        g_overlay_touch_active_rect_count = 0;
        g_overlay_touch_pending_rect_count = 0;
        g_overlay_focus_release_requested.store(true, std::memory_order_release);
    }
    const bool current = g_overlay_ui_visible;
    const int active_rects = g_overlay_touch_active_rect_count;
    pthread_mutex_unlock(&g_overlay_touch_lock);
    if (previous != current) {
        STARRAY_INPUT_LOGI("overlayVisible=%d activeRects=%d modalCapture=%d",
            current ? 1 : 0,
            active_rects,
            modmanager_modal_input_is_active());
    }
}

extern "C" void modmanager_overlay_input_request_focus_release(void) {
    g_overlay_focus_release_requested.store(true, std::memory_order_release);
}

extern "C" int modmanager_modal_input_is_active(void) {
    const bool active = __atomic_load_n(&g_modal_input_active, __ATOMIC_ACQUIRE);
    return active ? 1 : 0;
}

extern "C" int modmanager_modal_input_blocks_unity_event_system(void) {
    const bool active = __atomic_load_n(&g_modal_input_active, __ATOMIC_ACQUIRE);
    const bool blocked = __atomic_load_n(
        &g_modal_unity_event_system_blocked,
        __ATOMIC_ACQUIRE);
    return active && blocked ? 1 : 0;
}

extern "C" void modmanager_modal_input_set_unity_event_system_blocked(int blocked) {
    __atomic_store_n(
        &g_modal_unity_event_system_blocked,
        blocked != 0,
        __ATOMIC_RELEASE);
}

extern "C" void modmanager_modal_input_set_active(int active) {
    const bool previous = __atomic_load_n(&g_modal_input_active, __ATOMIC_ACQUIRE);
    const bool current = active != 0;
    __atomic_store_n(
        &g_modal_input_active,
        current,
        __ATOMIC_RELEASE);
    if (current && !previous)
        starray::realtime::cancel_touch_input();
    if (!current) {
        __atomic_store_n(
            &g_modal_unity_event_system_blocked,
            false,
            __ATOMIC_RELEASE);
    }
    __atomic_store_n(
        &g_modal_close_requested,
        false,
        __ATOMIC_RELEASE);

    pthread_mutex_lock(&g_overlay_touch_lock);
    g_overlay_touch_active = false;
    pthread_mutex_unlock(&g_overlay_touch_lock);

    pthread_mutex_lock(&g_forwarded_motion_lock);
    g_forwarded_motion_head = 0;
    g_forwarded_motion_count = 0;
    pthread_mutex_unlock(&g_forwarded_motion_lock);

    if (previous != current) {
        STARRAY_INPUT_LOGI("modalCapture=%d overlayVisible=%d eventSystemBlocked=%d",
            current ? 1 : 0,
            modmanager_overlay_ui_is_visible(),
            modmanager_modal_input_blocks_unity_event_system());
    }
}

extern "C" void modmanager_modal_input_request_close(void) {
    if (__atomic_load_n(&g_modal_input_active, __ATOMIC_ACQUIRE)) {
        __atomic_store_n(
            &g_modal_close_requested,
            true,
            __ATOMIC_RELEASE);
    }
}

extern "C" int modmanager_modal_input_take_close_request(void) {
    const bool requested = __atomic_exchange_n(
        &g_modal_close_requested,
        false,
        __ATOMIC_ACQ_REL);
    return requested ? 1 : 0;
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeSetOverlayVisible(
    JNIEnv* env,
    jclass clazz,
    jboolean visible) {
    (void)env;
    (void)clazz;
    modmanager_overlay_ui_set_visible(visible == JNI_TRUE ? 1 : 0);
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeSetModalInputCapture(
    JNIEnv* env,
    jclass clazz,
    jboolean active) {
    (void)env;
    (void)clazz;
    modmanager_modal_input_set_active(active == JNI_TRUE ? 1 : 0);
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeRequestModalClose(
    JNIEnv* env,
    jclass clazz) {
    (void)env;
    (void)clazz;
    modmanager_modal_input_request_close();
}

extern "C" JNIEXPORT jint JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeTakeModalCloseRequest(
    JNIEnv* env,
    jclass clazz) {
    (void)env;
    (void)clazz;
    return static_cast<jint>(modmanager_modal_input_take_close_request());
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeObserveGameplayMotionEvent(
    JNIEnv* env,
    jclass clazz,
    jint action,
    jint pointer_id,
    jint pointer_count,
    jlong event_time_ms,
    jfloat x,
    jfloat y,
    jint viewport_width,
    jint viewport_height,
    jint source,
    jint device_id,
    jint android_flags) {
    (void)env;
    (void)clazz;
    if (modmanager_modal_input_is_active() != 0)
        return;
    modmanager_pccompat_observe_touch_input(
        (int)action,
        (int)pointer_id,
        (int)pointer_count,
        (int64_t)event_time_ms,
        (float)x,
        (float)y,
        (int)viewport_width,
        (int)viewport_height,
        (int)source,
        (int)device_id,
        (int)android_flags);
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeObserveGameplayKeyEvent(
    JNIEnv* env,
    jclass clazz,
    jint action,
    jint key_code,
    jint scan_code,
    jint meta_state,
    jint device_id,
    jint repeat_count,
    jlong event_time_ms,
    jint source,
    jint android_flags) {
    (void)env;
    (void)clazz;
    modmanager_pccompat_observe_key_input(
        (int)action,
        (int)key_code,
        (int)scan_code,
        (int)meta_state,
        (int)device_id,
        (int)repeat_count,
        (int64_t)event_time_ms,
        (int)source,
        (int)android_flags);
}

extern "C" JNIEXPORT void JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeSetExternalInputDevices(
    JNIEnv* env,
    jclass clazz,
    jint flags) {
    (void)env;
    (void)clazz;
    modmanager_pccompat_set_external_input_devices(static_cast<uint32_t>(flags));
}

extern "C" void modmanager_overlay_touch_begin_frame(void) {
    pthread_mutex_lock(&g_overlay_touch_lock);
    g_overlay_touch_pending_rect_count = 0;
    pthread_mutex_unlock(&g_overlay_touch_lock);
}

extern "C" void modmanager_overlay_touch_add_rect(float x, float y, float width, float height) {
    if (width <= 0.0f || height <= 0.0f) {
        return;
    }

    pthread_mutex_lock(&g_overlay_touch_lock);
    if (g_overlay_touch_pending_rect_count <
            (int)(sizeof(g_overlay_touch_pending_rects) / sizeof(g_overlay_touch_pending_rects[0]))) {
        g_overlay_touch_pending_rects[g_overlay_touch_pending_rect_count++] = { x, y, width, height };
    }
    pthread_mutex_unlock(&g_overlay_touch_lock);
}

extern "C" void modmanager_overlay_touch_commit_frame(void) {
    pthread_mutex_lock(&g_overlay_touch_lock);
    g_overlay_touch_active_rect_count = g_overlay_touch_pending_rect_count;
    for (int i = 0; i < g_overlay_touch_active_rect_count; i++) {
        g_overlay_touch_active_rects[i] = g_overlay_touch_pending_rects[i];
    }
    pthread_mutex_unlock(&g_overlay_touch_lock);
}

static bool overlay_touch_contains_locked(float x, float y) {
    for (int i = 0; i < g_overlay_touch_active_rect_count; i++) {
        const OverlayTouchRect& rect = g_overlay_touch_active_rects[i];
        if (x >= rect.x && y >= rect.y && x <= rect.x + rect.w && y <= rect.y + rect.h) {
            return true;
        }
    }
    return false;
}

static bool overlay_touch_should_consume(int action, float x, float y) {
    pthread_mutex_lock(&g_overlay_touch_lock);
    if (!g_overlay_ui_visible) {
        g_overlay_touch_active = false;
        pthread_mutex_unlock(&g_overlay_touch_lock);
        return false;
    }

    const bool inside = overlay_touch_contains_locked(x, y);
    bool consume = inside || g_overlay_touch_active;

    switch (action) {
        case AMOTION_EVENT_ACTION_DOWN:
        case AMOTION_EVENT_ACTION_POINTER_DOWN:
            if (inside) {
                g_overlay_touch_active = true;
                consume = true;
            }
            break;
        case AMOTION_EVENT_ACTION_UP:
        case AMOTION_EVENT_ACTION_POINTER_UP:
        case AMOTION_EVENT_ACTION_CANCEL:
            consume = consume || inside;
            g_overlay_touch_active = false;
            break;
        default:
            break;
    }

    pthread_mutex_unlock(&g_overlay_touch_lock);
    return consume;
}

// ImGui.NET may bind the no-argument draw-list helpers to the unsuffixed
// cimgui names, while cimgui 1.91.6 exports the generated "_Nil" variants.
// Keep both names available from the single injected SO.
CIMGUI_API ImDrawList* igGetBackgroundDrawList(void) {
    return igGetBackgroundDrawList_Nil();
}

CIMGUI_API ImDrawList* igGetForegroundDrawList(void) {
    return igGetForegroundDrawList_Nil();
}

extern "C" JNIEXPORT jboolean JNICALL
Java_com_fizzd_connectedworlds_editorport_StArrayModManagerBootstrap_nativeForwardMotionEvent(
    JNIEnv* env,
    jclass clazz,
    jint action,
    jfloat x,
    jfloat y,
    jint tool_type,
    jint button_state) {
    (void)env;
    (void)clazz;

    // Background ModManager startup keeps the Java forwarding path ready,
    // but a hidden overlay must not enqueue stale events or add queue work to
    // normal gameplay input.
    if (modmanager_modal_input_is_active() != 0 ||
        modmanager_overlay_ui_is_visible() == 0)
        return JNI_FALSE;

    const bool consume = overlay_touch_should_consume((int)action, (float)x, (float)y);

    pthread_mutex_lock(&g_forwarded_motion_lock);
    if (g_forwarded_motion_count == (int)(sizeof(g_forwarded_motion_events) / sizeof(g_forwarded_motion_events[0]))) {
        g_forwarded_motion_head = (g_forwarded_motion_head + 1) %
            (int)(sizeof(g_forwarded_motion_events) / sizeof(g_forwarded_motion_events[0]));
        g_forwarded_motion_count--;
    }

    const int index = (g_forwarded_motion_head + g_forwarded_motion_count) %
        (int)(sizeof(g_forwarded_motion_events) / sizeof(g_forwarded_motion_events[0]));
    g_forwarded_motion_events[index] = {
        (int)action,
        (float)x,
        (float)y,
        (int)tool_type,
        (int)button_state
    };
    g_forwarded_motion_count++;
    pthread_mutex_unlock(&g_forwarded_motion_lock);
    return consume ? JNI_TRUE : JNI_FALSE;
}

static void add_forwarded_mouse_source(ImGuiIO& io, int tool_type) {
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

static bool is_touch_tool(int tool_type) {
    return tool_type == AMOTION_EVENT_TOOL_TYPE_FINGER ||
           tool_type == AMOTION_EVENT_TOOL_TYPE_UNKNOWN;
}

static ImGuiWindow* find_scroll_window_at(float x, float y) {
    if (GImGui == nullptr) {
        return nullptr;
    }

    ImGuiWindow* hovered = nullptr;
    ImGuiWindow* hovered_under_moving = nullptr;
    const ImVec2 pos(x, y);
    ImGui::FindHoveredWindowEx(pos, true, &hovered, &hovered_under_moving);

    for (ImGuiWindow* window = hovered; window != nullptr; window = window->ParentWindow) {
        if (window->ScrollMax.y <= 0.0f || window->Collapsed || window->SkipItems) {
            continue;
        }
        if ((window->Flags & ImGuiWindowFlags_NoScrollWithMouse) != 0) {
            continue;
        }
        if (!window->Rect().Contains(pos)) {
            continue;
        }
        return window;
    }
    return nullptr;
}

static void apply_touch_scroll_y(ImGuiWindow* window, float dy) {
    if (window == nullptr || dy == 0.0f || window->ScrollMax.y <= 0.0f) {
        return;
    }

    float next_y = window->Scroll.y - dy;
    if (next_y < 0.0f) {
        next_y = 0.0f;
    } else if (next_y > window->ScrollMax.y) {
        next_y = window->ScrollMax.y;
    }

    window->Scroll.y = next_y;
    ImGui::SetScrollY(window, next_y);
}

static void begin_forwarded_touch(ImGuiIO& io, const ForwardedMotionEvent& event) {
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

static void update_forwarded_touch_move(ImGuiIO& io, const ForwardedMotionEvent& event) {
    io.AddMousePosEvent(event.x, event.y);
    if (!g_touch_down || !is_touch_tool(event.tool_type)) {
        g_touch_last_x = event.x;
        g_touch_last_y = event.y;
        return;
    }

    const float total_dx = event.x - g_touch_down_x;
    const float total_dy = event.y - g_touch_down_y;
    const float abs_total_dx = fabsf(total_dx);
    const float abs_total_dy = fabsf(total_dy);

    if (!g_touch_scroll_active &&
        abs_total_dy >= kTouchScrollStartPx &&
        abs_total_dy >= abs_total_dx * kTouchScrollAxisBias) {
        g_touch_scroll_active = true;
        g_touch_suppress_up = true;
        io.AddMouseButtonEvent(0, false);
        ImGui::ClearActiveID();
    }

    if (g_touch_scroll_active) {
        const float dy = event.y - g_touch_last_y;
        apply_touch_scroll_y(g_touch_scroll_window, dy);
    }

    g_touch_last_x = event.x;
    g_touch_last_y = event.y;
}

static void end_forwarded_touch(ImGuiIO& io, const ForwardedMotionEvent& event) {
    io.AddMousePosEvent(event.x, event.y);
    if (!g_touch_suppress_up) {
        io.AddMouseButtonEvent(0, false);
    }

    g_touch_down = false;
    g_touch_scroll_active = false;
    g_touch_suppress_up = false;
    g_touch_scroll_window = nullptr;
}

static void cancel_forwarded_touch(ImGuiIO& io) {
    io.AddMouseButtonEvent(0, false);
    g_touch_down = false;
    g_touch_scroll_active = false;
    g_touch_suppress_up = false;
    g_touch_scroll_window = nullptr;
}

extern "C" int modmanager_imgui_drain_forwarded_motion_events(void) {
    if (ImGui::GetCurrentContext() == nullptr) {
        return 0;
    }

    ForwardedMotionEvent local_events[128];
    int local_count = 0;

    pthread_mutex_lock(&g_forwarded_motion_lock);
    local_count = g_forwarded_motion_count;
    for (int i = 0; i < local_count; i++) {
        local_events[i] = g_forwarded_motion_events[
            (g_forwarded_motion_head + i) %
            (int)(sizeof(g_forwarded_motion_events) / sizeof(g_forwarded_motion_events[0]))];
    }
    g_forwarded_motion_head = 0;
    g_forwarded_motion_count = 0;
    pthread_mutex_unlock(&g_forwarded_motion_lock);

    ImGuiIO& io = ImGui::GetIO();
    if (g_overlay_focus_release_requested.exchange(false, std::memory_order_acq_rel)) {
        cancel_forwarded_touch(io);
        ImGui::ClearActiveID();
    }
    for (int i = 0; i < local_count; i++) {
        const ForwardedMotionEvent& event = local_events[i];
        add_forwarded_mouse_source(io, event.tool_type);
        switch (event.action) {
            case AMOTION_EVENT_ACTION_DOWN:
            case AMOTION_EVENT_ACTION_POINTER_DOWN:
                begin_forwarded_touch(io, event);
                break;
            case AMOTION_EVENT_ACTION_UP:
            case AMOTION_EVENT_ACTION_POINTER_UP:
                end_forwarded_touch(io, event);
                break;
            case AMOTION_EVENT_ACTION_MOVE:
            case AMOTION_EVENT_ACTION_HOVER_MOVE:
                update_forwarded_touch_move(io, event);
                break;
            case AMOTION_EVENT_ACTION_CANCEL:
                cancel_forwarded_touch(io);
                break;
            case AMOTION_EVENT_ACTION_BUTTON_PRESS:
            case AMOTION_EVENT_ACTION_BUTTON_RELEASE:
                io.AddMouseButtonEvent(0, (event.button_state & AMOTION_EVENT_BUTTON_PRIMARY) != 0);
                io.AddMouseButtonEvent(1, (event.button_state & AMOTION_EVENT_BUTTON_SECONDARY) != 0);
                io.AddMouseButtonEvent(2, (event.button_state & AMOTION_EVENT_BUTTON_TERTIARY) != 0);
                break;
            default:
                break;
        }
    }
    return local_count;
}
