using System;
using System.Runtime.InteropServices;

namespace StArray.ModLoader.PInvoke;

/// <summary>
/// JNI Helper Native 函数绑定
/// 调用 libmodloader.so 中的 jnihelper C 函数
/// </summary>
public static class JniHelperNative
{
    private const string LibModLoader = "modloader";
    
    /// <summary>
    /// 获取 Unity Surface（C 实现，更快速）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_unity_surface")]
    public static extern IntPtr GetUnitySurface();
    
    /// <summary>
    /// 获取 Unity ANativeWindow（从 Surface 转换，可用于 ImGui）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_unity_native_window")]
    public static extern IntPtr GetUnityNativeWindow();
    
    /// <summary>
    /// 获取当前 Activity 或 Application
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_current_activity")]
    public static extern IntPtr GetCurrentActivity();
    
    /// <summary>
    /// 获取 JavaVM
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_jvm")]
    public static extern IntPtr GetJavaVM();
    
    /// <summary>
    /// 获取 JNIEnv（自动附加当前线程）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_env")]
    public static extern IntPtr GetJNIEnv();
    
    /// <summary>
    /// 查找 Java 类（返回全局引用）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_find_class")]
    public static extern IntPtr FindClass([MarshalAs(UnmanagedType.LPStr)] string className);
    
    /// <summary>
    /// 获取方法 ID
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_method_id")]
    public static extern IntPtr GetMethodID(IntPtr clazz, 
        [MarshalAs(UnmanagedType.LPStr)] string methodName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取静态方法 ID
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_static_method_id")]
    public static extern IntPtr GetStaticMethodID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string methodName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取字段 ID
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_field_id")]
    public static extern IntPtr GetFieldID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string fieldName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 获取静态字段 ID
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_static_field_id")]
    public static extern IntPtr GetStaticFieldID(IntPtr clazz,
        [MarshalAs(UnmanagedType.LPStr)] string fieldName,
        [MarshalAs(UnmanagedType.LPStr)] string signature);
    
    /// <summary>
    /// 创建 Java String
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_new_string")]
    public static extern IntPtr NewString([MarshalAs(UnmanagedType.LPStr)] string str);
    
    /// <summary>
    /// Java String 转 C 字符串（需要调用者释放内存）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_string")]
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
    [DllImport(LibModLoader, EntryPoint = "jnihelper_delete_local_ref")]
    public static extern void DeleteLocalRef(IntPtr obj);
    
    /// <summary>
    /// 删除全局引用
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_delete_global_ref")]
    public static extern void DeleteGlobalRef(IntPtr obj);
    
    /// <summary>
    /// 检查并清除异常
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_check_exception")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool CheckException();

    /// <summary>
    /// 调用 Java 对象方法（返回对象）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_call_object_method")]
    public static extern IntPtr CallObjectMethod(IntPtr obj, IntPtr methodID);

    /// <summary>
    /// 调用 Java 静态方法（返回对象）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_call_static_object_method")]
    public static extern IntPtr CallStaticObjectMethod(IntPtr clazz, IntPtr methodID);

    /// <summary>
    /// 获取静态对象字段
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_static_object_field")]
    public static extern IntPtr GetStaticObjectField(IntPtr clazz, IntPtr fieldID);

    /// <summary>
    /// 获取对象实例字段
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_object_field")]
    public static extern IntPtr GetObjectField(IntPtr obj, IntPtr fieldID);

    /// <summary>
    /// 获取对象的 Java 类
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_object_class")]
    public static extern IntPtr GetObjectClass(IntPtr obj);

    /// <summary>
    /// Surface 对象转 ANativeWindow（指针形式返回）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_surface_to_native_window")]
    public static extern IntPtr SurfaceToNativeWindow(IntPtr surface);

    /// <summary>
    /// 从 AInputEvent* 提取 Unicode 字符（JNI KeyEvent.getUnicodeChar）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_keyevent_get_unicode")]
    public static extern uint KeyEventGetUnicode(IntPtr keyEvent);

    /// <summary>
    /// C# → C: 写入 int[] 到指定 key（C# SetData → C buffer → Java nativeGetData）
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_set_data")]
    public static extern void SetData(
        [MarshalAs(UnmanagedType.LPStr)] string key, IntPtr data, int len);

    /// <summary>
    /// C ←: 获取指定 key 的数据长度
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_data_len")]
    public static extern int GetDataLength([MarshalAs(UnmanagedType.LPStr)] string key);

    /// <summary>
    /// C ←: 获取指定 key 的数据 buffer 指针
    /// </summary>
    [DllImport(LibModLoader, EntryPoint = "jnihelper_get_data_buf")]
    public static extern IntPtr GetDataBuffer([MarshalAs(UnmanagedType.LPStr)] string key);
}

/// <summary>
/// JNI Helper 高级封装
/// 结合 Native C 实现和 C# JNI 封装
/// </summary>
public static class JniHelperMixed
{
    /// <summary>
    /// 获取 Unity Surface（优先使用 Native C 实现）
    /// </summary>
    public static IntPtr GetUnitySurface()
    {
        try
        {
            // 优先使用 C 实现（更快）
            var surface = JniHelperNative.GetUnitySurface();
            
            if (surface != IntPtr.Zero)
            {
                AndroidLog.Info("JniHelperMixed", $"Got Unity Surface from native: 0x{surface:X}");
            }
            
            return surface;
        }
        catch (Exception ex)
        {
            AndroidLog.Error("JniHelperMixed", $"GetUnitySurface error: {ex}");
            return IntPtr.Zero;
        }
    }
    
