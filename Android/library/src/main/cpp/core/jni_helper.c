#include <jni.h>
#include <android/log.h>
#include <android/native_window_jni.h>
#include <android/input.h>
#include <string.h>
#include <stdlib.h>

#define LOG_TAG "JNIHelper"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

// 全局 JavaVM 指针
static JavaVM *g_jvm = NULL;
static JNIEnv *g_env = NULL;

/**
 * 保存 JavaVM 指针（在 JNI_OnLoad 中调用）
 */
void jnihelper_set_jvm(JavaVM *vm) {
    g_jvm = vm;
    LOGI("JavaVM set: %p", vm);
}

/**
 * 获取 JavaVM
 */
JavaVM* jnihelper_get_jvm() {
    return g_jvm;
}

/**
 * 获取 JNIEnv（自动附加当前线程）
 */
JNIEnv* jnihelper_get_env() {
    if (g_jvm == NULL) {
        LOGE("JavaVM not initialized");
        return NULL;
    }
    
    JNIEnv *env = NULL;
    int status = (*g_jvm)->GetEnv(g_jvm, (void**)&env, JNI_VERSION_1_6);
    
    if (status == JNI_EDETACHED) {
        // 当前线程未附加，需要附加
        status = (*g_jvm)->AttachCurrentThread(g_jvm, &env, NULL);
        if (status != JNI_OK) {
            LOGE("Failed to attach current thread: %d", status);
            return NULL;
        }
        LOGI("Thread attached to JavaVM");
    } else if (status != JNI_OK) {
        LOGE("Failed to get JNIEnv: %d", status);
        return NULL;
    }
    
    return env;
}

/**
 * 查找 Java 类
 */
jclass jnihelper_find_class(const char *class_name) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        return NULL;
    }
    
    jclass clazz = (*env)->FindClass(env, class_name);
    if (clazz == NULL) {
        LOGE("Failed to find class: %s", class_name);
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return NULL;
    }
    
    // 创建全局引用
    jclass global_clazz = (*env)->NewGlobalRef(env, clazz);
    (*env)->DeleteLocalRef(env, clazz);
    
    return global_clazz;
}

/**
 * 获取方法 ID
 */
jmethodID jnihelper_get_method_id(jclass clazz, const char *method_name, const char *signature) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL) {
        return NULL;
    }
    
    jmethodID method = (*env)->GetMethodID(env, clazz, method_name, signature);
    if (method == NULL) {
        LOGE("Failed to get method: %s%s", method_name, signature);
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return NULL;
    }
    
    return method;
}

/**
 * 获取静态方法 ID
 */
jmethodID jnihelper_get_static_method_id(jclass clazz, const char *method_name, const char *signature) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL) {
        return NULL;
    }
    
    jmethodID method = (*env)->GetStaticMethodID(env, clazz, method_name, signature);
    if (method == NULL) {
        LOGE("Failed to get static method: %s%s", method_name, signature);
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return NULL;
    }
    
    return method;
}

/**
 * 获取字段 ID
 */
jfieldID jnihelper_get_field_id(jclass clazz, const char *field_name, const char *signature) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL) {
        return NULL;
    }
    
    jfieldID field = (*env)->GetFieldID(env, clazz, field_name, signature);
    if (field == NULL) {
        LOGE("Failed to get field: %s:%s", field_name, signature);
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return NULL;
    }
    
    return field;
}

/**
 * 获取静态字段 ID
 */
jfieldID jnihelper_get_static_field_id(jclass clazz, const char *field_name, const char *signature) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL) {
        return NULL;
    }
    
    jfieldID field = (*env)->GetStaticFieldID(env, clazz, field_name, signature);
    if (field == NULL) {
        LOGE("Failed to get static field: %s:%s", field_name, signature);
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return NULL;
    }
    
    return field;
}

/**
 * 创建 Java String
 */
jstring jnihelper_new_string(const char *str) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        return NULL;
    }
    
    jstring jstr = (*env)->NewStringUTF(env, str);
    if (jstr == NULL) {
        LOGE("Failed to create Java string");
        return NULL;
    }
    
    return jstr;
}

