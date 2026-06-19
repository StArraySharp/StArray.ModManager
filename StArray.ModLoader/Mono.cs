using System.Runtime.InteropServices;

namespace StArray.ModLoader;

/// <summary>
/// 加载器入口 — 由 CoreCLR DOTNET_STARTUP_HOOKS 回调，无需参数。
/// 通过 DLL 自身路径推导加载器根目录。
/// </summary>
public static class Mono
{
    private const string LogFile = "modloader.log";
    private static string _logDir = "";
    private static string _logPath = "";

    /// <summary>
    /// 加载器入口点（无参）。DLL 所在目录即为加载器根目录。
    /// </summary>
    public static int Entry()
    {
        _logDir = Path.GetDirectoryName(typeof(Mono).Assembly.Location) ?? ".";
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
            Log("[Mono] ModManagerUI installed (C# eglSwapBuffers hook)");
        }
        catch (Exception ex) { Log($"[Mono] ModManagerUI install failed: {ex.Message}"); }

        return 0;
    }

    /// <summary>日志目录。</summary>
    public static string LogDir => _logDir;

    /// <summary>追加一行日志。</summary>
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
