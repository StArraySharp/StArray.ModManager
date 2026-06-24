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
/// </summary>
public static class Mono
{
    private const string LogTag = "StArray.ModManager";

    public static int Entry()
    {
        string modsPath =
            new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.Parent.FullName + "/mods";
        if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
        ModLoader loader = new ModLoader(modsPath);
        loader.OnLogMessage += s => AndroidLog.Info(LogTag, s); 
        ImGuiEGLRenderer.OnRender += new ModManagerUI(loader).Render;
        return 0;
    }
}
