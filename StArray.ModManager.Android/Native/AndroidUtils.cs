using System.Runtime.InteropServices;

namespace StArray.ModManager.Native;

/// <summary>
/// Android 工具 — 日志、Toast、Unity Surface（通过 JavaClass/JavaObject）
/// </summary>
public static class AndroidUtils
{
    public enum Priority
    {
        Unknown = 0, Default = 1, Verbose = 2, Debug = 3,
        Info = 4, Warn = 5, Error = 6, Fatal = 7, Silent = 8
    }

    [DllImport("modmanager", EntryPoint = "modmanager_log_write")]
    private static extern void modmanager_log_write(int prio, string tag, string msg);

    public static void Write(Priority prio, string tag, string msg)
        => modmanager_log_write((int)prio, tag, msg);

    public static void Verbose(string tag, string msg) => Write(Priority.Error, tag, $"[VERBOSE] {msg}");
    public static void Debug(string tag, string msg)   => Write(Priority.Error, tag, $"[DEBUG] {msg}");
    public static void Info(string tag, string msg)    => Write(Priority.Error, tag, $"[INFO] {msg}");
    public static void Warn(string tag, string msg)    => Write(Priority.Error, tag, $"[WARN] {msg}");
    public static void Error(string tag, string msg)   => Write(Priority.Error, tag, msg);

    public static IntPtr GetCurrentActivity()
    {
        try
        {
            var activity = JniHelperNative.GetCurrentActivity();
            if (activity != IntPtr.Zero) Info("AndroidUtils", $"Activity: 0x{activity:X}");
            return activity;
        }
        catch (Exception ex) { Error("AndroidUtils", $"GetCurrentActivity: {ex}"); return IntPtr.Zero; }
    }

    public static void ShowToast(string message)
    {
        try
        {
            using var toast = new JavaClass("android/widget/Toast");
            var context = JniHelperNative.GetCurrentActivity();
            if (context == IntPtr.Zero) return;

            var makeText = toast.GetStaticMethodID("makeText",
                "(Landroid/content/Context;Ljava/lang/CharSequence;I)Landroid/widget/Toast;");
            var jMsg = JniHelperNative.NewString(message);
            var toastObj = toast.CallStaticObjectMethod3(makeText, context, jMsg, 0);
            JniHelperNative.DeleteLocalRef(jMsg);

            if (toastObj != IntPtr.Zero)
            {
                using var obj = new JavaObject(toastObj);
                var show = toast.GetMethodID("show", "()V");
                obj.CallVoidMethod0(show);
            }
            Info("AndroidUtils", $"Toast: {message}");
        }
        catch (Exception ex) { Error("AndroidUtils", $"ShowToast: {ex}"); }
    }

    private static IntPtr _cachedNativeWindow;

    public static IntPtr GetUnitySurface()
    {
        try
        {
            using var up = new JavaClass("com.unity3d.player.UnityPlayer");
            var curActF = up.GetStaticFieldID("currentActivity", "Landroid/app/Activity;");
            var activity = up.GetStaticObjectField(curActF);
            if (activity == IntPtr.Zero) return IntPtr.Zero;
            Info("AndroidUtils", $"Activity: 0x{activity:X}");

            using var actObj = new JavaObject(activity);
            using var actCls = actObj.GetClass();
            var upField = JniHelperNative.GetFieldID(actCls.Handle, "mUnityPlayer",
                "Lcom/unity3d/player/UnityPlayerForActivityOrService;");
            if (upField == IntPtr.Zero)
                upField = JniHelperNative.GetFieldID(actCls.Handle, "mUnityPlayer",
                    "Lcom/unity3d/player/UnityPlayer;");
            if (upField == IntPtr.Zero) return IntPtr.Zero;

            var player = actObj.GetObjectField(upField);
            if (player == IntPtr.Zero) return IntPtr.Zero;

            using var pObj = new JavaObject(player);
            using var pCls = pObj.GetClass();
            var getSV = JniHelperNative.GetMethodID(pCls.Handle, "getSurfaceView",
                "()Landroid/view/SurfaceView;");
            var sv = pObj.CallObjectMethod0(getSV);
            if (sv == IntPtr.Zero) return IntPtr.Zero;

            using var svObj = new JavaObject(sv);
            var getH = JniHelperNative.GetMethodID(
                JniHelperNative.FindClass("android/view/SurfaceView"), "getHolder",
                "()Landroid/view/SurfaceHolder;");
            var holder = svObj.CallObjectMethod0(getH);
            if (holder == IntPtr.Zero) return IntPtr.Zero;

            using var hObj = new JavaObject(holder);
            var getS = JniHelperNative.GetMethodID(
                JniHelperNative.FindClass("android/view/SurfaceHolder"), "getSurface",
                "()Landroid/view/Surface;");
            var surface = hObj.CallObjectMethod0(getS);

            Info("AndroidUtils", surface != IntPtr.Zero
                ? $"Surface: 0x{surface:X}" : "Surface: null");
            return surface;
        }
        catch (Exception ex) { Error("AndroidUtils", $"GetUnitySurface: {ex}"); return IntPtr.Zero; }
    }

    public static IntPtr GetUnityNativeWindow()
    {
        if (_cachedNativeWindow != IntPtr.Zero) return _cachedNativeWindow;
        var surface = GetUnitySurface();
        if (surface == IntPtr.Zero) return IntPtr.Zero;
        _cachedNativeWindow = JniHelperNative.SurfaceToNativeWindow(surface);
        JniHelperNative.DeleteLocalRef(surface);
        return _cachedNativeWindow;
    }

    /// <summary>
    /// 获取 /data/data/{package}/files 私有目录（内部存储）
    /// </summary>
    public static string? GetInternalFilesDir()
    {
        var context = JniHelperNative.GetCurrentActivity();
        if (context == IntPtr.Zero) return null;
        return GetDirFromContext(context, "getFilesDir", "()Ljava/io/File;");
    }

    /// <summary>
    /// 获取 /storage/emulated/0/Android/data/{package}/files 私有目录（外部存储）
    /// </summary>
    public static string? GetExternalFilesDir()
    {
        var context = JniHelperNative.GetCurrentActivity();
        if (context == IntPtr.Zero) return null;
        return GetDirFromContext(context, "getExternalFilesDir", "(Ljava/lang/String;)Ljava/io/File;", null);
    }

    private static string? GetDirFromContext(IntPtr context, string methodName, string sig, string? arg = null)
    {
        try
        {
            using var ctxObj = new JavaObject(context);
            using var ctxCls = ctxObj.GetClass();
            var methodId = JniHelperNative.GetMethodID(ctxCls.Handle, methodName, sig);

            IntPtr file;
            if (arg != null)
            {
                var jArg = JniHelperNative.NewString(arg);
                file = ctxObj.CallObjectMethod1(methodId, jArg);
                JniHelperNative.DeleteLocalRef(jArg);
            }
            else
            {
                file = ctxObj.CallObjectMethod0(methodId);
            }

            if (file == IntPtr.Zero) return null;

            using var fileObj = new JavaObject(file);
            using var fileCls = fileObj.GetClass();
            var getPath = JniHelperNative.GetMethodID(fileCls.Handle, "getAbsolutePath", "()Ljava/lang/String;");
            var pathStr = fileObj.CallObjectMethod0(getPath);

            var result = JniHelperNative.GetString(pathStr);
            JniHelperNative.DeleteLocalRef(pathStr);
            return result;
        }
        catch (Exception ex) { Error("AndroidUtils", $"GetDirFromContext({methodName}): {ex}"); return null; }
    }
}
