using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.PInvoke;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;

namespace StArray.ModManager;

/// <summary>CoreCLR entry / 加载器入口 — called by native delegate</summary>
public static class Managed
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Entry(int argc, IntPtr argv)
    {
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr pStr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(pStr)!;
        }

        string modsPath = args.Length > 0
            ? args[0]
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");

        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);

        var loader = new ModLoader(modsPath);
        loader.OnLogMessage += s => AndroidUtils.Info(nameof(Managed), s);
        ImGuiEGLRender.OnRender += new ModManagerUI(loader).Render;
        ImGuiEGLRender.Install();
        return 0;
    }
}
