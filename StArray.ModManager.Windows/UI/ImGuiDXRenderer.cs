using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Manager;
using StArray.ModManager.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows.UI;

public sealed unsafe class ImGuiDXRenderer : IImGuiRenderer
{
    static ImGuiDXRenderer? s_i;
    public static ImGuiDXRenderer Instance => s_i ?? throw new("Not installed");
    public static bool Install() => (s_i = new ImGuiDXRenderer()).InstallInstance();

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
            Logger.Info(nameof(ImGuiDXRenderer), $"NativeInit={r}");

            if (s_pending != null) { _onRender += s_pending; s_pending = null; }
            _ok = true; Logger.Info(nameof(ImGuiDXRenderer), "Ready (multi-backend)");
            return true;
        }
        catch (Exception e) { Logger.Error(nameof(ImGuiDXRenderer), $"fail: {e.Message}"); return false; }
    }

    static IntPtr InitCallback()
    {
        Logger.Info(nameof(ImGuiDXRenderer), "Init");
        if (s_i != null)
            (s_i as IImGuiRenderer).InitImGui();
        return IntPtr.Zero;
    }

    static void ShutdownCallback()
    {
        Logger.Info(nameof(ImGuiDXRenderer), "Shutdown");
        ImGui.DestroyContext();
    }

    static void RenderCallback()
    {
        if (s_i == null) return;
        ImGui.NewFrame();
        try { s_i._onRender(); } catch (Exception e) { Logger.Error(nameof(ImGuiDXRenderer), $"Render error: {e.Message}"); }
        ImGui.EndFrame();
        ImGui.Render();
    }
}