/**
 * Java String 转 C string（需要调用者释放内存）
 */
char* jnihelper_get_string(jstring jstr) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || jstr == NULL) {
        return NULL;
    }
    
    const char *str = (*env)->GetStringUTFChars(env, jstr, NULL);
    if (str == NULL) {
        return NULL;
    }
    
    // 复制字符串
    char *result = strdup(str);
    (*env)->ReleaseStringUTFChars(env, jstr, str);
    
    return result;
}

/**
 * 删除本地引用
 */
void jnihelper_delete_local_ref(jobject obj) {
    JNIEnv *env = jnihelper_get_env();
    if (env != NULL && obj != NULL) {
        (*env)->DeleteLocalRef(env, obj);
    }
}

/**
 * 删除全局引用
 */
void jnihelper_delete_global_ref(jobject obj) {
    JNIEnv *env = jnihelper_get_env();
    if (env != NULL && obj != NULL) {
        (*env)->DeleteGlobalRef(env, obj);
    }
}

/**
 * 检查并清除异常
 */
jboolean jnihelper_check_exception() {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        return JNI_FALSE;
    }
    
    if ((*env)->ExceptionCheck(env)) {
        LOGE("JNI Exception occurred:");
        (*env)->ExceptionDescribe(env);
        (*env)->ExceptionClear(env);
        return JNI_TRUE;
    }
    
    return JNI_FALSE;
}

/**
 * 获取当前 Activity
 */
jobject jnihelper_get_current_activity() {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        return NULL;
    }
    
    // 尝试方法1: UnityPlayer.currentActivity
    jclass unity_player_class = (*env)->FindClass(env, "com/unity3d/player/UnityPlayer");
    if (unity_player_class != NULL) {
        jfieldID field = (*env)->GetStaticFieldID(env, unity_player_class, "currentActivity", "Landroid/app/Activity;");
        if (field != NULL) {
            jobject activity = (*env)->GetStaticObjectField(env, unity_player_class, field);
            (*env)->DeleteLocalRef(env, unity_player_class);
            if (activity != NULL) {
                LOGI("Got activity from UnityPlayer.currentActivity");
                return activity;
            }
        }
        (*env)->DeleteLocalRef(env, unity_player_class);
    }
    
    (*env)->ExceptionClear(env);
    
    // 尝试方法2: ActivityThread
    jclass activity_thread_class = (*env)->FindClass(env, "android/app/ActivityThread");
    if (activity_thread_class == NULL) {
        (*env)->ExceptionClear(env);
        LOGE("Failed to find ActivityThread class");
        return NULL;
    }
    
    jmethodID current_activity_thread = (*env)->GetStaticMethodID(env, activity_thread_class, 
        "currentActivityThread", "()Landroid/app/ActivityThread;");
    if (current_activity_thread == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, activity_thread_class);
        LOGE("Failed to get currentActivityThread method");
        return NULL;
    }
    
    jobject activity_thread = (*env)->CallStaticObjectMethod(env, activity_thread_class, current_activity_thread);
    if (activity_thread == NULL) {
        (*env)->DeleteLocalRef(env, activity_thread_class);
        return NULL;
    }
    
    jmethodID get_application = (*env)->GetMethodID(env, activity_thread_class, 
        "getApplication", "()Landroid/app/Application;");
    if (get_application == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, activity_thread);
        (*env)->DeleteLocalRef(env, activity_thread_class);
        LOGE("Failed to get getApplication method");
        return NULL;
    }
    
    jobject application = (*env)->CallObjectMethod(env, activity_thread, get_application);
    
    (*env)->DeleteLocalRef(env, activity_thread);
    (*env)->DeleteLocalRef(env, activity_thread_class);
    
    if (application != NULL) {
        LOGI("Got Application context from ActivityThread");
    }
    
    return application;
}

// ===== 通用 JNI 调用辅助（供 C# P/Invoke 使用） =====

