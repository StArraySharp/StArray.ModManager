using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StArray.ModLoader;

/// <summary>
/// 加载器入口 — 由 CoreCLR delegate 回调。
/// </summary>
public static class Mono
{
    private const string LogFile = "modloader.log";
    private const string LogTag = "StArray.ModLoader.Managed";
    private static string _logDir = "";
    private static string _logPath = "";

    [DllImport("modloader", EntryPoint = "modloader_log_write")]
    private static extern void NativeLog(int prio, string tag, string msg);

    [DllImport("dl")]
    static extern IntPtr dlopen(string filename, int flags);

    [DllImport("modloader")]
    static extern void GetAssemblyCount();

    /// <summary>
    /// 加载器入口点（无参）。
    /// </summary>
    public static int Entry2()
    {
        // 最先安装全局异常处理器
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) => {
            AndroidLog.Error(LogTag, $"Unobserved: {e.Exception}");
            e.SetObserved();
        };

        _logDir = AppContext.BaseDirectory ??
                  Path.GetDirectoryName(typeof(Mono).Assembly.Location) ??
                  Environment.GetEnvironmentVariable("APP_PATHS")?.Split(':')[0] ??
                  ".";
        _logPath = Path.Combine(_logDir, LogFile);

        Log("========================================");
        Log($"ModLoader managed Entry() called");
        Log($"Loader directory: {_logDir}");
        Log($".NET runtime: {Environment.Version}");
        Log($"OS: {Environment.OSVersion}");
        Log("========================================");

        try
        {
            ModManagerUI.Install();
            Log("[Mono] ModManagerUI installed");
        }
        catch (Exception ex) { Log($"[Mono] Install failed: {ex}"); }

        return 0;
    }

    public static int Entry()
    {
        Thread.Sleep(5000);
        UnityResolve resolve = new UnityResolve();
        resolve.InitIl2Cpp();
        AndroidLog.Error(LogTag, $"AssemblyCount:{resolve.Assemblies.Count()}");
        foreach (var assembly1 in resolve.Assemblies)
        {
            AndroidLog.Error("Assembly",assembly1.Name);
        }
        var assembly = resolve.GetAssembly("Assembly-CSharp.dll");
        AndroidLog.Error(LogTag, $"assembly is null: {assembly == null}");
        if (assembly == null) return -1;
        var klass = assembly.GetClass("", "NewBehaviourScript");
        AndroidLog.Error(LogTag, $"class is null: {klass == null}");
        if (klass == null) return -1;
        var method = klass.GetMethod("Button_OnClick");
        AndroidLog.Error(LogTag, $"method is null: {method == null} ptr:{method?.NativePtr:X}");
        if (method == null) return -1;
        Dobby.Hook(method.FunctionPtr, typeof(Mono).GetMethod(nameof(OnClick)), out var orig);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void OnClick(IntPtr instance)
    {
        NativeLog(6,"DOTNET",$"0x{instance:X}: OnClick Called");
    }

    static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = $"UNHANDLED EXCEPTION (terminating={e.IsTerminating}): {ex}";
        AndroidLog.Error(LogTag, msg);
        Log(msg);
    }

    public static string LogDir => _logDir;

    public static void Log(string message)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}\n";
            File.AppendAllText(_logPath, line);
        }
        catch { }
    }
}
