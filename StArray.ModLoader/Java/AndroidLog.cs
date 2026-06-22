using System.Runtime.InteropServices;

namespace StArray.ModLoader;

/// <summary>
/// Android logcat 输出 — P/Invoke 封装 <c>modloader_log_write</c>。
/// 通过 [DllImport("modloader")] 调用 libmodloader.so 中的导出函数，
/// 避免嵌入式 Mono 中 [DllImport("liblog")] 找不到系统库的问题。
/// </summary>
public static class AndroidLog
{
    public enum Priority
    {
        Unknown = 0,
        Default = 1,
        Verbose = 2,
        Debug   = 3,
        Info    = 4,
        Warn    = 5,
        Error   = 6,
        Fatal   = 7,
        Silent  = 8
    }

    [DllImport("modloader", EntryPoint = "modloader_log_write")]
    private static extern void modloader_log_write(int prio, string tag, string msg);

    public static void Write(Priority prio, string tag, string msg)
    {
        modloader_log_write((int)prio, tag, msg);
    }

    public static void Verbose(string tag, string msg) => Write(Priority.Error, tag, $"[VERBOSE] {msg}");
    public static void Debug(string tag, string msg)   => Write(Priority.Error, tag, $"[DEBUG] {msg}");
    public static void Info(string tag, string msg)    => Write(Priority.Error, tag, $"[INFO] {msg}");
    public static void Warn(string tag, string msg)    => Write(Priority.Error, tag, $"[WARN] {msg}");
    public static void Error(string tag, string msg)   => Write(Priority.Error, tag, msg);
}
