using System.Runtime.InteropServices;
using StArray.ModLoader.PInvoke;

namespace StArray.ModLoader.Unity;

/// <summary>
/// Unity Surface 辅助类 - 纯 C# 实现，通过 Application ClassLoader 绕过 native 线程 FindClass 限制
/// 
/// 调用链:
///   ActivityThread.currentActivityThread().getApplication().getClassLoader()
///     -> loadClass("com.unity3d.player.UnityPlayer")
///       -> UnityPlayer.currentActivity
///         -> mUnityPlayer (兼容 UnityPlayerForActivityOrService / UnityPlayer)
///           -> getSurfaceView() -> getHolder() -> getSurface()
///             -> ANativeWindow_fromSurface(surface)
/// </summary>
public static class UnitySurfaceHelper
{
    private static IntPtr _cachedNativeWindow;

    // ===== 核心方法 =====

    /// <summary>
    /// 获取 Unity Surface (jobject)，纯 C# 实现
    /// </summary>
    public static IntPtr GetUnitySurface()
    {
        try
        {
            // 第1步：通过 Application ClassLoader 查找 UnityPlayer
            var unityPlayerClass = FindClassViaAppClassLoader("com.unity3d.player.UnityPlayer");
            if (unityPlayerClass == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "FindClassViaAppClassLoader failed for UnityPlayer");
                return IntPtr.Zero;
            }
            AndroidLog.Info(nameof(UnitySurfaceHelper), "Found UnityPlayer class via Application ClassLoader");

            // 第2步：获取 UnityPlayer.currentActivity
            var currentActivityField = JniHelperNative.GetStaticFieldID(
                unityPlayerClass, "currentActivity", "Landroid/app/Activity;");
            if (currentActivityField == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "Failed: currentActivity field");
                return IntPtr.Zero;
            }

