using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Java;
using StArray.ModManager.Manager;
using StArray.ModManager.PInvoke;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;
using StArray.ModManager.Unity;

namespace StArray.ModManager;

/// <summary>
/// 加载器入口 — 由 CoreCLR delegate 回调。
/// Native 端调用签名: int Entry(int argc, const char** argv)
/// </summary>
public static class Managed
{
    private const string LogTag = "StArray.ModManager";

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int Entry(int argc, IntPtr argv)
    {
        // 解析 native argc/argv → string[]
        string[] args = new string[argc];
        for (int i = 0; i < argc; i++)
        {
            IntPtr ptr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
            args[i] = Marshal.PtrToStringUTF8(ptr)!;
        }

        string modsPath = args.Length > 0 ? args[0]
            : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "mods");

        if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
        //ModLoader loader = new ModLoader(modsPath);
        //loader.OnLogMessage += s => AndroidLog.Info(LogTag, s); 
        //ImGuiRender.OnRender += new ModManagerUI(loader).Render;
        ImGuiRender.Install();
        return 0;
    }
}
