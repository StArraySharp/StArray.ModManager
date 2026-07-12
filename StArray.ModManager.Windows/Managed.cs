using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.RuntimeAbstractions;
using StArray.ModManager.Windows.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows;

public static class Managed
{
    public static string AssemblyPath = string.Empty;

    static Managed()
    {
        NativeLibrary.SetDllImportResolver(typeof(Managed).Assembly, ResolveDll);
        NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, ResolveDll);
    }

    private static StreamWriter Writer = null;

    private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory,libraryName), out var handle))
        {
            Write($"[Preload] Loaded {libraryName} from: {AppContext.BaseDirectory}\n");
            return handle;
        }
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
            : Path.Combine(AppContext.BaseDirectory, "..", "mods");

        // Managed directory = where our DLLs live (AppContext.BaseDirectory for CoreCLR host)
        string managedDir = AppContext.BaseDirectory;
        AssemblyPath = Path.Combine(managedDir, typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        Writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(modsPath), "log.txt"), append: false);
        Writer.AutoFlush = true;
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Path resolve: {Benchmark.End():F3}s");

        Il2CppFunctions.SetIl2CppLibraryPath(Path.Combine(new DirectoryInfo(modsPath).Parent.Parent.FullName,"GameAssembly.dll"));
        // 检测运行时后端 + 图形设备类型
        //RuntimeManager.Detect();
        Logger.Info("Runtime", $"Detected backend: {RuntimeManager.Backend}");

        ModManagerUI modManagerUI = new(new ModLoader(modsPath), managedDir);

        // Detect backend and pick renderer
        var renderer = Renderer.D3D11;
        Logger.Info("Backend", $"Detected renderer: {renderer}");

        if ((renderer & Renderer.D3D11) != 0)
        {
            NativeApi.SetBackend(1); // D3D11
            ImGuiRenderer.OnRender += modManagerUI.Render;
            ImGuiRenderer.Install();
            return 0;
        }
        totalSw.Stop();
        Logger.Info($"{nameof(Managed)}-Benchmark", $"=== Startup total: {totalSw.Elapsed.TotalSeconds:F3}s ===");
        return 0;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool WriteConsoleW(nint hConsole, string text, uint len, out uint written, nint reserved);

    const int STD_OUTPUT_HANDLE = -11;

    [DllImport("kernel32.dll")]
    static extern nint GetStdHandle(int nStdHandle);

    static void Write(string s)
    {
        Writer?.Write(s);
        WriteConsoleW(GetStdHandle(STD_OUTPUT_HANDLE), s, (uint)s.Length, out _, 0);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [Flags]
    public enum Renderer
    {
        None = 0,
        D3D12 = 1,
        D3D11 = 2,
        D3D9 = 4,
        OpenGL = 8,
        Vulkan = 16
    }

    public static Renderer GetGameRenderer()
    {
        Renderer result = Renderer.None;

        // Unity 2020+ GraphicsDeviceType 枚举值:
        //   Direct3D9 = 0, Direct3D11 = 2, Direct3D12 = 3
        //   OpenGLCore = 11, Vulkan = 13
        int gdt = GraphicsDevice.GetGraphicsDeviceType();
        switch (gdt)
        {
            case 0:  result |= Renderer.D3D9;   break;
            case 2:  result |= Renderer.D3D11;   break;
            case 3:  result |= Renderer.D3D12;   break;
            case 11: result |= Renderer.OpenGL;   break;
            case 13: result |= Renderer.Vulkan;   break;
            default:
                // 回退到模块名检测（非 Unity 或未知版本）
                if (GetModuleHandle("d3d12.dll") != IntPtr.Zero)
                    result |= Renderer.D3D12;
                if (GetModuleHandle("d3d11.dll") != IntPtr.Zero)
                    result |= Renderer.D3D11;
                if (GetModuleHandle("d3d9.dll") != IntPtr.Zero)
                    result |= Renderer.D3D9;
                if (GetModuleHandle("opengl32.dll") != IntPtr.Zero)
                    result |= Renderer.OpenGL;
                if (GetModuleHandle("vulkan-1.dll") != IntPtr.Zero)
                    result |= Renderer.Vulkan;
                break;
        }

        return result;
    }
}
