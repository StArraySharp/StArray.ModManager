#include "starray_touch_bridge.h"

#include <jni.h>

extern "C" JNIEXPORT jboolean JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeForwardMotionEvent(
    JNIEnv*,
    jclass,
    jint action,
    jfloat x,
    jfloat y,
    jint tool_type,
    jint button_state) {
    return modmanager_touch_forward_motion_event(
               static_cast<int>(action),
               static_cast<float>(x),
               static_cast<float>(y),
               static_cast<int>(tool_type),
               static_cast<int>(button_state)) != 0
        ? JNI_TRUE
        : JNI_FALSE;
}

extern "C" JNIEXPORT void JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeSetOverlayVisible(
    JNIEnv*,
    jclass,
    jboolean visible) {
    modmanager_overlay_ui_set_visible(visible == JNI_TRUE ? 1 : 0);
}

extern "C" JNIEXPORT void JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeSetModalInputCapture(
    JNIEnv*,
    jclass,
    jboolean active) {
    modmanager_modal_input_set_active(active == JNI_TRUE ? 1 : 0);
}

extern "C" JNIEXPORT jint JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeIsModalInputCaptureActive(
    JNIEnv*,
    jclass) {
    return static_cast<jint>(modmanager_modal_input_is_active());
}

extern "C" JNIEXPORT void JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeRequestModalClose(
    JNIEnv*,
    jclass) {
    modmanager_modal_input_request_close();
}

extern "C" JNIEXPORT jint JNICALL
Java_starray_android_modmanager_touch_StArrayTouchBridge_nativeTakeModalCloseRequest(
    JNIEnv*,
    jclass) {
    return static_cast<jint>(modmanager_modal_input_take_close_request());
}
