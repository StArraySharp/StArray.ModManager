using System.Diagnostics;

namespace StArray.ModManager.Manager;

/// <summary>
/// 轻量计时 — Begin 启动，End 传入 Action&lt;double&gt; 打印耗时
/// <code>
/// Benchmark.Begin();
/// // ... work ...
/// Benchmark.End(s => Logger.Info("Foo", $"done in {s:F3}s"));
/// </code>
/// </summary>
public static class Benchmark
{
    [ThreadStatic] private static Stopwatch? _sw;

    public static void Begin()
    {
        _sw = Stopwatch.StartNew();
    }

    public static void End(Action<double> onDone)
    {
        if (_sw == null) return;
        _sw.Stop();
        onDone(_sw.Elapsed.TotalSeconds);
    }
}
