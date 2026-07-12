using System.Diagnostics;

namespace StArray.ModManager.Manager;

/// <summary>
/// 轻量计时，支持嵌套 — Begin 启动，End 返回耗时秒数
/// <code>
/// Benchmark.Begin();
/// // ... work ...
/// var elapsed = Benchmark.End();
/// Logger.Info("Foo", $"done in {elapsed:F3}s");
/// </code>
/// </summary>
public static class Benchmark
{
    [ThreadStatic] private static Stack<Stopwatch>? _stack;

    /// <summary>启动计时（支持嵌套）</summary>
    public static void Begin()
    {
        _stack ??= new Stack<Stopwatch>();
        _stack.Push(Stopwatch.StartNew());
    }

    /// <summary>结束最近一次计时，返回耗时秒数</summary>
    public static double End()
    {
        if (_stack == null || _stack.Count == 0) return 0;
        var sw = _stack.Pop();
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }
}
