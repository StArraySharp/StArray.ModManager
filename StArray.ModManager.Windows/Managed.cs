using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Native;
using StArray.ModManager.Runtime;
using StArray.ModManager.RuntimeAbstractions;
using StArray.ModManager.Windows.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows;

public static class Managed
{
    static string DllParentPath = Environment.GetEnvironmentVariable("COREHOLD_TARGET_DIR") ?? AppContext.BaseDirectory;
    const int STD_OUTPUT_HANDLE = -11;
    static readonly Lock _writeLock = new();

    static Managed()
    {
        // 统一走核心解析器：Native/ImGui 库句柄复用（宿主已加载）逻辑以事件接入，
        // 系统库（kernel32、user32、d3d11 等）返回 0 交还默认解析。
        NativeLibraryResolver.Install(typeof(Managed).Assembly);
        NativeLibraryResolver.Install(typeof(ImGui).Assembly);
        NativeLibraryResolver.ResolveRequested += ResolveDll;
    }

    private static StreamWriter? Writer = null;

    private static IntPtr ResolveDll(string libraryName, System.Reflection.Assembly assembly)
    {
        // Our own Native DLL is already loaded by the host — return its handle.
        if (libraryName == NativeApi.LibraryName || libraryName == "cimgui")
        {
            var h = Win32Native.GetModuleHandleW(NativeApi.LibraryName + ".dll");
            if (h != IntPtr.Zero) return h;
        }
        // System DLLs (kernel32, user32, d3d11): let the OS resolve them.
        return IntPtr.Zero;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl), typeof(CallConvStdcall)])]
    public static int Entry(int argc, IntPtr argv)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Write($"UnhandledException: {e.ExceptionObject}\n");
        };
        var totalSw = Stopwatch.StartNew();

        Benchmark.Begin();
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }

        Logger.OnLog += (level, s, arg3) => { Write($"[ModManager/{s}][{level}]: {arg3}\n"); };
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Args parse ({argc} args): {Benchmark.End():F3}s");

        Benchmark.Begin();
        // args[0]: mods directory (from corehold.json entrypoint_string_args)
        string modsPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(DllParentPath, "..", "mods");

        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        lock (_writeLock)
        {
            Writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(modsPath)!, "log.txt"), append: false);
        }
        Writer.AutoFlush = true;
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Path resolve: {Benchmark.End():F3}s");
        
        var backend = RuntimeManager.Detect();
        Logger.Info("Runtime", $"Detected backend: {backend}");
        if (backend == RuntimeBackend.Il2Cpp)
        {
            Il2CppFunctions.SetIl2CppLibraryPath(Path.Combine(DllParentPath, "..", "..", "GameAssembly.dll"));
        }
        HookHelper.Instance = new MinHook();
        ModManagerUI modManagerUI = new(new ModLoader(modsPath), DllParentPath);

        NativeApi.SetBackend(1); // D3D11
        ImGuiDXRenderer.OnRender += modManagerUI.Render;
        ImGuiDXRenderer.Install();

        totalSw.Stop();
        Logger.Info($"{nameof(Managed)}-Benchmark", $"=== Startup total: {totalSw.Elapsed.TotalSeconds:F3}s ===");
        return 0;
    }

    static void Write(string s)
    {
        lock (_writeLock)
        {
            Writer?.Write(s);
            Win32Native.WriteConsoleW(Win32Native.GetStdHandle(STD_OUTPUT_HANDLE), s, (uint)s.Length, out _, 0);
        }
    }
}