    /// <summary>
    /// 获取当前 Activity
    /// </summary>
    public static IntPtr GetCurrentActivity()
    {
        try
        {
            var activity = JniHelperNative.GetCurrentActivity();
            
            if (activity != IntPtr.Zero)
            {
                AndroidLog.Info("JniHelperMixed", $"Got Activity: 0x{activity:X}");
            }
            
            return activity;
        }
        catch (Exception ex)
        {
            AndroidLog.Error("JniHelperMixed", $"GetCurrentActivity error: {ex}");
            return IntPtr.Zero;
        }
    }
    
    /// <summary>
    /// 显示 Toast（使用 Native JNI helper）
    /// </summary>
    public static void ShowToast(string message)
    {
        try
        {
            var env = JniHelperNative.GetJNIEnv();
            if (env == IntPtr.Zero)
            {
                AndroidLog.Error("JniHelperMixed", "Failed to get JNIEnv");
                return;
            }
            
            // 获取 Context
            var context = JniHelperNative.GetCurrentActivity();
            if (context == IntPtr.Zero)
            {
                AndroidLog.Error("JniHelperMixed", "Failed to get Context");
                return;
            }
            
            // 查找 Toast 类
            var toastClass = JniHelperNative.FindClass("android/widget/Toast");
            if (toastClass == IntPtr.Zero)
            {
                AndroidLog.Error("JniHelperMixed", "Failed to find Toast class");
                JniHelperNative.DeleteLocalRef(context);
                return;
            }
            
            // 获取 makeText 方法
            var makeTextMethod = JniHelperNative.GetStaticMethodID(toastClass,
                "makeText", "(Landroid/content/Context;Ljava/lang/CharSequence;I)Landroid/widget/Toast;");
            
            if (makeTextMethod == IntPtr.Zero)
            {
                AndroidLog.Error("JniHelperMixed", "Failed to get makeText method");
                JniHelperNative.DeleteGlobalRef(toastClass);
                JniHelperNative.DeleteLocalRef(context);
                return;
            }
            
            // 创建 Java String
            var javaMessage = JniHelperNative.NewString(message);
            
            // 调用 makeText（使用 JNI.CallStaticObjectMethod）
            unsafe
            {
                var vtable = *(IntPtr**)env;
                var callStaticObjectMethodA = Marshal.GetDelegateForFunctionPointer<CallStaticObjectMethodADelegate>(vtable[114]);
                
                var args = stackalloc IntPtr[3];
                args[0] = context;
                args[1] = javaMessage;
                args[2] = (IntPtr)0; // Toast.LENGTH_SHORT
                
                var toast = callStaticObjectMethodA(env, toastClass, makeTextMethod, (IntPtr)args);
                
                if (toast != IntPtr.Zero)
                {
                    // 获取 show 方法
                    var showMethod = JniHelperNative.GetMethodID(toastClass, "show", "()V");
                    if (showMethod != IntPtr.Zero)
                    {
                        var callVoidMethodA = Marshal.GetDelegateForFunctionPointer<CallVoidMethodADelegate>(vtable[61]);
                        callVoidMethodA(env, toast, showMethod, IntPtr.Zero);
                    }
                    
                    JniHelperNative.DeleteLocalRef(toast);
                }
            }
            
            JniHelperNative.DeleteLocalRef(javaMessage);
            JniHelperNative.DeleteGlobalRef(toastClass);
            JniHelperNative.DeleteLocalRef(context);
            
            AndroidLog.Info("JniHelperMixed", $"Toast shown: {message}");
        }
        catch (Exception ex)
        {
            AndroidLog.Error("JniHelperMixed", $"ShowToast error: {ex}");
        }
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CallStaticObjectMethodADelegate(IntPtr env, IntPtr clazz, IntPtr methodID, IntPtr args);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CallVoidMethodADelegate(IntPtr env, IntPtr obj, IntPtr methodID, IntPtr args);
}
