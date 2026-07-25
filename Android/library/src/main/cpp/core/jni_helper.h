#ifndef JNI_HELPER_H
#define JNI_HELPER_H

#include <jni.h>
#include <android/input.h>

#ifdef __cplusplus
extern "C" {
#endif

// ===== 初始化 =====
void jnihelper_set_jvm(JavaVM *vm);
JavaVM* jnihelper_get_jvm();
JNIEnv* jnihelper_get_env();

// ===== 类 / 方法 / 字段查找 =====
jclass jnihelper_find_class(const char *class_name);
jmethodID jnihelper_get_method_id(jclass clazz, const char *method_name, const char *signature);
jmethodID jnihelper_get_static_method_id(jclass clazz, const char *method_name, const char *signature);
jfieldID jnihelper_get_field_id(jclass clazz, const char *field_name, const char *signature);
jfieldID jnihelper_get_static_field_id(jclass clazz, const char *field_name, const char *signature);

// ===== 引用管理 =====
jobject jnihelper_new_global_ref(jobject obj);
jobject jnihelper_new_local_ref(jobject obj);
void jnihelper_delete_local_ref(jobject obj);
void jnihelper_delete_global_ref(jobject obj);
jclass jnihelper_get_object_class(jobject obj);

// ===== 异常 =====
jboolean jnihelper_check_exception();
void jnihelper_clear_exception();

// ===== 字符串 =====
jstring jnihelper_new_string(const char *str);
char* jnihelper_get_string(jstring jstr);
const char* jnihelper_get_string_utf_chars(jstring jstr);
void jnihelper_release_string_utf_chars(jstring jstr, const char *utf);
jsize jnihelper_get_string_length(jstring jstr);
jstring jnihelper_new_string_utf(const char *utf);

// ===== 实例方法调用 (A 变体，通过 jvalue* 传参) =====
jobject jnihelper_call_object_method_a(jobject obj, jmethodID methodID, jvalue *args);
jboolean jnihelper_call_boolean_method_a(jobject obj, jmethodID methodID, jvalue *args);
jbyte jnihelper_call_byte_method_a(jobject obj, jmethodID methodID, jvalue *args);
jchar jnihelper_call_char_method_a(jobject obj, jmethodID methodID, jvalue *args);
jshort jnihelper_call_short_method_a(jobject obj, jmethodID methodID, jvalue *args);
jint jnihelper_call_int_method_a(jobject obj, jmethodID methodID, jvalue *args);
jlong jnihelper_call_long_method_a(jobject obj, jmethodID methodID, jvalue *args);
jfloat jnihelper_call_float_method_a(jobject obj, jmethodID methodID, jvalue *args);
jdouble jnihelper_call_double_method_a(jobject obj, jmethodID methodID, jvalue *args);
void jnihelper_call_void_method_a(jobject obj, jmethodID methodID, jvalue *args);

// ===== 实例方法调用 (无参简版) =====
jobject jnihelper_call_object_method(jobject obj, jmethodID methodID);

// ===== 静态方法调用 (A 变体) =====
jobject jnihelper_call_static_object_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jboolean jnihelper_call_static_boolean_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jbyte jnihelper_call_static_byte_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jchar jnihelper_call_static_char_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jshort jnihelper_call_static_short_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jint jnihelper_call_static_int_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jlong jnihelper_call_static_long_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jfloat jnihelper_call_static_float_method_a(jclass clazz, jmethodID methodID, jvalue *args);
jdouble jnihelper_call_static_double_method_a(jclass clazz, jmethodID methodID, jvalue *args);
void jnihelper_call_static_void_method_a(jclass clazz, jmethodID methodID, jvalue *args);

// ===== 静态方法调用 (无参简版) =====
jobject jnihelper_call_static_object_method(jclass clazz, jmethodID methodID);

// ===== 实例字段 Get/Set =====
jobject jnihelper_get_object_field(jobject obj, jfieldID fieldID);
jboolean jnihelper_get_boolean_field(jobject obj, jfieldID fieldID);
jbyte jnihelper_get_byte_field(jobject obj, jfieldID fieldID);
jchar jnihelper_get_char_field(jobject obj, jfieldID fieldID);
jshort jnihelper_get_short_field(jobject obj, jfieldID fieldID);
jint jnihelper_get_int_field(jobject obj, jfieldID fieldID);
jlong jnihelper_get_long_field(jobject obj, jfieldID fieldID);
jfloat jnihelper_get_float_field(jobject obj, jfieldID fieldID);
jdouble jnihelper_get_double_field(jobject obj, jfieldID fieldID);

void jnihelper_set_object_field(jobject obj, jfieldID fieldID, jobject value);
void jnihelper_set_boolean_field(jobject obj, jfieldID fieldID, jboolean value);
void jnihelper_set_byte_field(jobject obj, jfieldID fieldID, jbyte value);
void jnihelper_set_char_field(jobject obj, jfieldID fieldID, jchar value);
void jnihelper_set_short_field(jobject obj, jfieldID fieldID, jshort value);
void jnihelper_set_int_field(jobject obj, jfieldID fieldID, jint value);
void jnihelper_set_long_field(jobject obj, jfieldID fieldID, jlong value);
void jnihelper_set_float_field(jobject obj, jfieldID fieldID, jfloat value);
void jnihelper_set_double_field(jobject obj, jfieldID fieldID, jdouble value);

// ===== 静态字段 Get/Set =====
jobject jnihelper_get_static_object_field(jclass clazz, jfieldID fieldID);
jboolean jnihelper_get_static_boolean_field(jclass clazz, jfieldID fieldID);
jbyte jnihelper_get_static_byte_field(jclass clazz, jfieldID fieldID);
jchar jnihelper_get_static_char_field(jclass clazz, jfieldID fieldID);
jshort jnihelper_get_static_short_field(jclass clazz, jfieldID fieldID);
jint jnihelper_get_static_int_field(jclass clazz, jfieldID fieldID);
jlong jnihelper_get_static_long_field(jclass clazz, jfieldID fieldID);
jfloat jnihelper_get_static_float_field(jclass clazz, jfieldID fieldID);
jdouble jnihelper_get_static_double_field(jclass clazz, jfieldID fieldID);

void jnihelper_set_static_object_field(jclass clazz, jfieldID fieldID, jobject value);
void jnihelper_set_static_boolean_field(jclass clazz, jfieldID fieldID, jboolean value);
void jnihelper_set_static_byte_field(jclass clazz, jfieldID fieldID, jbyte value);
void jnihelper_set_static_char_field(jclass clazz, jfieldID fieldID, jchar value);
void jnihelper_set_static_short_field(jclass clazz, jfieldID fieldID, jshort value);
void jnihelper_set_static_int_field(jclass clazz, jfieldID fieldID, jint value);
void jnihelper_set_static_long_field(jclass clazz, jfieldID fieldID, jlong value);
void jnihelper_set_static_float_field(jclass clazz, jfieldID fieldID, jfloat value);
void jnihelper_set_static_double_field(jclass clazz, jfieldID fieldID, jdouble value);

// ===== 数组 =====
jsize jnihelper_get_array_length(jarray array);
jobject jnihelper_get_object_array_element(jobjectArray array, jsize index);
void jnihelper_set_object_array_element(jobjectArray array, jsize index, jobject value);

// ===== Unity 集成 =====
jobject jnihelper_get_current_activity();
jobject jnihelper_get_unity_surface();
struct ANativeWindow* jnihelper_get_unity_native_window();
jobject jnihelper_surface_to_native_window(jobject surface);

// ===== Input 事件 =====
uint32_t jnihelper_keyevent_get_unicode(AInputEvent *event);
void jnihelper_capture_input_event_unicode(JNIEnv* env, jobject inputEvent);
uint32_t jnihelper_poll_captured_unicode();

// ===== Data 通道（C# <-> Java） =====
void jnihelper_set_data(const char *key, jint *data, int len);
int jnihelper_get_data_len(const char *key);
jint* jnihelper_get_data_buf(const char *key);

#ifdef __cplusplus
}
#endif

#endif // JNIHELPER_H