jobject jnihelper_call_object_method(jobject obj, jmethodID methodID) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || obj == NULL || methodID == NULL) return NULL;
    return (*env)->CallObjectMethod(env, obj, methodID);
}

jobject jnihelper_call_static_object_method(jclass clazz, jmethodID methodID) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL || methodID == NULL) return NULL;
    return (*env)->CallStaticObjectMethod(env, clazz, methodID);
}

jobject jnihelper_get_static_object_field(jclass clazz, jfieldID fieldID) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || clazz == NULL || fieldID == NULL) return NULL;
    return (*env)->GetStaticObjectField(env, clazz, fieldID);
}

jobject jnihelper_get_object_field(jobject obj, jfieldID fieldID) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || obj == NULL || fieldID == NULL) return NULL;
    return (*env)->GetObjectField(env, obj, fieldID);
}

jclass jnihelper_get_object_class(jobject obj) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || obj == NULL) return NULL;
    return (*env)->GetObjectClass(env, obj);
}

jobject jnihelper_surface_to_native_window(jobject surface) {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL || surface == NULL) return NULL;
    ANativeWindow *window = ANativeWindow_fromSurface(env, surface);
    return (jobject)window;
}

/**
 * 从 AInputEvent (KeyEvent) 提取 Unicode 字符
 * 参考 ImGui example_android_opengl3 的 dispatchKeyEvent → getUnicodeChar(metaState)
 */
uint32_t jnihelper_keyevent_get_unicode(AInputEvent *event) {
    if (!event) return 0;
    int32_t action = AKeyEvent_getAction(event);
    if (action != AKEY_EVENT_ACTION_DOWN) return 0;
    
    JNIEnv *env = jnihelper_get_env();
    if (!env) return 0;
    
    int32_t keyCode = AKeyEvent_getKeyCode(event);
    int32_t metaState = AKeyEvent_getMetaState(event);
    
    jclass keyEventClass = (*env)->FindClass(env, "android/view/KeyEvent");
    if (!keyEventClass) { (*env)->ExceptionClear(env); return 0; }
    
    jmethodID getUnicodeChar = (*env)->GetMethodID(env, keyEventClass, "getUnicodeChar", "(I)I");
    if (!getUnicodeChar) { (*env)->ExceptionClear(env); (*env)->DeleteLocalRef(env, keyEventClass); return 0; }
    
    // KeyEvent.ACTION_DOWN = 0
    jmethodID ctor = (*env)->GetMethodID(env, keyEventClass, "<init>", "(II)V");
    jobject keyEventObj = (*env)->NewObject(env, keyEventClass, ctor, (jint)0, keyCode);
    
    jint unicode = 0;
    if (keyEventObj) {
        unicode = (*env)->CallIntMethod(env, keyEventObj, getUnicodeChar, metaState);
        (*env)->DeleteLocalRef(env, keyEventObj);
    }
    (*env)->DeleteLocalRef(env, keyEventClass);
    return (uint32_t)unicode;
}

/**
 * 获取 Unity Surface
 */
