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
        NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, (string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
        {
            if (libraryName.Contains("cimgui"))
            {
                // cimgui symbols are compiled into StArray.ModManager.Windows.Native.dll,
                // which is already loaded by the native host before managed code runs.
                // Just return that module's handle — no filesystem path needed.
                var h = Win32Native.GetModuleHandleW(NativeApi.LibraryName + ".dll");
                if (h != IntPtr.Zero) return h;
            }
            return IntPtr.Zero;
        });
    }

    private static StreamWriter Writer = null;

    private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Our own Native DLL is already loaded by the host — return its handle.
        if (libraryName == NativeApi.LibraryName)
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
            : Path.Combine(AppContext.BaseDirectory, "..", "mods");

        // Managed directory = where our DLLs live (AppContext.BaseDirectory for CoreCLR host)
        string managedDir = AppContext.BaseDirectory;
        AssemblyPath = Path.Combine(managedDir, typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        Writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(modsPath), "log.txt"), append: false);
        Writer.AutoFlush = true;
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Path resolve: {Benchmark.End():F3}s");

        Il2CppFunctions.SetIl2CppLibraryPath(Path.Combine(AppContext.BaseDirectory,"..","..", "GameAssembly.dll"));
        // 检测运行时后端 (IL2CPP / Mono)
        RuntimeManager.Detect();
        Logger.Info("Runtime", $"Detected backend: {RuntimeManager.Backend}");

        // 检测图形设备类型
        var renderer = GetGameRenderer();
        Logger.Info("Backend", $"Detected renderer: {renderer}");

        ModManagerUI modManagerUI = new(new ModLoader(modsPath), managedDir);

        NativeApi.SetBackend(1); // D3D11
        ImGuiDXRenderer.OnRender += modManagerUI.Render;
        ImGuiDXRenderer.Install();
        /*if ((renderer & Renderer.D3D11) != 0)
        {
            NativeApi.SetBackend(1); // D3D11
            ImGuiRenderer.OnRender += modManagerUI.Render;
            ImGuiRenderer.Install();
        }
        else if ((renderer & Renderer.D3D12) != 0)
        {
            NativeApi.SetBackend(0); // D3D12
            ImGuiRenderer.OnRender += modManagerUI.Render;
            ImGuiRenderer.Install();
        }
        else if ((renderer & Renderer.D3D9) != 0)
        {
            NativeApi.SetBackend(2); // D3D9
            ImGuiRenderer.OnRender += modManagerUI.Render;
            ImGuiRenderer.Install();
        }
        else if ((renderer & Renderer.Vulkan) != 0 ||
                 (renderer & Renderer.OpenGL) != 0)
        {
            // GL/VK: native handles Win32 init only, C# handles ImGui backend
            ImGuiRenderer.OnRender += modManagerUI.Render;
            ImGuiRenderer.Install();
        }
        else
        {
            Logger.Error("Backend", "No supported renderer found");
            return 1;
        }*/

        totalSw.Stop();
        Logger.Info($"{nameof(Managed)}-Benchmark", $"=== Startup total: {totalSw.Elapsed.TotalSeconds:F3}s ===");
        return 0;
    }

    const int STD_OUTPUT_HANDLE = -11;
    static readonly object _writeLock = new();

    static void Write(string s)
    {
        lock (_writeLock)
        {
            Writer?.Write(s);
            Win32Native.WriteConsoleW(Win32Native.GetStdHandle(STD_OUTPUT_HANDLE), s, (uint)s.Length, out _, 0);
        }
    }

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
                if (Win32Native.GetModuleHandleW("d3d12.dll") != nint.Zero)
                    result |= Renderer.D3D12;
                if (Win32Native.GetModuleHandleW("d3d11.dll") != nint.Zero)
                    result |= Renderer.D3D11;
                if (Win32Native.GetModuleHandleW("d3d9.dll") != nint.Zero)
                    result |= Renderer.D3D9;
                if (Win32Native.GetModuleHandleW("opengl32.dll") != nint.Zero)
                    result |= Renderer.OpenGL;
                if (Win32Native.GetModuleHandleW("vulkan-1.dll") != nint.Zero)
                    result |= Renderer.Vulkan;
                break;
        }

        return result;
    }
}
