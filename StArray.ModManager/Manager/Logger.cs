using System.Runtime.CompilerServices;

namespace StArray.ModManager.Manager;

/// <summary>
/// 统一日志系统 —— 纯事件广播，不依赖任何平台。由 Managed 入口订阅以桥接 Android logcat。
/// </summary>
public static class Logger
{
    public enum Level { Debug, Info, Warn, Error }

    /// <summary>日志广播事件（Level, Tag, Message）</summary>
    public static event Action<Level, string, string>? OnLog;

    public static void Debug(string tag, string? msg = null,
        [CallerFilePath] string? path = null, [CallerLineNumber] int line = 0)
        => OnLog?.Invoke(Level.Debug, tag, msg ?? $"{path}:{line}");

    public static void Info(string tag, string? msg = null,
        [CallerFilePath] string? path = null, [CallerLineNumber] int line = 0)
        => OnLog?.Invoke(Level.Info, tag, msg ?? $"{path}:{line}");

    public static void Warn(string tag, string? msg = null,
        [CallerFilePath] string? path = null, [CallerLineNumber] int line = 0)
        => OnLog?.Invoke(Level.Warn, tag, msg ?? $"{path}:{line}");

    public static void Error(string tag, string? msg = null,
        [CallerFilePath] string? path = null, [CallerLineNumber] int line = 0)
        => OnLog?.Invoke(Level.Error, tag, msg ?? $"{path}:{line}");
}
