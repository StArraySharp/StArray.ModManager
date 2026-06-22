#ifndef JNI_HELPER_H
#define JNI_HELPER_H

#include <jni.h>
#include <android/input.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * JNI Helper - 简化 JNI 调用的辅助函数
 */

/**
 * 保存 JavaVM 指针（在 JNI_OnLoad 中调用）
 */
void jnihelper_set_jvm(JavaVM *vm);

/**
 * 获取 JavaVM
 */
JavaVM* jnihelper_get_jvm();

/**
 * 获取 JNIEnv（自动附加当前线程）
 */
JNIEnv* jnihelper_get_env();

/**
 * 查找 Java 类（返回全局引用）
 */
jclass jnihelper_find_class(const char *class_name);

/**
 * 获取方法 ID
 */
jmethodID jnihelper_get_method_id(jclass clazz, const char *method_name, const char *signature);

/**
 * 获取静态方法 ID
 */
jmethodID jnihelper_get_static_method_id(jclass clazz, const char *method_name, const char *signature);

/**
 * 获取字段 ID
 */
jfieldID jnihelper_get_field_id(jclass clazz, const char *field_name, const char *signature);

/**
 * 获取静态字段 ID
 */
jfieldID jnihelper_get_static_field_id(jclass clazz, const char *field_name, const char *signature);

/**
 * 创建 Java String
 */
jstring jnihelper_new_string(const char *str);

/**
 * Java String 转 C string（需要调用者释放内存）
 */
char* jnihelper_get_string(jstring jstr);

/**
 * 删除本地引用
 */
void jnihelper_delete_local_ref(jobject obj);

/**
 * 删除全局引用
 */
void jnihelper_delete_global_ref(jobject obj);

/**
 * 检查并清除异常
 */
jboolean jnihelper_check_exception();

/**
 * 获取当前 Activity 或 Application
 */
jobject jnihelper_get_current_activity();

/**
 * 获取 Unity Surface
 * 调用链: UnityPlayer.currentActivity -> mUnityPlayer -> getSurfaceView() -> getHolder() -> getSurface()
 */
jobject jnihelper_get_unity_surface();

/**
 * 获取 Unity ANativeWindow（从 Surface 对象转换）
 * 返回可用于 ImGui_ImplAndroid_Init 的 ANativeWindow*
 */
struct ANativeWindow* jnihelper_get_unity_native_window();

/**
 * 通用 JNI 调用辅助（供 C# P/Invoke 使用）
 */
jobject jnihelper_call_object_method(jobject obj, jmethodID methodID);
jobject jnihelper_call_static_object_method(jclass clazz, jmethodID methodID);
jobject jnihelper_get_static_object_field(jclass clazz, jfieldID fieldID);
jobject jnihelper_get_object_field(jobject obj, jfieldID fieldID);
jclass jnihelper_get_object_class(jobject obj);
jobject jnihelper_surface_to_native_window(jobject surface);

/**
 * 从 AInputEvent KeyEvent 提取 Unicode 字符（通过 JNI KeyEvent.getUnicodeChar）
 */
uint32_t jnihelper_keyevent_get_unicode(AInputEvent *event);

/**
 * 从 Java InputEvent 对象提取并缓存 Unicode（配合 nativeInjectEvent hook）
 */
void jnihelper_capture_input_event_unicode(JNIEnv* env, jobject inputEvent);
uint32_t jnihelper_poll_captured_unicode();

#ifdef __cplusplus
}
#endif

#endif // JNIHELPER_H
