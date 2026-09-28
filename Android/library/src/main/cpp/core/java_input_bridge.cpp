#include <jni.h>
#include "java_input_bridge.h"

#include <algorithm>
#include <atomic>
#include <cstddef>
#include <cstdint>
#include <mutex>
#include <vector>

namespace {

constexpr int32_t kInputTypeMotion = 1;
constexpr int32_t kInputTypeKey = 2;
constexpr int32_t kMaxPointers = MODMANAGER_JAVA_INPUT_MAX_POINTERS;
constexpr int32_t kMaxHistorySamples = MODMANAGER_JAVA_INPUT_MAX_HISTORY_SAMPLES;
constexpr int32_t kAxisCount = MODMANAGER_JAVA_INPUT_AXIS_COUNT;
constexpr int32_t kAxisVScroll = 9;
constexpr int32_t kAxisHScroll = 10;
constexpr int32_t kAxisDistance = 24;
constexpr int32_t kAxisTilt = 25;

using JavaInputEvent = ModManagerJavaInputEvent;
using JavaInputHistorySample = ModManagerJavaInputHistorySample;
using JavaInputPointer = ModManagerJavaInputPointer;
using java_input_callback = ModManagerJavaInputCallback;

static_assert(sizeof(JavaInputPointer) == 316);
static_assert(sizeof(JavaInputHistorySample) == 24);
static_assert(offsetof(JavaInputEvent, history_samples) == 120);
static_assert(offsetof(JavaInputEvent, pointers) == 128);
static_assert(sizeof(JavaInputEvent) == 10240);

std::atomic<java_input_callback> g_java_input_callback{nullptr};
std::atomic<bool> g_java_activity_bridge_active{false};
std::atomic<bool> g_full_data_enabled{false};
std::atomic<bool> g_overlay_visible{false};
std::atomic<bool> g_capture_mouse{false};
std::atomic<bool> g_capture_keyboard{false};

struct MotionMethods {
    jclass clazz = nullptr;
    jmethodID action = nullptr;
    jmethodID action_masked = nullptr;
    jmethodID action_index = nullptr;
    jmethodID pointer_count = nullptr;
    jmethodID pointer_id = nullptr;
    jmethodID tool_type = nullptr;
    jmethodID x = nullptr;
    jmethodID y = nullptr;
    jmethodID pressure = nullptr;
    jmethodID size = nullptr;
    jmethodID touch_major = nullptr;
    jmethodID touch_minor = nullptr;
    jmethodID tool_major = nullptr;
    jmethodID tool_minor = nullptr;
    jmethodID orientation = nullptr;
    jmethodID axis_value = nullptr;
    jmethodID event_time_nanos = nullptr;
    jmethodID historical_event_time_nanos = nullptr;
    jmethodID raw_x_pointer = nullptr;
    jmethodID raw_y_pointer = nullptr;
    jmethodID raw_x = nullptr;
    jmethodID raw_y = nullptr;
    jmethodID historical_event_time = nullptr;
    jmethodID historical_x = nullptr;
    jmethodID historical_y = nullptr;
    jmethodID historical_pressure = nullptr;
    jmethodID historical_size = nullptr;
    jmethodID historical_touch_major = nullptr;
    jmethodID historical_touch_minor = nullptr;
    jmethodID historical_tool_major = nullptr;
    jmethodID historical_tool_minor = nullptr;
    jmethodID historical_orientation = nullptr;
    jmethodID historical_axis_value = nullptr;
    jmethodID history_size = nullptr;
    jmethodID event_time = nullptr;
    jmethodID down_time = nullptr;
    jmethodID source = nullptr;
    jmethodID device_id = nullptr;
    jmethodID flags = nullptr;
    jmethodID meta_state = nullptr;
    jmethodID button_state = nullptr;
    jmethodID action_button = nullptr;
};

struct KeyMethods {
    jclass clazz = nullptr;
    jmethodID action = nullptr;
    jmethodID key_code = nullptr;
    jmethodID scan_code = nullptr;
    jmethodID meta_state = nullptr;
    jmethodID repeat_count = nullptr;
    jmethodID event_time = nullptr;
    jmethodID down_time = nullptr;
    jmethodID source = nullptr;
    jmethodID device_id = nullptr;
    jmethodID flags = nullptr;
    jmethodID unicode_char = nullptr;
};

std::mutex g_method_lock;
MotionMethods g_motion_methods;
KeyMethods g_key_methods;

template <typename T>
bool has_exception(JNIEnv *env, T *value) {
    if (value != nullptr && !env->ExceptionCheck())
        return false;
    if (env->ExceptionCheck())
        env->ExceptionClear();
    return true;
}

jmethodID method(JNIEnv *env, jclass clazz, const char *name, const char *signature) {
    jmethodID id = env->GetMethodID(clazz, name, signature);
    return has_exception(env, id) ? nullptr : id;
}

bool initialize_motion_methods(JNIEnv *env, jobject event) {
    if (g_motion_methods.clazz != nullptr)
        return true;

    std::lock_guard<std::mutex> guard(g_method_lock);
    if (g_motion_methods.clazz != nullptr)
        return true;

    jclass local = env->GetObjectClass(event);
    if (has_exception(env, local))
        return false;
    jclass global = static_cast<jclass>(env->NewGlobalRef(local));
    env->DeleteLocalRef(local);
    if (has_exception(env, global))
        return false;

    MotionMethods methods;
    methods.clazz = global;
    methods.action = method(env, global, "getAction", "()I");
    methods.action_masked = method(env, global, "getActionMasked", "()I");
    methods.action_index = method(env, global, "getActionIndex", "()I");
    methods.pointer_count = method(env, global, "getPointerCount", "()I");
    methods.pointer_id = method(env, global, "getPointerId", "(I)I");
    methods.tool_type = method(env, global, "getToolType", "(I)I");
    methods.x = method(env, global, "getX", "(I)F");
    methods.y = method(env, global, "getY", "(I)F");
    methods.pressure = method(env, global, "getPressure", "(I)F");
    methods.size = method(env, global, "getSize", "(I)F");
    methods.touch_major = method(env, global, "getTouchMajor", "(I)F");
    methods.touch_minor = method(env, global, "getTouchMinor", "(I)F");
    methods.tool_major = method(env, global, "getToolMajor", "(I)F");
    methods.tool_minor = method(env, global, "getToolMinor", "(I)F");
    methods.orientation = method(env, global, "getOrientation", "(I)F");
    methods.axis_value = method(env, global, "getAxisValue", "(II)F");
    methods.event_time_nanos = method(env, global, "getEventTimeNanos", "()J");
    methods.historical_event_time_nanos = method(
        env, global, "getHistoricalEventTimeNanos", "(I)J");
    methods.raw_x_pointer = method(env, global, "getRawX", "(I)F");
    methods.raw_y_pointer = method(env, global, "getRawY", "(I)F");
    methods.raw_x = method(env, global, "getRawX", "()F");
    methods.raw_y = method(env, global, "getRawY", "()F");
    methods.historical_event_time = method(env, global, "getHistoricalEventTime", "(I)J");
    methods.historical_x = method(env, global, "getHistoricalX", "(II)F");
    methods.historical_y = method(env, global, "getHistoricalY", "(II)F");
    methods.historical_pressure = method(env, global, "getHistoricalPressure", "(II)F");
    methods.historical_size = method(env, global, "getHistoricalSize", "(II)F");
    methods.historical_touch_major = method(env, global, "getHistoricalTouchMajor", "(II)F");
    methods.historical_touch_minor = method(env, global, "getHistoricalTouchMinor", "(II)F");
    methods.historical_tool_major = method(env, global, "getHistoricalToolMajor", "(II)F");
    methods.historical_tool_minor = method(env, global, "getHistoricalToolMinor", "(II)F");
    methods.historical_orientation = method(env, global, "getHistoricalOrientation", "(II)F");
    methods.historical_axis_value = method(env, global, "getHistoricalAxisValue", "(III)F");
    methods.history_size = method(env, global, "getHistorySize", "()I");
    methods.event_time = method(env, global, "getEventTime", "()J");
    methods.down_time = method(env, global, "getDownTime", "()J");
    methods.source = method(env, global, "getSource", "()I");
    methods.device_id = method(env, global, "getDeviceId", "()I");
    methods.flags = method(env, global, "getFlags", "()I");
    methods.meta_state = method(env, global, "getMetaState", "()I");
    methods.button_state = method(env, global, "getButtonState", "()I");
    methods.action_button = method(env, global, "getActionButton", "()I");

    const bool valid = methods.action && methods.action_masked && methods.action_index &&
        methods.pointer_count && methods.pointer_id && methods.tool_type && methods.x &&
        methods.y && methods.pressure && methods.size && methods.touch_major &&
        methods.touch_minor && methods.tool_major && methods.tool_minor &&
        methods.orientation && methods.axis_value && methods.historical_event_time &&
        methods.historical_x && methods.historical_y && methods.historical_pressure &&
        methods.historical_size && methods.historical_touch_major &&
        methods.historical_touch_minor && methods.historical_tool_major &&
        methods.historical_tool_minor && methods.historical_orientation &&
        methods.historical_axis_value && methods.history_size && methods.event_time &&
        methods.down_time && methods.source && methods.device_id && methods.flags &&
        methods.meta_state && methods.button_state && methods.action_button;
    if (!valid || env->ExceptionCheck()) {
        if (env->ExceptionCheck())
            env->ExceptionClear();
        env->DeleteGlobalRef(global);
        return false;
    }

    g_motion_methods = methods;
    return true;
}

bool initialize_key_methods(JNIEnv *env, jobject event) {
    if (g_key_methods.clazz != nullptr)
        return true;

    std::lock_guard<std::mutex> guard(g_method_lock);
    if (g_key_methods.clazz != nullptr)
        return true;

    jclass local = env->GetObjectClass(event);
    if (has_exception(env, local))
        return false;
    jclass global = static_cast<jclass>(env->NewGlobalRef(local));
    env->DeleteLocalRef(local);
    if (has_exception(env, global))
        return false;

    KeyMethods methods;
    methods.clazz = global;
    methods.action = method(env, global, "getAction", "()I");
    methods.key_code = method(env, global, "getKeyCode", "()I");
    methods.scan_code = method(env, global, "getScanCode", "()I");
    methods.meta_state = method(env, global, "getMetaState", "()I");
    methods.repeat_count = method(env, global, "getRepeatCount", "()I");
    methods.event_time = method(env, global, "getEventTime", "()J");
    methods.down_time = method(env, global, "getDownTime", "()J");
    methods.source = method(env, global, "getSource", "()I");
    methods.device_id = method(env, global, "getDeviceId", "()I");
    methods.flags = method(env, global, "getFlags", "()I");
    methods.unicode_char = method(env, global, "getUnicodeChar", "(I)I");

    const bool valid = methods.action && methods.key_code && methods.scan_code &&
        methods.meta_state && methods.repeat_count && methods.event_time &&
        methods.down_time && methods.source && methods.device_id && methods.flags &&
        methods.unicode_char;
    if (!valid || env->ExceptionCheck()) {
        if (env->ExceptionCheck())
            env->ExceptionClear();
        env->DeleteGlobalRef(global);
        return false;
    }

    g_key_methods = methods;
    return true;
}

jfloat motion_float(JNIEnv *env, jobject event, jmethodID method_id, jint pointer,
                    jint history_index, bool historical) {
    if (historical)
        return env->CallFloatMethod(event, method_id, pointer, history_index);
    return env->CallFloatMethod(event, method_id, pointer);
}

jfloat motion_axis(JNIEnv *env, jobject event, const MotionMethods &methods,
                   jint pointer, jint axis, jint history_index, bool historical) {
    if (historical) {
        return env->CallFloatMethod(event, methods.historical_axis_value,
                                    axis, pointer, history_index);
    }
    return env->CallFloatMethod(event, methods.axis_value, axis, pointer);
}

void fill_motion_pointer(JNIEnv *env, jobject event, const MotionMethods &methods,
                         int32_t index, bool historical, int32_t history_index,
                         bool include_full_data, float raw_offset_x,
                         float raw_offset_y, JavaInputPointer &pointer) {
    pointer.id = env->CallIntMethod(event, methods.pointer_id, index);
    pointer.tool_type = env->CallIntMethod(event, methods.tool_type, index);
    pointer.x = motion_float(env, event,
        historical ? methods.historical_x : methods.x, index, history_index, historical);
    pointer.y = motion_float(env, event,
        historical ? methods.historical_y : methods.y, index, history_index, historical);
    pointer.raw_x = pointer.x + raw_offset_x;
    pointer.raw_y = pointer.y + raw_offset_y;
    pointer.pressure = motion_float(env, event,
        historical ? methods.historical_pressure : methods.pressure,
        index, history_index, historical);

    if (!include_full_data)
        return;

    pointer.size = motion_float(env, event,
        historical ? methods.historical_size : methods.size, index, history_index, historical);
    pointer.touch_major = motion_float(env, event,
        historical ? methods.historical_touch_major : methods.touch_major,
        index, history_index, historical);
    pointer.touch_minor = motion_float(env, event,
        historical ? methods.historical_touch_minor : methods.touch_minor,
        index, history_index, historical);
    pointer.tool_major = motion_float(env, event,
        historical ? methods.historical_tool_major : methods.tool_major,
        index, history_index, historical);
    pointer.tool_minor = motion_float(env, event,
        historical ? methods.historical_tool_minor : methods.tool_minor,
        index, history_index, historical);
    pointer.orientation = motion_float(env, event,
        historical ? methods.historical_orientation : methods.orientation,
        index, history_index, historical);
    for (int32_t axis = 0; axis < kAxisCount; ++axis) {
        pointer.axis_values[axis] = motion_axis(
            env, event, methods, index, axis, history_index, historical);
    }
    pointer.tilt = pointer.axis_values[kAxisTilt];
    pointer.distance = pointer.axis_values[kAxisDistance];
}

bool build_motion_event(JNIEnv *env, jobject event, jint viewport_width,
                        jint viewport_height, bool generic, bool include_full_data,
                        JavaInputEvent &input,
                        std::vector<JavaInputHistorySample> &history_samples,
                        std::vector<JavaInputPointer> &history_pointers) {
    const MotionMethods &m = g_motion_methods;
    input = {};
    input.struct_size = sizeof(input);
    input.abi_version = MODMANAGER_JAVA_INPUT_ABI_VERSION;
    input.type = kInputTypeMotion;
    input.is_generic_motion = generic ? 1 : 0;
    input.full_data_included = include_full_data ? 1 : 0;
    input.raw_action = env->CallIntMethod(event, m.action);
    input.action = env->CallIntMethod(event, m.action_masked);
    input.action_index = env->CallIntMethod(event, m.action_index);
    input.pointer_count = env->CallIntMethod(event, m.pointer_count);
    input.stored_pointer_count = std::clamp(input.pointer_count, 0, kMaxPointers);
    input.source = env->CallIntMethod(event, m.source);
    input.device_id = env->CallIntMethod(event, m.device_id);
    input.flags = env->CallIntMethod(event, m.flags);
    input.meta_state = env->CallIntMethod(event, m.meta_state);
    input.button_state = env->CallIntMethod(event, m.button_state);
    input.action_button = env->CallIntMethod(event, m.action_button);
    input.event_time_nanos = m.event_time_nanos != nullptr
        ? static_cast<int64_t>(env->CallLongMethod(event, m.event_time_nanos))
        : static_cast<int64_t>(env->CallLongMethod(event, m.event_time)) * 1000000LL;
    input.down_time_nanos = static_cast<int64_t>(env->CallLongMethod(event, m.down_time)) * 1000000LL;
    input.viewport_width = viewport_width;
    input.viewport_height = viewport_height;

    if (env->ExceptionCheck()) {
        env->ExceptionClear();
        return false;
    }

    float raw_offset_x = 0.0f;
    float raw_offset_y = 0.0f;
    if (input.stored_pointer_count > 0) {
        if (m.raw_x_pointer != nullptr && m.raw_y_pointer != nullptr) {
            const jfloat raw_x = env->CallFloatMethod(event, m.raw_x_pointer, 0);
            const jfloat raw_y = env->CallFloatMethod(event, m.raw_y_pointer, 0);
            const jfloat local_x = env->CallFloatMethod(event, m.x, 0);
            const jfloat local_y = env->CallFloatMethod(event, m.y, 0);
            raw_offset_x = raw_x - local_x;
            raw_offset_y = raw_y - local_y;
        } else if (m.raw_x != nullptr && m.raw_y != nullptr) {
            const jfloat raw_x = env->CallFloatMethod(event, m.raw_x);
            const jfloat raw_y = env->CallFloatMethod(event, m.raw_y);
            const jfloat local_x = env->CallFloatMethod(event, m.x, 0);
            const jfloat local_y = env->CallFloatMethod(event, m.y, 0);
            raw_offset_x = raw_x - local_x;
            raw_offset_y = raw_y - local_y;
        }
    }

    if (env->ExceptionCheck()) {
        env->ExceptionClear();
        return false;
    }

    for (int32_t index = 0; index < input.stored_pointer_count; ++index) {
        fill_motion_pointer(env, event, m, index, false, 0, include_full_data,
                            raw_offset_x, raw_offset_y, input.pointers[index]);
    }

    const jint axis_index = std::clamp(input.action_index, 0,
        std::max(0, input.stored_pointer_count - 1));
    if (input.stored_pointer_count > 0) {
        input.horizontal_scroll = motion_axis(env, event, m, axis_index, kAxisHScroll,
                                              0, false);
        input.vertical_scroll = motion_axis(env, event, m, axis_index, kAxisVScroll,
                                            0, false);
    }

    input.history_sample_count = std::max(0, env->CallIntMethod(event, m.history_size));
    input.stored_history_sample_count = include_full_data
        ? std::min(input.history_sample_count, kMaxHistorySamples)
        : 0;

    if (env->ExceptionCheck()) {
        env->ExceptionClear();
        return false;
    }

    if (input.stored_history_sample_count > 0) {
        const size_t sample_count = static_cast<size_t>(input.stored_history_sample_count);
        const size_t pointers_per_sample = static_cast<size_t>(input.stored_pointer_count);
        history_samples.resize(sample_count);
        history_pointers.resize(sample_count * pointers_per_sample);

        for (int32_t history_index = 0;
             history_index < input.stored_history_sample_count;
             ++history_index) {
            JavaInputHistorySample &sample = history_samples[history_index];
            sample.pointer_count = input.pointer_count;
            sample.stored_pointer_count = input.stored_pointer_count;
            const jlong sample_time_nanos = m.historical_event_time_nanos != nullptr
                ? env->CallLongMethod(event, m.historical_event_time_nanos, history_index)
                : static_cast<int64_t>(env->CallLongMethod(
                    event, m.historical_event_time, history_index)) * 1000000LL;
            if (env->ExceptionCheck()) {
                env->ExceptionClear();
                return false;
            }
            sample.event_time_nanos = static_cast<int64_t>(sample_time_nanos);
            JavaInputPointer *sample_pointers = pointers_per_sample > 0
                ? history_pointers.data() +
                    static_cast<size_t>(history_index) * pointers_per_sample
                : nullptr;
            sample.pointers = sample_pointers;
            for (int32_t pointer_index = 0;
                 pointer_index < sample.stored_pointer_count;
                 ++pointer_index) {
                fill_motion_pointer(env, event, m, pointer_index, true, history_index,
                    true, raw_offset_x, raw_offset_y, sample_pointers[pointer_index]);
            }

            if (env->ExceptionCheck()) {
                env->ExceptionClear();
                return false;
            }
        }

        input.history_samples = history_samples.data();
    }

    if (env->ExceptionCheck()) {
        env->ExceptionClear();
        return false;
    }
    return true;
}

bool should_capture_motion() {
    return g_java_activity_bridge_active.load(std::memory_order_acquire) &&
        (g_overlay_visible.load(std::memory_order_acquire) ||
         g_capture_mouse.load(std::memory_order_acquire));
}

} // namespace

