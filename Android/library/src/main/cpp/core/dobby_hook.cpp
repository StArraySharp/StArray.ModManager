#include <jni.h>
#include <cstdint>
#include <android/log.h>

#include <dobby.h>

#define LOG_TAG "StArray.ModManager.Dobby"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO,  LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

// ============================================================================
// C ABI exports for P/Invoke from C# (Mono)
// 命名规范: modmanager_dobby_xxx
// C# 通过 [DllImport("modmanager")] 直接调用
// ============================================================================
extern "C" {

/**
 * DobbyHook — 安装 inline hook。
 * @param address        目标函数地址
 * @param replace_func   替换函数地址
 * @param origin_func    [out] 保存原函数地址的指针
 * @return 0 成功，非 0 失败
 */
int modmanager_dobby_hook(void *address, void *replace_func, void **origin_func) {
    LOGI("DobbyHook at %p, replace=%p", address, replace_func);
    int ret = DobbyHook(address, (dobby_dummy_func_t)replace_func, (dobby_dummy_func_t *)origin_func);
    if (ret != 0) LOGE("DobbyHook failed at %p, ret=%d", address, ret);
    return ret;
}

/**
 * DobbyInstrument — 安装动态指令插桩。
 * @param address      目标函数地址
 * @param pre_handler  前置回调 (dobby_instrument_callback_t)
 * @return 0 成功
 */
int modmanager_dobby_instrument(void *address, void *pre_handler) {
    LOGI("DobbyInstrument at %p, handler=%p", address, pre_handler);
    return DobbyInstrument(address, (dobby_instrument_callback_t)pre_handler);
}

/**
 * DobbyDestroy — 移除 hook 并恢复原函数。
 * @param address  被 hook 的函数地址
 * @return 0 成功
 */
int modmanager_dobby_destroy(void *address) {
    LOGI("DobbyDestroy at %p", address);
    return DobbyDestroy(address);
}

/**
 * DobbySymbolResolver — 按 image 名称和 symbol 名称解析函数地址。
 * @param image_name   动态库名 (e.g., "libil2cpp.so")
 * @param symbol_name   符号名
 * @return 符号地址，失败返回 nullptr
 */
void *modmanager_dobby_symbol_resolver(const char *image_name, const char *symbol_name) {
    void *addr = DobbySymbolResolver(image_name, symbol_name);
    LOGI("DobbySymbolResolver(%s, %s) = %p", image_name, symbol_name, addr);
    return addr;
}

/**
 * DobbyCodePatch — 内存代码补丁。
 * @param address      目标地址
 * @param buffer       补丁数据
 * @param buffer_size  补丁数据大小
 * @return 0 成功
 */
int modmanager_dobby_code_patch(void *address, const uint8_t *buffer, uint32_t buffer_size) {
    LOGI("DobbyCodePatch at %p, size=%u", address, buffer_size);
    return DobbyCodePatch(address, (uint8_t *)buffer, buffer_size);
}

/**
 * DobbyGetVersion — 获取 Dobby 版本字符串。
 */
const char *modmanager_dobby_get_version(void) {
    return DobbyGetVersion();
}

/**
 * modmanager_log_write — write a line to Android logcat.
 * Called from C# via [DllImport("modmanager")].
 */
void modmanager_log_write(int prio, const char *tag, const char *msg) {
    __android_log_write(prio, tag ? tag : "ModManager", msg ? msg : "");
}

} // extern "C"
