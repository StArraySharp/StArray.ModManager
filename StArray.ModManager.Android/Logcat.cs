namespace StArray.ModManager.Android;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class LogcatCapture
{
    private Process _process;
    private readonly string _outputFilePath;
    private readonly string _logcatArguments;
    private CancellationTokenSource _cts;

    /// <summary>
    /// 初始化 Logcat 捕获器
    /// </summary>
    /// <param name="outputFilePath">输出文件路径</param>
    /// <param name="logcatArguments">logcat 参数，默认为 "-v time"</param>
    public LogcatCapture(string outputFilePath, string logcatArguments = "-v time")
    {
        _outputFilePath = outputFilePath;
        _logcatArguments = logcatArguments;
    }

    /// <summary>
    /// 启动 logcat 并异步写入文件
    /// </summary>
    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        await Task.Run(() => RunLogcat(_cts.Token));
    }

    /// <summary>
    /// 停止 logcat 捕获
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _process?.Kill();
            _process?.Dispose();
        }
        catch { }
    }

    private void RunLogcat(CancellationToken token)
    {
        // 确保输出目录存在
        var dir = Path.GetDirectoryName(_outputFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var startInfo = new ProcessStartInfo
        {
            FileName = "logcat",
            Arguments = _logcatArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };

        using (_process = new Process { StartInfo = startInfo })
        {
            _process.Start();

            // 异步读取 stdout 并写入文件
            Task stdoutTask = ReadStreamAsync(_process.StandardOutput, token);
            Task stderrTask = ReadStreamAsync(_process.StandardError, token);

            // 等待进程退出或取消
            Task.WaitAny(
                Task.Run(() => _process.WaitForExit(), token),
                Task.Delay(Timeout.Infinite, token)
            );

            // 确保读取完成
            Task.WaitAll(stdoutTask, stderrTask);
        }
    }

    private async Task ReadStreamAsync(StreamReader reader, CancellationToken token)
    {
        using var writer = new StreamWriter(_outputFilePath, append: true) { AutoFlush = true };
        while (!reader.EndOfStream && !token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                await writer.WriteLineAsync(line);
                // 可选：同时输出到控制台调试
                // Console.WriteLine(line);
            }
        }
    }
}