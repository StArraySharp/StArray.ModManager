using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Android.UI;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Native;
using StArray.ModManager.Runtime;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Android;

/// <summary>CoreCLR entry / 加载器入口 — called by native delegate</summary>
/// <summary>CoreCLR 入口 / 加载器入口 — called by native delegate</summary>
public static class Managed
{
    /// <summary>当前管理器 DLL 的完整路径</summary>
    public static string AssemblyPath = string.Empty;
    private static StreamWriter? _logWriter;
    private static readonly object _logLock = new();
    /// <summary>原生入口，初始化 Logger 桥接、扫描 Mod、启动 ImGui</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Entry(int argc, IntPtr argv)
    {
        Benchmark.Begin();
        Logger.OnLog += (level, tag, msg) =>
        {
            var prio = level switch
            {
                Logger.Level.Debug => AndroidUtils.Priority.Debug,
                Logger.Level.Info  => AndroidUtils.Priority.Info,
                Logger.Level.Warn  => AndroidUtils.Priority.Warn,
                Logger.Level.Error => AndroidUtils.Priority.Error,
                _                  => AndroidUtils.Priority.Info
            };
            AndroidUtils.Write(prio, tag, msg);
        };
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }

        string modsPath = args.Length > 0
            ? args[0]
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");
        var logcat = new LogcatCapture(Path.Combine(Path.GetDirectoryName(modsPath), "logcat-output.txt"));
        logcat.StartAsync();
        AssemblyPath = Path.Combine(Path.GetDirectoryName(modsPath), "manager", typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        // 统一库解析：Android 上 il2cpp 库由注入器预加载，直接返回其初始化句柄。
        NativeLibraryResolver.Install(typeof(Il2CppFunctions).Assembly);
        NativeLibraryResolver.ResolveRequested += (libraryName, assembly) =>
            libraryName.Contains("IL2CPP_LIBRARY_NAME") ? Il2CppInit() : IntPtr.Zero;

        // 文件日志 → manager 根目录（与 mods/、runtime/ 同级）
        var rootDir = Path.GetDirectoryName(modsPath)!;
        _logWriter = new StreamWriter(Path.Combine(rootDir, "manager.log"), append: true) { AutoFlush = true };
        _logWriter.AutoFlush = true;
        Logger.OnLog += (level, tag, msg) =>
        {
            lock (_logLock)
                _logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {msg}");
        };
        
        _ = RuntimeManager.Detect();
        var loader = new ModLoader(modsPath);
        var ui = new ModManagerUI(loader, Path.GetDirectoryName(AssemblyPath)!);
        HookHelper.Instance = new DobbyHook();
        ImGuiEGLRender.OnRender += ui.Render;
        ImGuiEGLRender.Install();

        Logger.Info($"{nameof(Managed)}-Benchmark", $"=== Startup total: {Benchmark.End():F3}s ===");
        return 0;
    }
    
    [DllImport("modmanager", EntryPoint = "modmanager_il2cpp_init")]
    private static extern IntPtr Il2CppInit();
}