jobject jnihelper_get_unity_surface() {
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        return NULL;
    }
    
    // 1. 查找 UnityPlayer 类
    jclass unity_player_class = (*env)->FindClass(env, "com/unity3d/player/UnityPlayer");
    if (unity_player_class == NULL) {
        (*env)->ExceptionClear(env);
        LOGE("Failed to find UnityPlayer class");
        return NULL;
    }
    
    // 2. 获取静态字段 currentActivity
    jfieldID current_activity_field = (*env)->GetStaticFieldID(env, unity_player_class, 
        "currentActivity", "Landroid/app/Activity;");
    if (current_activity_field == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, unity_player_class);
        LOGE("Failed to get currentActivity field");
        return NULL;
    }
    
    jobject activity = (*env)->GetStaticObjectField(env, unity_player_class, current_activity_field);
    if (activity == NULL) {
        (*env)->DeleteLocalRef(env, unity_player_class);
        LOGE("currentActivity is null");
        return NULL;
    }
    
    // 3. 从 Activity 获取 mUnityPlayer 实例字段
    // 支持两种类型以兼容不同 Unity 版本
    jclass activity_class = (*env)->GetObjectClass(env, activity);
    jfieldID unity_player_field = NULL;
    
    // 先尝试新版本类型 UnityPlayerForActivityOrService
    unity_player_field = (*env)->GetFieldID(env, activity_class, 
        "mUnityPlayer", "Lcom/unity3d/player/UnityPlayerForActivityOrService;");
    
    if (unity_player_field == NULL) {
        (*env)->ExceptionClear(env);
        LOGI("Trying legacy UnityPlayer type...");
        
        // 再尝试旧版本类型 UnityPlayer
        unity_player_field = (*env)->GetFieldID(env, activity_class, 
            "mUnityPlayer", "Lcom/unity3d/player/UnityPlayer;");
        
        if (unity_player_field == NULL) {
            (*env)->ExceptionClear(env);
            (*env)->DeleteLocalRef(env, activity_class);
            (*env)->DeleteLocalRef(env, activity);
            (*env)->DeleteLocalRef(env, unity_player_class);
            LOGE("Failed to get mUnityPlayer field (tried both types)");
            return NULL;
        }
    }
    
    jobject unity_player = (*env)->GetObjectField(env, activity, unity_player_field);
    (*env)->DeleteLocalRef(env, activity_class);
    (*env)->DeleteLocalRef(env, activity);
    
    if (unity_player == NULL) {
        (*env)->DeleteLocalRef(env, unity_player_class);
        LOGE("mUnityPlayer is null");
        return NULL;
    }
    
    // 4. 获取 UnityPlayer 实例的实际类并调用 getSurfaceView()
    jclass unity_player_instance_class = (*env)->GetObjectClass(env, unity_player);
    jmethodID get_surface_view = (*env)->GetMethodID(env, unity_player_instance_class, 
        "getSurfaceView", "()Landroid/view/SurfaceView;");
    if (get_surface_view == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, unity_player);
        (*env)->DeleteLocalRef(env, unity_player_instance_class);
        (*env)->DeleteLocalRef(env, unity_player_class);
        LOGE("Failed to get getSurfaceView method");
        return NULL;
    }
    
    jobject surface_view = (*env)->CallObjectMethod(env, unity_player, get_surface_view);
    (*env)->DeleteLocalRef(env, unity_player);
    (*env)->DeleteLocalRef(env, unity_player_instance_class);
    (*env)->DeleteLocalRef(env, unity_player_class);
    
    if (surface_view == NULL) {
        LOGE("getSurfaceView returned null");
        return NULL;
    }
    
    // 5. 调用 SurfaceView.getHolder()
    jclass surface_view_class = (*env)->FindClass(env, "android/view/SurfaceView");
    if (surface_view_class == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, surface_view);
        LOGE("Failed to find SurfaceView class");
        return NULL;
    }
    
    jmethodID get_holder = (*env)->GetMethodID(env, surface_view_class, 
        "getHolder", "()Landroid/view/SurfaceHolder;");
    if (get_holder == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, surface_view);
        (*env)->DeleteLocalRef(env, surface_view_class);
        LOGE("Failed to get getHolder method");
        return NULL;
    }
    
    jobject surface_holder = (*env)->CallObjectMethod(env, surface_view, get_holder);
    (*env)->DeleteLocalRef(env, surface_view);
    (*env)->DeleteLocalRef(env, surface_view_class);
    
    if (surface_holder == NULL) {
        LOGE("getHolder returned null");
        return NULL;
    }
    
    // 6. 调用 SurfaceHolder.getSurface()
    jclass surface_holder_class = (*env)->FindClass(env, "android/view/SurfaceHolder");
    if (surface_holder_class == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, surface_holder);
        LOGE("Failed to find SurfaceHolder class");
        return NULL;
    }
    
    jmethodID get_surface = (*env)->GetMethodID(env, surface_holder_class, 
        "getSurface", "()Landroid/view/Surface;");
    if (get_surface == NULL) {
        (*env)->ExceptionClear(env);
        (*env)->DeleteLocalRef(env, surface_holder);
        (*env)->DeleteLocalRef(env, surface_holder_class);
        LOGE("Failed to get getSurface method");
        return NULL;
    }
    
    jobject surface = (*env)->CallObjectMethod(env, surface_holder, get_surface);
    (*env)->DeleteLocalRef(env, surface_holder);
    (*env)->DeleteLocalRef(env, surface_holder_class);
    
    if (surface != NULL) {
        LOGI("Successfully got Unity Surface: %p", surface);
    } else {
        LOGE("getSurface returned null");
    }
    
    return surface;
}

