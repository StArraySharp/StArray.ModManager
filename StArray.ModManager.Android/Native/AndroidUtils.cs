using System.Runtime.InteropServices;
using StArray.ModManager.Native;

namespace StArray.ModManager.Android.Native;

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
            var activity = JniNative.GetCurrentActivity();
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
            var context = JniNative.GetCurrentActivity();
            if (context == IntPtr.Zero) return;

            var makeText = toast.GetStaticMethodID("makeText",
                "(Landroid/content/Context;Ljava/lang/CharSequence;I)Landroid/widget/Toast;");
            var jMsg = JniNative.NewString(message);
            var toastObj = toast.CallStaticObjectMethod3(makeText, context, jMsg, 0);
            JniNative.DeleteLocalRef(jMsg);

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
            var upField = JniNative.GetFieldID(actCls.Handle, "mUnityPlayer",
                "Lcom/unity3d/player/UnityPlayerForActivityOrService;");
            if (upField == IntPtr.Zero)
                upField = JniNative.GetFieldID(actCls.Handle, "mUnityPlayer",
                    "Lcom/unity3d/player/UnityPlayer;");
            if (upField == IntPtr.Zero) return IntPtr.Zero;

            var player = actObj.GetObjectField(upField);
            if (player == IntPtr.Zero) return IntPtr.Zero;

            using var pObj = new JavaObject(player);
            using var pCls = pObj.GetClass();
            var getSV = JniNative.GetMethodID(pCls.Handle, "getSurfaceView",
                "()Landroid/view/SurfaceView;");
            var sv = pObj.CallObjectMethod0(getSV);
            if (sv == IntPtr.Zero) return IntPtr.Zero;

            using var svObj = new JavaObject(sv);
            var getH = JniNative.GetMethodID(
                JniNative.FindClass("android/view/SurfaceView"), "getHolder",
                "()Landroid/view/SurfaceHolder;");
            var holder = svObj.CallObjectMethod0(getH);
            if (holder == IntPtr.Zero) return IntPtr.Zero;

            using var hObj = new JavaObject(holder);
            var getS = JniNative.GetMethodID(
                JniNative.FindClass("android/view/SurfaceHolder"), "getSurface",
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
        _cachedNativeWindow = JniNative.SurfaceToNativeWindow(surface);
        JniNative.DeleteLocalRef(surface);
        return _cachedNativeWindow;
    }

    /// <summary>
    /// 获取 /data/data/{package}/files 私有目录（内部存储）
    /// </summary>
    public static string? GetInternalFilesDir()
    {
        var context = JniNative.GetCurrentActivity();
        if (context == IntPtr.Zero) return null;
        return GetDirFromContext(context, "getFilesDir", "()Ljava/io/File;");
    }

    /// <summary>
    /// 获取 /storage/emulated/0/Android/data/{package}/files 私有目录（外部存储）
    /// </summary>
    public static string? GetExternalFilesDir()
    {
        var context = JniNative.GetCurrentActivity();
        if (context == IntPtr.Zero) return null;
        return GetDirFromContext(context, "getExternalFilesDir", "(Ljava/lang/String;)Ljava/io/File;", null);
    }

    /// <summary>
    /// 获取 /data/data/{package}/cache 私有缓存目录
    /// </summary>
    public static string? GetCacheDir()
    {
        var context = JniNative.GetCurrentActivity();
        if (context == IntPtr.Zero) return null;
        return GetDirFromContext(context, "getCacheDir", "()Ljava/io/File;");
    }

    /// <summary>
    /// 无 JNI 降级：从 /proc/self/cmdline 读进程名（普通 app 即包名，多进程形如 pkg:svc），
    /// 推导 /data/data/{package}/cache。Activity 未附加 / JNI 不可用时使用。
    /// </summary>
    private static string? GetCacheDirNoJni()
    {
        try
        {
            // cmdline 以 \0 结尾，多进程进程名形如 "com.a.b:service"
            var proc = File.ReadAllText("/proc/self/cmdline");
            var pkg = proc.Split('\0')[0].Split(':')[0].Trim();
            if (pkg.Length == 0 || pkg.Contains('/')) return null;

            // /data/data 与 /data/user/0 在现代 Android 上互通
            var dir = Path.Combine("/data/data", pkg, "cache");
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch (Exception ex)
        {
            Error("AndroidUtils", $"GetCacheDirNoJni: {ex.Message}");
            return null;
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int chmod(string path, uint mode);

    // 0o755: rwxr-xr-x
    private const uint MODE_755 = 0x1ED;

    /// <summary>
    /// 加载 so 库的辅助方法：
    /// 把 <paramref name="sourcePath"/> 处的库复制到 /data/data/{package}/cache，
    /// 授予可执行权限（chmod 755）后 dlopen。
    /// <para>适用场景：库文件位于不可执行位置（如外部存储 / 只读分区），
    /// linker 拒绝直接 dlopen 时，经 app 私有 cache 目录中转加载。</para>
    /// </summary>
    /// <param name="sourcePath">源库文件绝对路径（如 .../manager/libcapstone.so）</param>
    /// <param name="flags">dlopen 标志，默认 RTLD_NOW | RTLD_GLOBAL</param>
    /// <returns>库句柄（dlopen）；已在进程中时返回其基址；失败返回 <see cref="IntPtr.Zero"/></returns>
    public static IntPtr LoadLibrary(string sourcePath,
        DL.RTLDFlags flags = DL.RTLDFlags.RTLD_NOW | DL.RTLDFlags.RTLD_GLOBAL)
    {
        var name = Path.GetFileName(sourcePath);
        try
        {
            if (!File.Exists(sourcePath))
            {
                Error("AndroidUtils", $"LoadLibrary: source not found: {sourcePath}");
                return IntPtr.Zero;
            }

            // 已加载 → 直接返回基址（也避免了覆写已 mmap 的 so 导致映射损坏）
            var existing = DL.GetBaseAddress(name);
            if (existing != IntPtr.Zero)
            {
                Info("AndroidUtils", $"LoadLibrary: already loaded: {name}");
                return existing;
            }

            // Activity/JNI 可用走 getCacheDir()；否则从 /proc/self/cmdline 推导。
            // CreateDirectory 幂等——目录可能被清理工具连目录本身删掉。
            var cacheDir = GetCacheDir() ?? GetCacheDirNoJni();
            if (string.IsNullOrEmpty(cacheDir))
            {
                Error("AndroidUtils", "LoadLibrary: cache dir unavailable (JNI and /proc fallback both failed)");
                return IntPtr.Zero;
            }
            Directory.CreateDirectory(cacheDir);

            var dest = Path.Combine(cacheDir, name);

            // 目标缺失或长度不一致才复制（覆盖写只发生在未加载时，安全）
            var needCopy = !File.Exists(dest) || new FileInfo(dest).Length != new FileInfo(sourcePath).Length;
            if (needCopy)
            {
                File.Copy(sourcePath, dest, overwrite: true);
                Info("AndroidUtils", $"LoadLibrary: copied {name} -> {dest}");
            }

            // 可执行权限（cache 下的新建文件默认没有 x 位）
            chmod(dest, MODE_755);

            var handle = DL.Open(dest, flags);
            if (handle == IntPtr.Zero)
                Error("AndroidUtils", $"LoadLibrary: dlopen failed: {Marshal.PtrToStringAnsi(DL.Error())}");
            else
                Info("AndroidUtils", $"LoadLibrary: loaded {name} @ 0x{handle:X}");

            return handle;
        }
        catch (Exception ex)
        {
            Error("AndroidUtils", $"LoadLibrary({sourcePath}): {ex}");
            return IntPtr.Zero;
        }
    }

    private static string? GetDirFromContext(IntPtr context, string methodName, string sig, string? arg = null)
    {
        try
        {
            using var ctxObj = new JavaObject(context);
            using var ctxCls = ctxObj.GetClass();
            var methodId = JniNative.GetMethodID(ctxCls.Handle, methodName, sig);

            IntPtr file;
            if (arg != null)
            {
                var jArg = JniNative.NewString(arg);
                file = ctxObj.CallObjectMethod1(methodId, jArg);
                JniNative.DeleteLocalRef(jArg);
            }
            else
            {
                file = ctxObj.CallObjectMethod0(methodId);
            }

            if (file == IntPtr.Zero) return null;

            using var fileObj = new JavaObject(file);
            using var fileCls = fileObj.GetClass();
            var getPath = JniNative.GetMethodID(fileCls.Handle, "getAbsolutePath", "()Ljava/lang/String;");
            var pathStr = fileObj.CallObjectMethod0(getPath);

            var result = JniHelperNative.GetString(pathStr);
            JniNative.DeleteLocalRef(pathStr);
            return result;
        }
        catch (Exception ex) { Error("AndroidUtils", $"GetDirFromContext({methodName}): {ex}"); return null; }
    }
}