            var activity = JniHelperNative.GetStaticObjectField(unityPlayerClass, currentActivityField);
            if (activity == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "currentActivity is null");
                return IntPtr.Zero;
            }
            AndroidLog.Info(nameof(UnitySurfaceHelper), $"Got activity: 0x{activity:X}");

            // 第3步：获取 mUnityPlayer（兼容新旧类型）
            var activityClass = JniHelperNative.GetObjectClass(activity);
            var unityPlayerField = TryGetFieldID(activityClass, "mUnityPlayer",
                "Lcom/unity3d/player/UnityPlayerForActivityOrService;");

            if (unityPlayerField == IntPtr.Zero)
            {
                AndroidLog.Info(nameof(UnitySurfaceHelper), "Trying legacy UnityPlayer type...");
                unityPlayerField = TryGetFieldID(activityClass, "mUnityPlayer",
                    "Lcom/unity3d/player/UnityPlayer;");
                if (unityPlayerField == IntPtr.Zero)
                {
                    AndroidLog.Error(nameof(UnitySurfaceHelper), "Failed: mUnityPlayer (both types)");
                    return IntPtr.Zero;
                }
            }

            var unityPlayer = JniHelperNative.GetObjectField(activity, unityPlayerField);
            if (unityPlayer == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "mUnityPlayer is null");
                return IntPtr.Zero;
            }

            // 第4步：getSurfaceView()
            var unityPlayerClass2 = JniHelperNative.GetObjectClass(unityPlayer);
            var getSurfaceView = JniHelperNative.GetMethodID(
                unityPlayerClass2, "getSurfaceView", "()Landroid/view/SurfaceView;");
            if (getSurfaceView == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "Failed: getSurfaceView method");
                return IntPtr.Zero;
            }

            var surfaceView = JniHelperNative.CallObjectMethod(unityPlayer, getSurfaceView);
            if (surfaceView == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "getSurfaceView returned null");
                return IntPtr.Zero;
            }

            // 第5步：getHolder()
            var svClass = JniHelperNative.FindClass("android/view/SurfaceView");
            var getHolder = JniHelperNative.GetMethodID(svClass, "getHolder", "()Landroid/view/SurfaceHolder;");
            var holder = JniHelperNative.CallObjectMethod(surfaceView, getHolder);
            if (holder == IntPtr.Zero)
            {
                AndroidLog.Error(nameof(UnitySurfaceHelper), "getHolder returned null");
                return IntPtr.Zero;
            }

            // 第6步：getSurface()
            var shClass = JniHelperNative.FindClass("android/view/SurfaceHolder");
            var getSurface = JniHelperNative.GetMethodID(shClass, "getSurface", "()Landroid/view/Surface;");
            var surface = JniHelperNative.CallObjectMethod(holder, getSurface);
            
            if (surface != IntPtr.Zero)
                AndroidLog.Info(nameof(UnitySurfaceHelper), $"Surface: 0x{surface:X}");
            else
                AndroidLog.Error(nameof(UnitySurfaceHelper), "getSurface returned null");

            return surface;
        }
        catch (Exception ex)
        {
            AndroidLog.Error(nameof(UnitySurfaceHelper), $"GetUnitySurface: {ex}");
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 获取 Unity ANativeWindow（从 Surface 转换，用于 ImGui）
    /// </summary>
    public static IntPtr GetUnityNativeWindow()
    {
        if (_cachedNativeWindow != IntPtr.Zero)
            return _cachedNativeWindow;

        var surface = GetUnitySurface();
        if (surface == IntPtr.Zero)
            return IntPtr.Zero;

        _cachedNativeWindow = JniHelperNative.SurfaceToNativeWindow(surface);
        JniHelperNative.DeleteLocalRef(surface);

        if (_cachedNativeWindow != IntPtr.Zero)
            AndroidLog.Info(nameof(UnitySurfaceHelper), $"ANativeWindow: 0x{_cachedNativeWindow:X}");
        else
            AndroidLog.Error(nameof(UnitySurfaceHelper), "ANativeWindow_fromSurface returned NULL");

        return _cachedNativeWindow;
    }

    // ===== Application ClassLoader 方式查找类 =====

    /// <summary>
    /// 通过 Application.getClassLoader().loadClass() 查找类
    /// 解决 native 线程 FindClass 使用 Boot ClassLoader 找不到应用类的问题
    /// </summary>
    private static IntPtr FindClassViaAppClassLoader(string className)
    {
        // 1. ActivityThread.currentActivityThread()
        var atClass = JniHelperNative.FindClass("android/app/ActivityThread");
        if (atClass == IntPtr.Zero) { LogErr("ActivityThread class"); return IntPtr.Zero; }

        var currentAtMethod = JniHelperNative.GetStaticMethodID(
            atClass, "currentActivityThread", "()Landroid/app/ActivityThread;");
        if (currentAtMethod == IntPtr.Zero) { LogErr("currentActivityThread method"); return IntPtr.Zero; }

        var activityThread = JniHelperNative.CallStaticObjectMethod(atClass, currentAtMethod);
        if (activityThread == IntPtr.Zero) { LogErr("currentActivityThread() returned null"); return IntPtr.Zero; }

        // 2. getApplication()
        var getAppMethod = JniHelperNative.GetMethodID(
            atClass, "getApplication", "()Landroid/app/Application;");
        if (getAppMethod == IntPtr.Zero) { LogErr("getApplication method"); return IntPtr.Zero; }

        var app = JniHelperNative.CallObjectMethod(activityThread, getAppMethod);
        if (app == IntPtr.Zero) { LogErr("getApplication() returned null"); return IntPtr.Zero; }

        // 3. application.getClassLoader()
        var appClass = JniHelperNative.GetObjectClass(app);
        var getClMethod = JniHelperNative.GetMethodID(
            appClass, "getClassLoader", "()Ljava/lang/ClassLoader;");
        if (getClMethod == IntPtr.Zero) { LogErr("getClassLoader method"); return IntPtr.Zero; }

        var classLoader = JniHelperNative.CallObjectMethod(app, getClMethod);
        if (classLoader == IntPtr.Zero) { LogErr("getClassLoader() returned null"); return IntPtr.Zero; }
        AndroidLog.Info(nameof(UnitySurfaceHelper), "Got Application ClassLoader");

        // 4. classLoader.loadClass(className)
        var clClass = JniHelperNative.GetObjectClass(classLoader);
        var loadClassMethod = JniHelperNative.GetMethodID(
            clClass, "loadClass", "(Ljava/lang/String;)Ljava/lang/Class;");
        if (loadClassMethod == IntPtr.Zero) { LogErr("loadClass method"); return IntPtr.Zero; }

        var jClassName = JniHelperNative.NewString(className);
        var env = JniHelperNative.GetJNIEnv();
        var result = CallJniObjectMethod(env, classLoader, loadClassMethod, jClassName);
        JniHelperNative.DeleteLocalRef(jClassName);

        if (result == IntPtr.Zero)
            LogErr($"loadClass(\"{className}\") returned null");

        return result;
    }

    // ===== JNI vtable 直接调用 =====

    private static IntPtr CallJniObjectMethod(IntPtr env, IntPtr obj, IntPtr methodID, IntPtr arg)
    {
        var funcTable = Marshal.ReadIntPtr(env);
        if (funcTable == IntPtr.Zero) return IntPtr.Zero;
        int offset = 34 * IntPtr.Size; // JNI CallObjectMethod index
        var funcPtr = Marshal.ReadIntPtr(funcTable + offset);
        var del = Marshal.GetDelegateForFunctionPointer<CallObjMethodDel>(funcPtr);
        return del(env, obj, methodID, arg);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CallObjMethodDel(IntPtr env, IntPtr obj, IntPtr methodID, IntPtr arg);

    // ===== 辅助方法 =====

    private static IntPtr TryGetFieldID(IntPtr clazz, string name, string sig)
    {
        var f = JniHelperNative.GetFieldID(clazz, name, sig);
        if (f == IntPtr.Zero) JniHelperNative.CheckException();
        return f;
    }

    private static void LogErr(string msg)
        => AndroidLog.Error(nameof(UnitySurfaceHelper), $"Failed: {msg}");
}
