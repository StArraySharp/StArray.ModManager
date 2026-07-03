using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiHook.DX12;
using ImGuiNET;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;

namespace StArray.ModManager;

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
        if (libraryName != "cimgui")
            return IntPtr.Zero;

        var dllDir = Path.Combine(AppContext.BaseDirectory, "dlls");
        if (NativeLibrary.TryLoad(Path.Combine(dllDir, "cimgui.dll"), out var handle))
            return handle;

        return IntPtr.Zero;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Entry(int argc, IntPtr argv)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => { Write($"UnhandledException: {e.ExceptionObject}\n"); };
        var totalSw = Stopwatch.StartNew();

        Benchmark.Begin();
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }

        Logger.OnLog += (level, s, arg3) =>
        {
            Write($"[ModManager/{s}][{level}]: {arg3}\n");
        };
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Args parse ({argc} args): {Benchmark.End():F3}s");

        Benchmark.Begin();
        string modsPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");
        AssemblyPath = Path.Combine(Path.GetDirectoryName(modsPath)!, "manager", typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        Writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(modsPath), "log.txt"), append: false);
        Writer.AutoFlush = true;
        Logger.Info($"{nameof(Managed)}-Benchmark", $"Path resolve: {Benchmark.End():F3}s");

        
        /*Il2CppFunctions.SetIl2CppLibraryPath(Path.Combine(new DirectoryInfo(Path.GetDirectoryName(modsPath)).Parent.FullName,"GameAssembly.dll"));
        StreamReader reader =
            new StreamReader(Path.Combine(Path.GetDirectoryName(AssemblyPath), "..", "..", "winmm.dll"));
        Thread.Sleep(5000);
        foreach (var assembly in Il2CppDomain.Current.GetAssemblies())
        {
            Logger.Info($"{nameof(Managed)}-Benchmark", $"Assembly: {assembly.Name}");
        }*/

        totalSw.Stop();
        Logger.Info($"{nameof(Managed)}-Benchmark", $"=== Startup total: {totalSw.Elapsed.TotalSeconds:F3}s ===");
        return 0;
    }

    static void EachIl2Cpp()
    {
        
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
}
