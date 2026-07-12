using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows.UI;

public static partial class WglHooks
{
    internal static Func<IntPtr, int>? OnWglSwap;

    [NativeHook("opengl32.dll", "wglSwapBuffers")]
    public static int HookWglSwapBuffers(IntPtr hdc)
    {
        if (OnWglSwap != null)
            return OnWglSwap(hdc);
        return HookWglSwapBuffersOriginal(hdc);
    }
}

public sealed unsafe class ImGUIGLRenderer : IImGuiRenderer
{
    private static ImGUIGLRenderer? s_instance;

    public static ImGUIGLRenderer Instance =>
        s_instance ?? throw new InvalidOperationException("OpenGL renderer not installed");

    public static bool Install() =>
        (s_instance = new ImGUIGLRenderer()).InstallInstance();

    private static Action? s_pendingOnRender;

    public static event Action OnRender
    {
        add { if (s_instance != null) s_instance._onRender += value; else s_pendingOnRender += value; }
        remove { if (s_instance != null) s_instance._onRender -= value; else s_pendingOnRender -= value; }
    }

    private bool _initialized;
    private event Action _onRender = () => { };
    private bool _imguiInited;
    private IntPtr _hwnd;
    private IntPtr _origWndProc;

    event Action IImGuiRenderer.OnRender { add => _onRender += value; remove => _onRender -= value; }
    public bool IsInitialized => _initialized;
    bool IImGuiRenderer.Install() => InstallInstance();

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromDC(IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;

    private bool InstallInstance()
    {
        try
        {
            HookHelper.Instance = new MinHook();
            WglHooks.InstallHooks();
            WglHooks.OnWglSwap = OnWglSwapBuffers;
            if (s_pendingOnRender != null) { _onRender += s_pendingOnRender; s_pendingOnRender = null; }
            _initialized = true;
            Logger.Info(nameof(ImGUIGLRenderer), "wglSwapBuffers hooked");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGUIGLRenderer), $"Install failed: {ex.Message}");
            return false;
        }
    }

    private int OnWglSwapBuffers(IntPtr hdc)
    {
        if (!_imguiInited) TryInitImGui(hdc);
        if (!_imguiInited) return WglHooks.HookWglSwapBuffersOriginal(hdc);

        ImGui_ImplOpenGL3_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui.NewFrame();

        try { _onRender(); }
        catch (Exception ex) { Logger.Error(nameof(ImGUIGLRenderer), $"Render error: {ex.Message}"); }

        ImGui.Render();
        ImGui_ImplOpenGL3_RenderDrawData((IntPtr)ImGui.GetDrawData().NativePtr);
        return WglHooks.HookWglSwapBuffersOriginal(hdc);
    }

    private void TryInitImGui(IntPtr hdc)
    {
        _hwnd = WindowFromDC(hdc);
        if (_hwnd == nint.Zero) { _imguiInited = false; return; }

        var ctx = ImGui.CreateContext();
        ImGui.SetCurrentContext(ctx);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;

        var yaheiPath = @"C:\Windows\Fonts\msyh.ttc";
        if (File.Exists(yaheiPath))
            io.Fonts.AddFontFromFileTTF(yaheiPath, 16.0f, null,
                io.Fonts.GetGlyphRangesChineseSimplifiedCommon());

        if (!ImGui_ImplWin32_Init(_hwnd)) { _imguiInited = false; return; }
        if (!ImGui_ImplOpenGL3_Init()) { _imguiInited = false; return; }

        _origWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(new WndProcDelegate(WndProcHook)));

        _imguiInited = true;
        Logger.Info(nameof(ImGUIGLRenderer), "ImGui OpenGL initialized");
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (_imguiInited && ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
            return (IntPtr)1;
        return CallWindowProc(_origWndProc, hWnd, msg, wParam, lParam);
    }

    // ─── cimgui P/Invoke ─────────────────────────────

    [DllImport("cimgui", EntryPoint = "ImGui_ImplOpenGL3_Init")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool ImGui_ImplOpenGL3_Init(string? glslVersion = null);
    [DllImport("cimgui", EntryPoint = "ImGui_ImplOpenGL3_NewFrame")]
    private static extern void ImGui_ImplOpenGL3_NewFrame();
    [DllImport("cimgui", EntryPoint = "ImGui_ImplOpenGL3_RenderDrawData")]
    private static extern void ImGui_ImplOpenGL3_RenderDrawData(IntPtr drawData);
    [DllImport("cimgui", EntryPoint = "ImGui_ImplWin32_Init")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool ImGui_ImplWin32_Init(IntPtr hwnd);
    [DllImport("cimgui", EntryPoint = "ImGui_ImplWin32_InitForOpenGL")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool ImGui_ImplWin32_InitForOpenGL(IntPtr hwnd);
    [DllImport("cimgui", EntryPoint = "ImGui_ImplWin32_NewFrame")]
    private static extern void ImGui_ImplWin32_NewFrame();
    [DllImport("cimgui", EntryPoint = "ImGui_ImplWin32_WndProcHandler")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool ImGui_ImplWin32_WndProcHandler(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