extern "C" {

void modmanager_set_java_input_callback(void *callback) {
    g_java_input_callback.store(reinterpret_cast<java_input_callback>(callback),
                                std::memory_order_release);
}

void modmanager_set_java_input_capture_state(int overlay_visible,
                                             int capture_mouse,
                                             int capture_keyboard) {
    g_overlay_visible.store(overlay_visible != 0, std::memory_order_release);
    g_capture_mouse.store(capture_mouse != 0, std::memory_order_release);
    g_capture_keyboard.store(capture_keyboard != 0, std::memory_order_release);
}

void modmanager_set_java_input_full_data_enabled(int enabled) {
    g_full_data_enabled.store(enabled != 0, std::memory_order_release);
}

} // extern "C"

extern "C" JNIEXPORT void JNICALL
Java_starray_adofai_v3_MainActivity_nativeRegisterInputBridge(
    JNIEnv *, jobject) {
    g_java_activity_bridge_active.store(true, std::memory_order_release);
}

extern "C" JNIEXPORT jboolean JNICALL
Java_starray_adofai_v3_MainActivity_nativeObserveMotionEvent(
    JNIEnv *env, jobject, jobject event, jint viewport_width,
    jint viewport_height, jboolean generic_motion) {
    if (event == nullptr || !initialize_motion_methods(env, event))
        return JNI_FALSE;

    java_input_callback callback = g_java_input_callback.load(std::memory_order_acquire);
    if (callback != nullptr) {
        const bool include_full_data = g_full_data_enabled.load(std::memory_order_acquire);
        JavaInputEvent input{};
        std::vector<JavaInputHistorySample> history_samples;
        std::vector<JavaInputPointer> history_pointers;
        try {
            if (build_motion_event(env, event, viewport_width, viewport_height,
                    generic_motion == JNI_TRUE, include_full_data, input,
                    history_samples, history_pointers)) {
                input.history_samples = history_samples.empty()
                    ? nullptr
                    : history_samples.data();
                callback(&input);
            }
        } catch (...) {
            if (env->ExceptionCheck())
                env->ExceptionClear();
        }
    }

    return should_capture_motion() ? JNI_TRUE : JNI_FALSE;
}