/**
 * 获取 Unity ANativeWindow（从 Surface 对象转换）
 */
ANativeWindow* jnihelper_get_unity_native_window() {
    jobject surface = jnihelper_get_unity_surface();
    if (surface == NULL) {
        LOGE("Failed to get Unity Surface");
        return NULL;
    }
    
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        jnihelper_delete_local_ref(surface);
        return NULL;
    }
    
    // 使用 ANativeWindow_fromSurface 转换 Surface 为 ANativeWindow*
    ANativeWindow *window = ANativeWindow_fromSurface(env, surface);
    jnihelper_delete_local_ref(surface);
    
    if (window != NULL) {
        LOGI("Successfully converted Surface to ANativeWindow: %p", window);
    } else {
        LOGE("ANativeWindow_fromSurface returned NULL");
    }
    
    return window;
}

// ===== Unicode 字符捕获（从 Java InputEvent 提取） =====

static uint32_t g_captured_unicode = 0;

/**
 * 从 Java InputEvent 对象提取 Unicode 字符（用于 nativeInjectEvent hook）
 * 调用后会缓存字符，通过 poll_captured_unicode 获取
 */
void jnihelper_capture_input_event_unicode(JNIEnv* env, jobject inputEvent) {
    if (!env || !inputEvent) return;
    
    jclass cls = (*env)->GetObjectClass(env, inputEvent);
    if (!cls) { (*env)->ExceptionClear(env); return; }
    
    // 只处理 ACTION_DOWN (0)
    jmethodID getAction = (*env)->GetMethodID(env, cls, "getAction", "()I");
    if (!getAction) { (*env)->ExceptionClear(env); (*env)->DeleteLocalRef(env, cls); return; }
    if ((*env)->CallIntMethod(env, inputEvent, getAction) != 0) {
        (*env)->DeleteLocalRef(env, cls);
        return;
    }
    
    jmethodID getUnicodeChar = (*env)->GetMethodID(env, cls, "getUnicodeChar", "(I)I");
    jmethodID getMetaState = (*env)->GetMethodID(env, cls, "getMetaState", "()I");
    if (getUnicodeChar && getMetaState) {
        jint metaState = (*env)->CallIntMethod(env, inputEvent, getMetaState);
        jint unicode = (*env)->CallIntMethod(env, inputEvent, getUnicodeChar, metaState);
        if (unicode >= ' ') g_captured_unicode = (uint32_t)unicode;
    }
    (*env)->ExceptionClear(env);
    (*env)->DeleteLocalRef(env, cls);
}

/**
 * 轮询捕获的 Unicode 字符（获取后清零）
 */
uint32_t jnihelper_poll_captured_unicode() {
    uint32_t u = g_captured_unicode;
    g_captured_unicode = 0;
    return u;
}

// ===== Java ↔ C ↔ C# 数据传输（字符串 key 路由） =====

#define DATA_SLOTS     8
#define DATA_BUF_SIZE  4096
#define KEY_MAX_LEN    64

typedef struct {
    char  key[KEY_MAX_LEN];
    jint  buf[DATA_BUF_SIZE];
    int   len;
    int   dirty;  // 1 = Java 写入后待 C# 读取
} DataSlot;

static DataSlot s_slots[DATA_SLOTS];

