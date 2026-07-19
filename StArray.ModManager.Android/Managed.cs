using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Android.UI;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
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
        var totalSw = Stopwatch.StartNew();

        // 桥接 Logger → Android logcat
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
        Logger.Error($"{nameof(Managed)}-Benchmark", $"Logger bridge: {Benchmark.End():F3}s");

        // 解析命令行参数
        Benchmark.Begin();
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }
        Logger.Error($"{nameof(Managed)}-Benchmark", $"Args parse ({argc} args): {Benchmark.End():F3}s");

        // 路径解析
        Benchmark.Begin();
        string modsPath = args.Length > 0
            ? args[0]
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");
        AssemblyPath = Path.Combine(Path.GetDirectoryName(modsPath), "manager", typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        NativeLibrary.SetDllImportResolver(typeof(Il2CppFunctions).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == "IL2CPP_LIBRARY_NAME")
            {
                return Il2CppInit();
            }
            return IntPtr.Zero;
        });

        // 文件日志 → manager 根目录（与 mods/、runtime/ 同级）
        var rootDir = Path.GetDirectoryName(modsPath)!;
        _logWriter = new StreamWriter(Path.Combine(rootDir, "manager.log"), append: true) { AutoFlush = true };
        _logWriter.AutoFlush = true;
        Logger.OnLog += (level, tag, msg) =>
        {
            lock (_logLock)
                _logWriter?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{tag}] {msg}");
        };
        
        Logger.Error($"{nameof(Managed)}-Benchmark", $"Path resolve: {Benchmark.End():F3}s");
        var backend = RuntimeManager.Detect();
        Logger.Info(nameof(Managed), "Backend: "+ backend);
        // 初始化
        Benchmark.Begin();
        var loader = new ModLoader(modsPath);
        Logger.Error($"{nameof(Managed)}-Benchmark", $"ModLoader init: {Benchmark.End():F3}s");

        Benchmark.Begin();
        var ui = new ModManagerUI(loader, Path.GetDirectoryName(AssemblyPath)!);
        Logger.Error($"{nameof(Managed)}-Benchmark", $"ModManagerUI init: {Benchmark.End():F3}s");

        HookHelper.Instance = new DobbyHook();
        Benchmark.Begin();
        ImGuiEGLRender.OnRender += ui.Render;
        ImGuiEGLRender.Install();
        Logger.Error($"{nameof(Managed)}-Benchmark", $"ImGui install: {Benchmark.End():F3}s");

        totalSw.Stop();
        Logger.Error($"{nameof(Managed)}-Benchmark", $"=== Startup total: {totalSw.Elapsed.TotalSeconds:F3}s ===");
        return 0;
    }
    
    [DllImport("modmanager", EntryPoint = "modmanager_il2cpp_init")]
    private static extern IntPtr Il2CppInit();
}