extern "C" JNIEXPORT jboolean JNICALL
Java_starray_adofai_v3_MainActivity_nativeObserveKeyEvent(
    JNIEnv *env, jobject, jobject event) {
    if (event == nullptr || !initialize_key_methods(env, event))
        return JNI_FALSE;

    const KeyMethods &m = g_key_methods;
    JavaInputEvent input{};
    input.struct_size = sizeof(input);
    input.abi_version = MODMANAGER_JAVA_INPUT_ABI_VERSION;
    input.type = kInputTypeKey;
    input.action = env->CallIntMethod(event, m.action);
    input.key_code = env->CallIntMethod(event, m.key_code);
    input.scan_code = env->CallIntMethod(event, m.scan_code);
    input.meta_state = env->CallIntMethod(event, m.meta_state);
    input.repeat_count = env->CallIntMethod(event, m.repeat_count);
    input.event_time_nanos = static_cast<int64_t>(env->CallLongMethod(event, m.event_time)) * 1000000LL;
    input.down_time_nanos = static_cast<int64_t>(env->CallLongMethod(event, m.down_time)) * 1000000LL;
    input.source = env->CallIntMethod(event, m.source);
    input.device_id = env->CallIntMethod(event, m.device_id);
    input.flags = env->CallIntMethod(event, m.flags);
    input.unicode_code_point = env->CallIntMethod(event, m.unicode_char, input.meta_state);
    if (env->ExceptionCheck()) {
        env->ExceptionClear();
        return JNI_FALSE;
    }

    java_input_callback callback = g_java_input_callback.load(std::memory_order_acquire);
    if (callback != nullptr)
        callback(&input);

    const bool back_key = input.key_code == 4;
    const bool capture = g_java_activity_bridge_active.load(std::memory_order_acquire) &&
        !back_key && (g_overlay_visible.load(std::memory_order_acquire) ||
                      g_capture_keyboard.load(std::memory_order_acquire));
    return capture ? JNI_TRUE : JNI_FALSE;
}
