using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows.UI;

public sealed unsafe class ImGuiRenderer : IImGuiRenderer
{
    static ImGuiRenderer? s_i;
    public static ImGuiRenderer Instance => s_i ?? throw new("Not installed");
    public static bool Install() => (s_i = new ImGuiRenderer()).InstallInstance();

    static Action? s_pending;
    public static event Action OnRender
    {
        add { if (s_i != null) s_i._onRender += value; else s_pending += value; }
        remove { if (s_i != null) s_i._onRender -= value; else s_pending -= value; }
    }

    bool _ok; Action _onRender = () => { };

    static NativeApi.ImGuiInitCallback?    _sInit;
    static NativeApi.ImGuiShutdownCallback? _sShutdown;
    static NativeApi.ImGuiRenderCallback?   _sRender;

    event Action IImGuiRenderer.OnRender { add => _onRender += value; remove => _onRender -= value; }
    public bool IsInitialized => _ok;
    bool IImGuiRenderer.Install() => InstallInstance();

    bool InstallInstance()
    {
        try
        {
            _sInit = InitCallback;
            _sShutdown = ShutdownCallback;
            _sRender = RenderCallback;

            int r = NativeApi.Init(
                Marshal.GetFunctionPointerForDelegate(_sInit),
                Marshal.GetFunctionPointerForDelegate(_sShutdown),
                Marshal.GetFunctionPointerForDelegate(_sRender));
            W($"[NH] NativeInit={r}");

            if (s_pending != null) { _onRender += s_pending; s_pending = null; }
            _ok = true; W("[Renderer] Ready (multi-backend)");
            return true;
        }
        catch (Exception e) { W($"[Renderer] fail: {e.Message}"); return false; }
    }

    static IntPtr InitCallback()
    {
        W("[Renderer] Init");
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        var f = @"C:\Windows\Fonts\msyh.ttc";
        if (File.Exists(f)) io.Fonts.AddFontFromFileTTF(f, 16f, null, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
        return IntPtr.Zero;
    }

    static void ShutdownCallback()
    {
        W("[Renderer] Shutdown");
        ImGui.DestroyContext();
    }

    static void RenderCallback()
    {
        if (s_i == null) return;
        ImGui.NewFrame();
        try { s_i._onRender(); } catch (Exception e) { W($"[Renderer] err: {e.Message}"); }
        ImGui.EndFrame();
        ImGui.Render();
    }

    [DllImport("user32")] static extern IntPtr GetForegroundWindow();
    [DllImport("kernel32")] static extern IntPtr GetStdHandle(int n);
    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern bool WriteConsoleW(IntPtr h, string s, uint l, out uint w, IntPtr r);
    static void W(string s) { var h = GetStdHandle(-11); uint _; WriteConsoleW(h, s + "\n", (uint)(s.Length + 1), out _, 0); }
}
