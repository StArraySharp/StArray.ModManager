using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.Egl;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;

namespace StArray.ModManager.Android.UI;

// ─── NativeHook 定义 ─────────────────────────────────
public static partial class EglHooks
{
    internal static Func<IntPtr, IntPtr, int>? OnEglSwapBuffers;

    [NativeHook("libEGL.so", "eglSwapBuffers", Convention = CallingConvention.Cdecl)]
    public static int HookEglSwapBuffers(IntPtr display, IntPtr surface)
    {
        if (OnEglSwapBuffers != null)
            return OnEglSwapBuffers(display, surface);
        return HookEglSwapBuffersOriginal(display, surface);
    }
}

/// <summary>ImGui EGL renderer / EGL 渲染器 — SwapBuffers hook, init, render pipeline</summary>
public sealed unsafe class ImGuiEGLRenderer : IImGuiRenderer
{
    private static ImGuiEGLRenderer? s_instance;

    /// <summary> 获取渲染器单例（Install 之后可用） </summary>
    public static ImGuiEGLRenderer Instance =>
        s_instance ?? throw new InvalidOperationException("Renderer not installed");

    /// <summary> 静态安装入口（供原生宿主调用） </summary>
    public static bool Install() => (s_instance = new ImGuiEGLRenderer()).InstallInstance();

    /// <summary> 静态 OnRender 事件（Install 之前订阅会缓存） </summary>
    private static Action? s_pendingOnRender;

    public static event Action OnRender
    {
        add
        {
            if (s_instance != null)
                s_instance._onRender += value;
            else
                s_pendingOnRender += value;
        }
        remove
        {
            if (s_instance != null)
                s_instance._onRender -= value;
            else
                s_pendingOnRender -= value;
        }
    }

    private bool _initialized;
    private Action _onRender = () => { };

    event Action IImGuiRenderer.OnRender
    {
        add => _onRender += value;
        remove => _onRender -= value;
    }

    /// <summary>渲染器是否已初始化</summary>
    public bool IsInitialized => _initialized;

    /// <summary> 实例安装（实现 IImGuiRenderer） </summary>
    bool IImGuiRenderer.Install() => InstallInstance();

    private bool InstallInstance()
    {
        HookHelper.Instance = new DobbyHook();
        EglHooks.InstallHooks();
        EglHooks.OnEglSwapBuffers = OnSwapBuffers;



        // 回放 Install 之前缓存的静态事件订阅
        if (s_pendingOnRender != null)
        {
            _onRender += s_pendingOnRender;
            s_pendingOnRender = null;
        }
        return true;
    }

    public static int OnSwapBuffers(IntPtr display, IntPtr surface)
    {
        var self = s_instance!;
        try
        {
            // 检查 surface 是否仍然有效
            Egl.GetError(); // 清除之前的错误

            // 使用 EGL 查询 surface 尺寸
            if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width) ||
                !Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
            {
                return EglHooks.HookEglSwapBuffersOriginal(display, surface);
            }

            if (width <= 0 || height <= 0)
            {
                return EglHooks.HookEglSwapBuffersOriginal(display, surface);
            }

            if (!self._initialized)
            {
                self.InitImGui(display, surface);
            }

            // 使用官方 backend 的 NewFrame
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplAndroid.NewFrame();

            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);

            ImGui.NewFrame();

            // 构建 UI
            self.BuildUI();
            ImGuiInputHandler.UpdateIme();
            // 渲染
            ImGui.Render();
            ImGuiImplOpenGL3.RenderDrawData((IntPtr)ImGui.GetDrawData().NativePtr);

            // 渲染后检查 surface 是否已被废弃
            var err = Egl.GetError();
            if (err != ErrorCode.SUCCESS)
            {
                Logger.Warn(nameof(ImGuiEGLRenderer), $"EGL error after render: 0x{err:X}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiEGLRenderer), $"OnSwapBuffers error: {ex}");
        }

        return EglHooks.HookEglSwapBuffersOriginal(display, surface);
    }

    private void InitImGui(IntPtr display, IntPtr surface)
    {
        if (_initialized) return;
        // 从 EGL surface 获取 ANativeWindow
        if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width) ||
            !Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
        {
            Logger.Error(nameof(ImGuiEGLRenderer), "Failed to query EGL surface for initialization");
            return;
        }
        // 创建 ImGui 上下文 + 加载嵌入式字体 msyh + FA 图标（共享接口）
        ((IImGuiRenderer)this).InitImGui();
        ImGuiInputHandler.IsInitialized = true;
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.FontGlobalScale = 3.0f;

        // 设置样式
        var style = ImGui.GetStyle();
        style.ScaleAllSizes(2.0f);
        ImGui.StyleColorsClassic();

        // 初始化官方 backends
        var nativeWindow = AndroidUtils.GetUnityNativeWindow();
        if (nativeWindow != IntPtr.Zero)
        {
            ImGuiImplAndroid.Init(nativeWindow);
        }

        ImGuiImplOpenGL3.Init();
        ImGuiInputHandler.InstallInputHooks();
        _initialized = true;
    }

    private void BuildUI()
    {
        _onRender?.Invoke();
    }
}

/// <summary>
/// ImGuiEGLRender 静态外观 —— 为原生宿主提供与旧版 API 兼容的入口
/// 内部委托给 <see cref="ImGuiEGLRenderer"/> 单例
/// </summary>
public static class ImGuiEGLRender
{
    /// <summary>安装 EGL 渲染器</summary>
    public static bool Install() => ImGuiEGLRenderer.Install();

    /// <summary>每帧渲染事件</summary>
    public static event Action OnRender
    {
        add => ImGuiEGLRenderer.OnRender += value;
        remove => ImGuiEGLRenderer.OnRender -= value;
    }
}
