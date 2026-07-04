using System.Runtime.InteropServices;

namespace StArray.ModManager.Android.Native;

/// <summary>
/// JNI Helper Native 函数绑定
/// 调用 libmodmanager.so 中的 jnihelper C 函数
/// </summary>
public static class JniHelperNative
{
    private const string LibModManager = "modmanager";
    
    /// <summary>
    /// 获取 Unity Surface（C 实现，更快速）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_unity_surface")]
    public static extern IntPtr GetUnitySurface();
    
    /// <summary>
    /// 获取 Unity ANativeWindow（从 Surface 转换，可用于 ImGui）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_unity_native_window")]
    public static extern IntPtr GetUnityNativeWindow();
    
    /// <summary>
    /// 获取当前 Activity 或 Application
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_current_activity")]
    public static extern IntPtr GetCurrentActivity();
    
    /// <summary>
    /// 获取 JavaVM
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_jvm")]
    public static extern IntPtr GetJavaVM();
    
    /// <summary>
    /// 获取 JNIEnv（自动附加当前线程）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_env")]
    public static extern IntPtr GetJNIEnv();
    
    /// <summary>
    /// 查找 Java 类（返回全局引用）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_find_class")]
    public static extern IntPtr FindClass([MarshalAs(UnmanagedType.LPStr)] string className);
    
    /// <summary>
    /// 获取方法 ID
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_method_id")]
    public static extern IntPtr GetMethodID(IntPtr clazz, 
        [MarshalAs(UnmanagedType.LPStr)] string methodName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取静态方法 ID
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_static_method_id")]
    public static extern IntPtr GetStaticMethodID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string methodName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取字段 ID
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_field_id")]
    public static extern IntPtr GetFieldID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string fieldName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取静态字段 ID
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_static_field_id")]
    public static extern IntPtr GetStaticFieldID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string fieldName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 创建 Java String
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_new_string")]
    public static extern IntPtr NewString([MarshalAs(UnmanagedType.LPStr)] string str);
    
    /// <summary>
    /// Java String 转 C 字符串（需要调用者释放内存）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_string")]
    private static extern IntPtr GetStringInternal(IntPtr jstr);
    
    /// <summary>
    /// Java String 转 C# string（自动管理内存）
    /// </summary>
    public static string? GetString(IntPtr jstr)
    {
        if (jstr == IntPtr.Zero)
            return null;
        
        IntPtr ptr = GetStringInternal(jstr);
        if (ptr == IntPtr.Zero)
            return null;
        
        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            // 释放 C 分配的内存
            Marshal.FreeHGlobal(ptr);
        }
    }
    
    /// <summary>
    /// 删除本地引用
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_delete_local_ref")]
    public static extern void DeleteLocalRef(IntPtr obj);
    
    /// <summary>
    /// 删除全局引用
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_delete_global_ref")]
    public static extern void DeleteGlobalRef(IntPtr obj);
    
    /// <summary>
    /// 检查并清除异常
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_check_exception")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool CheckException();

    /// <summary>
    /// 调用 Java 对象方法（返回对象）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_call_object_method")]
    public static extern IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID);

    /// <summary>
    /// 调用 Java 静态方法（返回对象）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_call_static_object_method")]
    public static extern IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID);

    /// <summary>
    /// 获取静态对象字段
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_static_object_field")]
    public static extern IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID);

    /// <summary>
    /// 获取对象实例字段
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_object_field")]
    public static extern IntPtr GetObjectField(IntPtr obj, IntPtr fieldID);

    /// <summary>
    /// 获取对象的 Java 类
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_object_class")]
    public static extern IntPtr GetObjectClass(IntPtr obj);

    /// <summary>
    /// Surface 对象转 ANativeWindow（指针形式返回）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_surface_to_native_window")]
    public static extern IntPtr SurfaceToNativeWindow(IntPtr surface);

    /// <summary>
    /// 从 AInputEvent* 提取 Unicode 字符（JNI KeyEvent.getUnicodeChar）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_keyevent_get_unicode")]
    public static extern uint KeyEventGetUnicode(IntPtr keyEvent);

    /// <summary>
    /// C# → C: 写入 int[] 到指定 key（C# SetData → C buffer → Java nativeGetData）
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_set_data")]
    public static extern void SetData(
        [MarshalAs(UnmanagedType.LPStr)] string key, IntPtr data, int len);

    /// <summary>
    /// C ←: 获取指定 key 的数据长度
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_data_len")]
    public static extern int GetDataLength([MarshalAs(UnmanagedType.LPStr)] string key);

    /// <summary>
    /// C ←: 获取指定 key 的数据 buffer 指针
    /// </summary>
    [DllImport(LibModManager, EntryPoint = "jnihelper_get_data_buf")]
    public static extern IntPtr GetDataBuffer([MarshalAs(UnmanagedType.LPStr)] string key);
}
