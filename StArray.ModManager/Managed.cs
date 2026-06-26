using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Manager;
using StArray.ModManager.Native;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;

namespace StArray.ModManager;

/// <summary>CoreCLR entry / 加载器入口 — called by native delegate</summary>
public static class Managed
{
    public static string AssemblyPath = string.Empty;
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Entry(int argc, IntPtr argv)
    {
        // 桥接 Logger → Android logcat
        Logger.OnLog += (level, tag, msg) =>
        {
            var prio = level switch
            {
                Logger.Level.Debug => AndroidUtils.Priority.Debug,
                Logger.Level.Info  => AndroidUtils.Priority.Info,
                Logger.Level.Warn  => AndroidUtils.Priority.Warn,
                Logger.Level.Error => AndroidUtils.Priority.Error,
                _                  => AndroidUtils.Priority.Info
            };
            AndroidUtils.Write(prio, tag, msg);
        };
        var sw = Stopwatch.StartNew();

        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }
        
        string modsPath = args.Length > 0
            ? args[0]
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");
        AssemblyPath = Path.Combine(Path.GetDirectoryName(modsPath), "manager", typeof(Managed).Namespace + ".dll");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);



        var loader = new ModLoader(modsPath);
        var ui = new ModManagerUI(loader);
        ImGuiEGLRender.OnRender += ui.Render;
        ImGuiEGLRender.Install();

        sw.Stop();
        Logger.Error(nameof(Managed), $"Startup completed in {sw.Elapsed.TotalSeconds:F3}s");
        return 0;
    }
}
