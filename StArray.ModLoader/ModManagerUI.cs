using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace StArray.ModLoader;

/// <summary>
/// Mod 管理器 UI — 纯 C# 实现。
/// Hook eglSwapBuffers 叠加 ImGui 到 Unity 画面之上。
/// 不需要 native imgui_overlay.cpp。
/// </summary>
public class ModManagerUI
{
    // ========================================================================
    // P/Invoke — eglSwapBuffers delegate
    // ========================================================================
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int EglSwapBuffersDelegate(IntPtr display, IntPtr surface);

    [DllImport("libGLESv2.so")]
    static extern void glGetIntegerv(int pname, int[] data);
    const int GL_VIEWPORT = 0x0BA2;

    // ========================================================================
    // State
    // ========================================================================
    static ModManagerUI? _instance;
    static IntPtr _context;
    bool _showMainWindow = true;
    bool _showDemoWindow;
    string _searchText = "";
    readonly List<string> _mods = new() { "Loading..." };
    int _selectedMod = -1;
    static bool _installed;
    static EglSwapBuffersDelegate _origSwap = null!;

    // ========================================================================
    // Install — called from Mono.Entry()
    // ========================================================================
    public static void Install()
    {
        if (_installed) return;

        _instance = new ModManagerUI();
        _instance._InitImGui();

        nint addr = Dobby.SymbolResolver("libEGL.so", "eglSwapBuffers");
        if (addr == IntPtr.Zero)
            addr = Dobby.SymbolResolver("libGLESv2.so", "eglSwapBuffers");
        if (addr == IntPtr.Zero) { Mono.Log("[UI] eglSwapBuffers not found"); return; }

        Mono.Log($"[UI] eglSwapBuffers @ 0x{addr:X}");

        var hookMethod = typeof(ModManagerUI).GetMethod(nameof(EglSwapHook),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        int ret = Dobby.Hook(addr, hookMethod, out nint origPtr);
        Mono.Log($"[UI] Hook eglSwapBuffers: ret={ret} orig=0x{origPtr:X}");

        _origSwap = Marshal.GetDelegateForFunctionPointer<EglSwapBuffersDelegate>(origPtr);
        _installed = true;

        ImGuiGLES.Init();
    }

    // ========================================================================
    // eglSwapBuffers hook
    // ========================================================================
    static int EglSwapHook(IntPtr display, IntPtr surface)
    {
        // 1. 先让 Unity 正常 swap
        int ret = _origSwap(display, surface);
        // 2. 叠加 ImGui
        try { _instance?.RenderFrame(); } catch { }
        return ret;
    }

    // ========================================================================
    // ImGui init / frame
    // ========================================================================
    void _InitImGui()
    {
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        ImGui.StyleColorsDark();
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(1080, 2400);
        io.DisplayFramebufferScale = Vector2.One;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        unsafe { io.Fonts.AddFontDefault(); }
    }

    void RenderFrame()
    {
        if (_context != ImGui.GetCurrentContext()) ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();
        var vp = new int[4];
        glGetIntegerv(GL_VIEWPORT, vp);
        if (vp[2] > 0 && vp[3] > 0) io.DisplaySize = new Vector2(vp[2], vp[3]);

        ImGui.NewFrame();
        if (_showMainWindow) DrawMainWindow();
        if (_showDemoWindow) ImGui.ShowDemoWindow(ref _showDemoWindow);
        ImGui.Render();
        ImGuiGLES.RenderDrawData(ImGui.GetDrawData());
    }

    // ========================================================================
    // 触摸输入转发
    // ========================================================================
    public static void FeedMouseButton(int button, bool pressed)
        => ImGui.GetIO().AddMouseButtonEvent(button, pressed);

    public static void FeedMousePos(float x, float y)
        => ImGui.GetIO().AddMousePosEvent(x, y);

    public static void FeedScroll(float dx, float dy)
        => ImGui.GetIO().AddMouseWheelEvent(dx, dy);

    public static void Toggle()
        { if (_instance != null) _instance._showMainWindow = !_instance._showMainWindow; }

    // ========================================================================
    // Main Window
    // ========================================================================
    void DrawMainWindow()
    {
        var io = ImGui.GetIO();
        ImGui.SetNextWindowSize(new Vector2(420, 560), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(20, 40), ImGuiCond.FirstUseEver);

        ImGui.Begin("StArray Mod Loader", ref _showMainWindow,
            ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoCollapse);

        // Menu Bar
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Refresh Mods")) RefreshMods();
                ImGui.Separator();
                if (ImGui.MenuItem("Exit")) _showMainWindow = false;
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("ImGui Demo", "", ref _showDemoWindow);
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        ImGui.TextColored(new Vector4(0.3f, 1, 0.3f, 1), "Status: Running");
        ImGui.SameLine(180);
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), $"FPS: {io.Framerate:F0}");
        ImGui.Separator();
        ImGui.InputText("Search", ref _searchText, 64);

        // Mod List
        ImGui.BeginChild("ModList", new Vector2(0, -40));
        for (int i = 0; i < _mods.Count; i++)
        {
            if (!string.IsNullOrEmpty(_searchText) &&
                _mods[i].IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (ImGui.Selectable(_mods[i], _selectedMod == i))
                _selectedMod = i;
        }
        ImGui.EndChild();

        // Bottom buttons
        if (ImGui.Button("Refresh", new Vector2(90, 0))) RefreshMods();
        ImGui.SameLine();
        if (ImGui.Button("Unload", new Vector2(90, 0)) && _selectedMod >= 0)
            Mono.Log($"Unload: {_mods[_selectedMod]}");
        ImGui.SameLine();
        if (ImGui.Button("Reload All", new Vector2(90, 0))) RefreshMods();
        ImGui.End();
    }

    void RefreshMods()
    {
        _mods.Clear();
        string modsDir = Path.Combine(Mono.LogDir, "mods");
        if (Directory.Exists(modsDir))
            foreach (var dir in Directory.GetDirectories(modsDir))
                _mods.Add(Path.GetFileName(dir));
        if (_mods.Count == 0) _mods.Add("(no mods found)");
        Mono.Log($"Mods refreshed: {_mods.Count} found");
    }
}
