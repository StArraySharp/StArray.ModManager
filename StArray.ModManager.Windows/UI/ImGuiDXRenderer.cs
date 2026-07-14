using System.Runtime.CompilerServices;
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

    event Action IImGuiRenderer.OnRender { add => _onRender += value; remove => _onRender -= value; }
    public bool IsInitialized => _ok;
    bool IImGuiRenderer.Install() => InstallInstance();

    unsafe bool InstallInstance()
    {
        try
        {
            int r = NativeApi.Init(
                (nint)(delegate* unmanaged[Cdecl]<IntPtr>)&InitCallback,
                (nint)(delegate* unmanaged[Cdecl]<void>)&ShutdownCallback,
                (nint)(delegate* unmanaged[Cdecl]<void>)&RenderCallback);
            Logger.Info(nameof(ImGuiDXRenderer), $"NativeInit={r}");

            if (s_pending != null) { _onRender += s_pending; s_pending = null; }
            _ok = true; Logger.Info(nameof(ImGuiDXRenderer), "Ready (multi-backend)");
            return true;
        }
        catch (Exception e) { Logger.Error(nameof(ImGuiDXRenderer), $"fail: {e.Message}"); return false; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static IntPtr InitCallback()
    {
        Logger.Info(nameof(ImGuiDXRenderer), "Native Init");
        if (s_i != null)
            (s_i as IImGuiRenderer).InitImGui();
        return IntPtr.Zero;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void ShutdownCallback()
    {
        Logger.Info(nameof(ImGuiDXRenderer), "Shutdown");
        ImGui.DestroyContext();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void RenderCallback()
    {
        if (s_i == null) return;
        ImGui.NewFrame();
        try { s_i._onRender(); } catch (Exception e) { Logger.Error(nameof(ImGuiDXRenderer), $"Render error: {e.Message}"); }
        ImGui.EndFrame();
        ImGui.Render();
    }
}