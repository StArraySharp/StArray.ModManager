#include <cstdint>
#include <android/log.h>

#include "unityresolve.h"

#define LOG_TAG "StArray.ModLoader.UnityResolve"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO,  LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

// 跟踪当前模式 (Init 时设置，避免访问私有 mode_)
static bool g_resolve_is_il2cpp = false;

// ============================================================================
// C ABI exports for P/Invoke from C# (Mono)
// 命名规范: modloader_resolve_xxx
// ============================================================================
extern "C" {

// ---- Init / Lifecycle ----

void GetAssemblyCount(){
    UnityResolve::Init(dlopen("libil2cpp.so",1),UnityResolve::Mode::Il2Cpp);
    LOGE("assembly count:%zu",UnityResolve::assembly.size());
}
/**
 * 初始化 UnityResolve 并遍历所有程序集。
 * @param hmodule  Mono 模块句柄 (通常为 dlopen("libmonosgen-2.0.so") 的返回值)
 * @param mode     0 = Mono, 1 = Il2Cpp
 * @return 0 成功
 */
int modloader_resolve_init(void *hmodule, int mode) {
    LOGI("UnityResolve::Init hmodule=%p mode=%d", hmodule, mode);
    g_resolve_is_il2cpp = (mode != 0);
    UnityResolve::Init(hmodule, static_cast<UnityResolve::Mode>(mode));
    return 0;
}

/**
 * 初始化 UnityResolve（Il2Cpp 模式，自动获取句柄）。
 * 内部使用 dlopen+RTLD_NOLOAD 获取已加载的 libil2cpp.so 句柄，
 * 避免 C# 侧传递的句柄导致新加载副本。
 * @return 0 成功
 */
int modloader_resolve_init_il2cpp(void) {
    void *hmodule = dlopen("libil2cpp.so", RTLD_LAZY | RTLD_NOLOAD);
    LOGE("modloader_resolve_init_il2cpp: dlopen handle=%p", hmodule);
    if (!hmodule) {
        // 回退：RTLD_NOLOAD 可能在一些旧版 Android 上不支持
        hmodule = dlopen("libil2cpp.so", RTLD_LAZY);
        LOGE("modloader_resolve_init_il2cpp: fallback handle=%p", hmodule);
    }
    if (!hmodule) return -1;
    g_resolve_is_il2cpp = true;
    UnityResolve::Init(hmodule, UnityResolve::Mode::Il2Cpp);
    return 0;
}

/**
 * 将当前线程附加到 Mono/Il2Cpp 域。
 */
void modloader_resolve_thread_attach(void) {
    UnityResolve::ThreadAttach();
}

/**
 * 将当前线程从 Mono/Il2Cpp 域分离。
 */
void modloader_resolve_thread_detach(void) {
    UnityResolve::ThreadDetach();
}

// ---- Assembly ----

/**
 * 按名称获取程序集。
 * @param name  程序集名称 (e.g., "UnityEngine.CoreModule.dll")
 * @return Assembly 指针 (不透明句柄)
 */
void *modloader_resolve_get_assembly(const char *name) {
    auto *asm_ = UnityResolve::Get(name);
    LOGI("GetAssembly(%s) = %p", name, (void*)asm_);
    return (void*)asm_;
}

/**
 * 获取程序集名称。
 */
const char *modloader_resolve_assembly_get_name(void *assembly) {
    if (!assembly) return "";
    return static_cast<UnityResolve::Assembly*>(assembly)->name.c_str();
}

/**
 * 获取程序集数量。
 */
int modloader_resolve_assembly_count(void) {
    return (int)UnityResolve::assembly.size();
}

/**
 * 按索引获取程序集。
 */
void *modloader_resolve_assembly_at(int index) {
    if (index < 0 || index >= (int)UnityResolve::assembly.size()) return nullptr;
    return (void*)UnityResolve::assembly[index];
}

// ---- Class ----

/**
 * 在程序集中按命名空间和类名查找类。
 * @param assembly    Assembly 指针
 * @param namespaze   命名空间 ("*" 表示任意)
 * @param name        类名
 * @return Class 指针 (不透明句柄)
 */
void *modloader_resolve_class_get(void *assembly, const char *namespaze, const char *name) {
    if (!assembly) return nullptr;
    auto *cls = static_cast<UnityResolve::Assembly*>(assembly)->Get(name, namespaze);
    LOGI("Class Get(%s.%s) = %p", namespaze, name, (void*)cls);
    return (void*)cls;
}

/**
 * 获取类名。
 */
const char *modloader_resolve_class_get_name(void *klass) {
    if (!klass) return "";
    return static_cast<UnityResolve::Class*>(klass)->name.c_str();
}

/**
 * 获取类命名空间。
 */
const char *modloader_resolve_class_get_namespace(void *klass) {
    if (!klass) return "";
    return static_cast<UnityResolve::Class*>(klass)->namespaze.c_str();
}

// ---- Method ----

/**
 * 按名称和方法参数数量查找方法。
 * @param klass       Class 指针
 * @param name         方法名
 * @param param_count  参数数量 (-1 表示不匹配)
 * @return Method 指针 (不透明句柄)
 */
void *modloader_resolve_method_get(void *klass, const char *name, int param_count) {
    if (!klass) return nullptr;
    auto *cls = static_cast<UnityResolve::Class*>(klass);
    // 用 args vector 进行匹配
    std::vector<std::string> args;
    if (param_count > 0) args.assign(param_count, "*");
    auto *method = cls->Get<UnityResolve::Method>(name, args);
    LOGI("Method Get(%s, %d args) = %p", name, param_count, (void*)method);
    return (void*)method;
}

/**
 * 获取方法名。
 */
const char *modloader_resolve_method_get_name(void *method) {
    if (!method) return "";
    return static_cast<UnityResolve::Method*>(method)->name.c_str();
}

/**
 * 编译方法 (Mono 模式下将 IL 编译为原生代码)。
 */
void modloader_resolve_method_compile(void *method) {
    if (!method) return;
    static_cast<UnityResolve::Method*>(method)->Compile();
}

/**
 * 获取方法原生函数指针。
 */
void *modloader_resolve_method_get_function(void *method) {
    if (!method) return nullptr;
    return static_cast<UnityResolve::Method*>(method)->function;
}

/**
 * 检查方法是否为静态方法。
 */
int modloader_resolve_method_is_static(void *method) {
    if (!method) return 0;
    return static_cast<UnityResolve::Method*>(method)->static_function ? 1 : 0;
}

// ---- Method Invoke (RuntimeInvoke) ----

/**
 * 通过 mono_runtime_invoke / il2cpp_runtime_invoke 调用托管方法。
 * @param method     Method 指针
 * @param obj        实例对象指针 (静态方法传 nullptr)
 * @param args       参数指针数组 (每个元素是 void* 指向托管对象或值类型)
 * @param arg_count  参数个数
 * @return 托管返回值 (MonoObject* / Il2CppObject*)，调用者需自行 Unbox
 */
void *modloader_resolve_method_runtime_invoke(void *method, void *obj, void **args, int arg_count) {
    if (!method) return nullptr;

    auto *m = static_cast<UnityResolve::Method*>(method);
    // 构建参数数组
    void *argArray[32] = {};
    int n = arg_count < 32 ? arg_count : 31;
    for (int i = 0; i < n; i++) argArray[i] = args[i];

    // il2cpp_runtime_invoke 和 mono_runtime_invoke 签名相同
    // il2cpp: void* il2cpp_runtime_invoke(MethodInfo*, void*, void**, MonoException**)
    if (g_resolve_is_il2cpp) {
        return UnityResolve::Invoke<void*>("il2cpp_runtime_invoke",
                                           m->address, obj, n > 0 ? argArray : nullptr, nullptr);
    } else {
        return UnityResolve::Invoke<void*>("mono_runtime_invoke",
                                           m->address, obj, n > 0 ? argArray : nullptr, nullptr);
    }
}

/**
 * 将托管对象 Unbox 为原语值。
 * @param obj  MonoObject* / Il2CppObject*
 * @return unboxed 指针
 */
void *modloader_resolve_object_unbox(void *obj) {
    if (!obj) return nullptr;
    if (g_resolve_is_il2cpp) {
        return UnityResolve::Invoke<void*>("il2cpp_object_unbox", obj);
    }
    return UnityResolve::Invoke<void*>("mono_object_unbox", obj);
}

// ---- Field ----

/**
 * 按名称查找字段。
 * @param klass  Class 指针
 * @param name   字段名
 * @return Field 指针 (不透明句柄)
 */
void *modloader_resolve_field_get(void *klass, const char *name) {
    if (!klass) return nullptr;
    auto *field = static_cast<UnityResolve::Class*>(klass)->Get<UnityResolve::Field>(name);
    LOGI("Field Get(%s) = %p", name, (void*)field);
    return (void*)field;
}

/**
 * 获取字段名。
 */
const char *modloader_resolve_field_get_name(void *field) {
    if (!field) return "";
    return static_cast<UnityResolve::Field*>(field)->name.c_str();
}

/**
 * 获取字段偏移量。
 */
int32_t modloader_resolve_field_get_offset(void *field) {
    if (!field) return 0;
    return static_cast<UnityResolve::Field*>(field)->offset;
}

/**
 * 检查字段是否为静态。
 */
int modloader_resolve_field_is_static(void *field) {
    if (!field) return 0;
    return static_cast<UnityResolve::Field*>(field)->static_field ? 1 : 0;
}

/**
 * 从实例对象读取字段值（按偏移量）。
 * @param obj     实例指针
 * @param offset  字段偏移量
 * @return 字段值 (原语类型直接返回；引用类型返回指针)
 */
void *modloader_resolve_field_get_value(void *obj, int32_t offset) {
    if (!obj || offset < 0) return nullptr;
    return *(void**)((uintptr_t)obj + offset);
}

/**
 * 向实例对象写入字段值（按偏移量）。
 */
void modloader_resolve_field_set_value(void *obj, int32_t offset, void *value) {
    if (!obj || offset < 0) return;
    *(void**)((uintptr_t)obj + offset) = value;
}

/**
 * 设置静态字段值。
 * @param field  Field 指针
 * @param value  值指针
 */
void modloader_resolve_field_set_static_value(void *field, void *value) {
    if (!field) return;
    auto *f = static_cast<UnityResolve::Field*>(field);
    f->SetStaticValue(&value);
}

/**
 * 获取静态字段值。
 * @param field  Field 指针
 * @param out    [out] 值输出指针
 */
void modloader_resolve_field_get_static_value(void *field, void *out) {
    if (!field || !out) return;
    auto *f = static_cast<UnityResolve::Field*>(field);
    f->GetStaticValue(out);
}

// ---- 直接通过类型和名称调用任意静态方法 (快捷方法) ----

/**
 * 快捷调用: 按程序集/类/方法名调用静态方法。
 * @param assembly_name   程序集名
 * @param namespaze       命名空间
 * @param class_name      类名
 * @param method_name     方法名
 * @param args            参数指针数组
 * @param arg_count       参数个数
 * @return 托管返回值 (MonoObject* / Il2CppObject*)
 */
void *modloader_resolve_invoke_static(const char *assembly_name,
                                       const char *namespaze,
                                       const char *class_name,
                                       const char *method_name,
                                       void **args,
                                       int arg_count) {
    auto *asm_ = UnityResolve::Get(assembly_name);
    if (!asm_) { LOGE("Assembly not found: %s", assembly_name); return nullptr; }

    auto *cls = asm_->Get(class_name, namespaze);
    if (!cls) { LOGE("Class not found: %s.%s", namespaze, class_name); return nullptr; }

    std::vector<std::string> argTypes;
    if (arg_count > 0) argTypes.assign(arg_count, "*");
    auto *method = cls->Get<UnityResolve::Method>(method_name, argTypes);
    if (!method) { LOGE("Method not found: %s", method_name); return nullptr; }

    return method->RuntimeInvoke<void*>((void*)nullptr, args);
}

} // extern "C"
