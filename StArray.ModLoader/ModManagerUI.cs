using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace StArray.ModLoader;

public class ModManagerUI
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int EglSwapDelegate(IntPtr display, IntPtr surface);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int VkPresentDelegate(IntPtr queue, IntPtr pPresentInfo);

    [DllImport("libGLESv2.so")] static extern void glGetIntegerv(int pname, int[] data);
    const int GL_VIEWPORT = 0x0BA2;

    static ModManagerUI? _instance;
    static IntPtr _context;
    bool _showMainWindow = true, _showDemoWindow;
    string _searchText = "";
    readonly List<string> _mods = new() { "Loading..." };
    int _selectedMod = -1;
    static bool _installed;
    static EglSwapDelegate _origEgl = null!;
    static VkPresentDelegate _origVk = null!;
    static int _frameCount;

    public static void Install()
    {
        if (_installed) return;
        _instance = new ModManagerUI();
        _instance._InitImGui();

        nint ea = Dobby.SymbolResolver("libEGL.so", "eglSwapBuffers");
        if (ea == IntPtr.Zero) ea = Dobby.SymbolResolver("libGLESv2.so", "eglSwapBuffers");
        if (ea != IntPtr.Zero) {
            Mono.Log($"[UI] eglSwapBuffers @ 0x{ea:X}");
            Dobby.Hook(ea, typeof(ModManagerUI).GetMethod(nameof(EglHook),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!, out nint o);
            _origEgl = Marshal.GetDelegateForFunctionPointer<EglSwapDelegate>(o);
            ImGuiGLES.Init();
        }

        nint va = Dobby.SymbolResolver("libvulkan.so", "vkQueuePresentKHR");
        if (va != IntPtr.Zero) {
            Mono.Log($"[UI] vkQueuePresentKHR @ 0x{va:X}");
            Dobby.Hook(va, typeof(ModManagerUI).GetMethod(nameof(VkHook),
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!, out nint vo);
            _origVk = Marshal.GetDelegateForFunctionPointer<VkPresentDelegate>(vo);
        }

        _installed = true;
    }

    static int EglHook(IntPtr d, IntPtr s) { int r = _origEgl(d, s); RenderImGui(); return r; }

    static int VkHook(IntPtr q, IntPtr p)
    {
        int r = _origVk(q, p);
        if (_frameCount == 0) Mono.Log("[UI] Vulkan detected");
        _frameCount++;
        return r;
    }

    static void RenderImGui()
    {
        _frameCount++;
        try {
            if (_frameCount <= 3) Mono.Log($"[UI] Frame#{_frameCount} Sz={ImGui.GetIO().DisplaySize} FPS={ImGui.GetIO().Framerate:F0}");
            _instance?.RenderFrame();
        } catch (Exception ex) {
            if (_frameCount <= 3) Mono.Log($"[UI] Frame#{_frameCount} CRASH: {ex}");
        }
    }

    void _InitImGui()
    {
        _context = ImGui.CreateContext(); ImGui.SetCurrentContext(_context); ImGui.StyleColorsDark();
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(1080, 2400); io.DisplayFramebufferScale = Vector2.One;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        unsafe { io.Fonts.AddFontDefault(); }
    }

    void RenderFrame()
    {
        if (_context != ImGui.GetCurrentContext()) ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO(); var vp = new int[4]; glGetIntegerv(GL_VIEWPORT, vp);
        if (vp[2] > 0 && vp[3] > 0) io.DisplaySize = new Vector2(vp[2], vp[3]);
        ImGui.NewFrame();
        if (_showMainWindow) DrawMainWindow();
        if (_showDemoWindow) ImGui.ShowDemoWindow(ref _showDemoWindow);
        ImGui.Render();
        ImGuiGLES.RenderDrawData(ImGui.GetDrawData());
    }

    void DrawMainWindow()
    {
        var io = ImGui.GetIO();
        ImGui.SetNextWindowSize(new Vector2(420, 560), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(20, 40), ImGuiCond.FirstUseEver);
        ImGui.Begin("StArray Mod Loader", ref _showMainWindow, ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoCollapse);
        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("File")) { if (ImGui.MenuItem("Refresh")) RefreshMods(); ImGui.Separator(); if (ImGui.MenuItem("Exit")) _showMainWindow = false; ImGui.EndMenu(); }
            if (ImGui.BeginMenu("View")) { ImGui.MenuItem("Demo", "", ref _showDemoWindow); ImGui.EndMenu(); }
            ImGui.EndMenuBar();
        }
        ImGui.TextColored(new Vector4(0.3f, 1, 0.3f, 1), "Running");
        ImGui.SameLine(180); ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), $"FPS:{io.Framerate:F0}");
        ImGui.Separator(); ImGui.InputText("Search", ref _searchText, 64);
        ImGui.BeginChild("List", new Vector2(0, -40));
        for (int i = 0; i < _mods.Count; i++) {
            if (!string.IsNullOrEmpty(_searchText) && _mods[i].IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (ImGui.Selectable(_mods[i], _selectedMod == i)) _selectedMod = i;
        }
        ImGui.EndChild();
        if (ImGui.Button("Refresh", new Vector2(90, 0))) RefreshMods();
        ImGui.SameLine(); if (ImGui.Button("Unload", new Vector2(90, 0)) && _selectedMod >= 0) Mono.Log($"Unload:{_mods[_selectedMod]}");
        ImGui.SameLine(); if (ImGui.Button("Reload", new Vector2(90, 0))) RefreshMods();
        ImGui.End();
    }

    void RefreshMods() {
        _mods.Clear(); var d = Path.Combine(Mono.LogDir, "mods");
        if (Directory.Exists(d)) foreach (var x in Directory.GetDirectories(d)) _mods.Add(Path.GetFileName(x));
        if (_mods.Count == 0) _mods.Add("(none)");
    }
}
