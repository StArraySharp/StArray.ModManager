#ifndef STARRAY_JAVA_INPUT_BRIDGE_H
#define STARRAY_JAVA_INPUT_BRIDGE_H

#include <stdint.h>

#define MODMANAGER_JAVA_INPUT_ABI_VERSION 2
#define MODMANAGER_JAVA_INPUT_MAX_POINTERS 32
#define MODMANAGER_JAVA_INPUT_MAX_HISTORY_SAMPLES 256
#define MODMANAGER_JAVA_INPUT_AXIS_COUNT 64

typedef struct ModManagerJavaInputPointer {
    int32_t id;
    int32_t tool_type;
    float x;
    float y;
    float raw_x;
    float raw_y;
    float pressure;
    float size;
    float touch_major;
    float touch_minor;
    float tool_major;
    float tool_minor;
    float orientation;
    float tilt;
    float distance;
    float axis_values[MODMANAGER_JAVA_INPUT_AXIS_COUNT];
} ModManagerJavaInputPointer;

#pragma pack(push, 4)
typedef struct ModManagerJavaInputHistorySample {
    int32_t pointer_count;
    int32_t stored_pointer_count;
    int64_t event_time_nanos;
    const ModManagerJavaInputPointer *pointers;
} ModManagerJavaInputHistorySample;

typedef struct ModManagerJavaInputEvent {
    int32_t struct_size;
    int32_t abi_version;
    int32_t type;
    int32_t is_generic_motion;
    int32_t full_data_included;
    int32_t action;
    int32_t action_index;
    int32_t pointer_count;
    int32_t stored_pointer_count;
    int32_t source;
    int32_t device_id;
    int32_t flags;
    int32_t meta_state;
    int32_t button_state;
    int32_t action_button;
    int32_t key_code;
    int32_t scan_code;
    int32_t repeat_count;
    int32_t unicode_code_point;
    int32_t viewport_width;
    int32_t viewport_height;
    int32_t raw_action;
    int64_t event_time_nanos;
    int64_t down_time_nanos;
    float horizontal_scroll;
    float vertical_scroll;
    int32_t history_sample_count;
    int32_t stored_history_sample_count;
    const ModManagerJavaInputHistorySample *history_samples;
    ModManagerJavaInputPointer pointers[MODMANAGER_JAVA_INPUT_MAX_POINTERS];
} ModManagerJavaInputEvent;
#pragma pack(pop)

typedef void (*ModManagerJavaInputCallback)(const ModManagerJavaInputEvent *event);

#ifdef __cplusplus
extern "C" {
#endif

void modmanager_set_java_input_callback(void *callback);
void modmanager_set_java_input_capture_state(int overlay_visible,
                                             int capture_mouse,
                                             int capture_keyboard);
void modmanager_set_java_input_full_data_enabled(int enabled);

#ifdef __cplusplus
}
#endif

#endif