/** 查找或分配 key 对应的 slot */
static DataSlot* find_slot(const char *key) {
    if (!key || !*key) return NULL;
    // 查找已有
    for (int i = 0; i < DATA_SLOTS; i++) {
        if (s_slots[i].key[0] && strcmp(s_slots[i].key, key) == 0)
            return &s_slots[i];
    }
    // 分配新 slot
    for (int i = 0; i < DATA_SLOTS; i++) {
        if (s_slots[i].key[0] == 0) {
            strncpy(s_slots[i].key, key, KEY_MAX_LEN - 1);
            return &s_slots[i];
        }
    }
    return NULL;
}

// === JNI 入口 (Java 调用) ===

/** Java → C: nativeSetData(String key, int[] data) */
JNIEXPORT void JNICALL
Java_starray_android_modloader_ModManagerUtils_nativeSetData(
    JNIEnv *env, jclass unused, jstring key, jintArray data) {
    const char *k = (*env)->GetStringUTFChars(env, key, NULL);
    DataSlot *s = find_slot(k);
    (*env)->ReleaseStringUTFChars(env, key, k);
    if (!s) return;
    
    if (data == NULL) {
        s->len = 0; s->dirty = 1;
        return;
    }
    jsize len = (*env)->GetArrayLength(env, data);
    if (len > DATA_BUF_SIZE) len = DATA_BUF_SIZE;
    (*env)->GetIntArrayRegion(env, data, 0, len, s->buf);
    s->len = (int)len;
    s->dirty = 1;
}

/** Java ← C: nativeGetData(String key) → int[] */
JNIEXPORT jintArray JNICALL
Java_starray_android_modloader_ModManagerUtils_nativeGetData(
    JNIEnv *env, jclass unused, jstring key) {
    const char *k = (*env)->GetStringUTFChars(env, key, NULL);
    DataSlot *s = find_slot(k);
    (*env)->ReleaseStringUTFChars(env, key, k);
    if (!s || s->len <= 0) return NULL;
    
    jintArray arr = (*env)->NewIntArray(env, s->len);
    (*env)->SetIntArrayRegion(env, arr, 0, s->len, s->buf);
    s->dirty = 0;
    return arr;
}

// === C 导出 (C# P/Invoke) ===

/** C# → C: 写入 int[] 到指定 key */
void jnihelper_set_data(const char *key, jint *data, int len) {
    DataSlot *s = find_slot(key);
    if (!s) return;
    if (data == NULL || len <= 0) { s->len = 0; return; }
    if (len > DATA_BUF_SIZE) len = DATA_BUF_SIZE;
    memcpy(s->buf, data, len * sizeof(jint));
    s->len = len;
}

/** C# ← C: 获取数据长度 */
int jnihelper_get_data_len(const char *key) {
    DataSlot *s = find_slot(key);
    return s ? s->len : 0;
}

/** C# ← C: 获取数据 buffer 指针 */
jint* jnihelper_get_data_buf(const char *key) {
    DataSlot *s = find_slot(key);
    return s ? s->buf : NULL;
}

/**
 * JNI_OnLoad - 库加载时的入口点
 */
JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM *vm, void *reserved) {
    LOGI("JNI_OnLoad called, JavaVM: %p", vm);
    
    // 保存 JavaVM 指针
    jnihelper_set_jvm(vm);
    
    // 获取 JNIEnv 测试
    JNIEnv *env = jnihelper_get_env();
    if (env == NULL) {
        LOGE("Failed to get JNIEnv in JNI_OnLoad");
        return JNI_ERR;
    }
    
    LOGI("JNI Helper initialized successfully");
    
    return JNI_VERSION_1_6;
}

/**
 * JNI_OnUnload - 库卸载时调用
 */
JNIEXPORT void JNICALL JNI_OnUnload(JavaVM *vm, void *reserved) {
    LOGI("JNI_OnUnload called");
    
    // 清理全局引用（如果有缓存的类引用）
    g_jvm = NULL;
    g_env = NULL;
}

