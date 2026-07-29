using System.Numerics;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ImGuiNET;
using OpenTK.Graphics.Egl;
using OpenTK.Graphics.ES30;
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
        try
        {
            if (OnEglSwapBuffers != null)
                return OnEglSwapBuffers(display, surface);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(EglHooks), $"HookEglSwapBuffers error: {ex}");
        }

        return CallOriginal(display, surface);
    }

    internal static int CallOriginal(IntPtr display, IntPtr surface)
    {
        try
        {
            return HookEglSwapBuffersOriginal(display, surface);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(EglHooks), $"eglSwapBuffers original call failed: {ex}");
            return 0;
        }
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
    public static bool Install()
    {
        if (s_instance != null)
            return true;

        var renderer = new ImGuiEGLRenderer();
        s_instance = renderer;
        if (renderer.InstallInstance())
            return true;

        s_instance = null;
        return false;
    }

    /// <summary> 静态 OnRender 事件（Install 之前订阅会缓存） </summary>
    private static Action? s_pendingOnRender;
    private static Func<bool>? s_pendingShouldRenderWhenHidden;

    public static event Action OnRender
    {
        add
        {
            if (s_instance != null)
                s_instance.AddRenderCallback(value);
            else
                s_pendingOnRender += value;
        }
        remove
        {
            if (s_instance != null)
                s_instance.RemoveRenderCallback(value);
            else
                s_pendingOnRender -= value;
        }
    }

    private bool _initialized;
    private bool _initFailed;
    private bool _glBindingsLoaded;
    private long _lastFrameTimestamp;
    private readonly int[] _viewport = new int[4];
    private readonly object _renderCallbackLock = new();
    private Action[] _renderCallbacks = Array.Empty<Action>();
    private Func<bool>? _shouldRenderWhenHidden;

    event Action IImGuiRenderer.OnRender
    {
        add => AddRenderCallback(value);
        remove => RemoveRenderCallback(value);
    }

    /// <summary>渲染器是否已初始化</summary>
    public bool IsInitialized => _initialized;

    /// <summary> 实例安装（实现 IImGuiRenderer） </summary>
    bool IImGuiRenderer.Install() => InstallInstance();

    private bool InstallInstance()
    {
        HookHelper.Instance = new DobbyHook();
        ImGuiInputHandler.RegisterImeCallbacks();
        try
        {
            EglHooks.InstallHooks();
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiEGLRenderer), $"EGL hook install failed: {ex}");
            return false;
        }
        EglHooks.OnEglSwapBuffers = OnSwapBuffers;

        if (EnvEnabled("STARRAY_MODMANAGER_ENABLE_INPUT_HOOKS", false))
        {
            try
            {
                ImGuiInputHandler.InstallHooks();
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(ImGuiEGLRenderer), $"Input hook install failed: {ex}");
            }
        }
        else
        {
            Logger.Info(nameof(ImGuiEGLRenderer), "Input hooks disabled");
        }

        // 回放 Install 之前缓存的静态事件订阅
        if (s_pendingOnRender != null)
        {
            foreach (var callback in s_pendingOnRender.GetInvocationList())
                AddRenderCallback((Action)callback);
            s_pendingOnRender = null;
        }
        if (s_pendingShouldRenderWhenHidden != null)
        {
            _shouldRenderWhenHidden = s_pendingShouldRenderWhenHidden;
            s_pendingShouldRenderWhenHidden = null;
        }

        Logger.Info(nameof(ImGuiEGLRenderer), "eglSwapBuffers hooked via [NativeHook]");
        return true;
    }

    private static bool EnvEnabled(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    public static int OnSwapBuffers(IntPtr display, IntPtr surface)
    {
        var self = s_instance;
        if (self == null)
            return EglHooks.CallOriginal(display, surface);

        var frameOpen = false;
        try
        {
            if (!OverlayUiIsVisible() && !self.ShouldRenderWhenHidden())
            {
                return EglHooks.CallOriginal(display, surface);
            }

            if (!self._initialized)
            {
                if (!self.InitImGui(display, surface))
                    return EglHooks.CallOriginal(display, surface);
            }

            if (!self.TryGetViewportSize(out var width, out var height))
                return EglHooks.CallOriginal(display, surface);

            ImGuiImplOpenGL3.NewFrame();
            self.UpdatePlatformFrame(width, height);
            ImGuiImplAndroid.DrainForwardedMotionEvents();

            ImGui.NewFrame();
            frameOpen = true;

            // 构建 UI
            self.BuildUI();
            ImGuiInputHandler.UpdateIme();
            // 渲染
            ImGui.Render();
            frameOpen = false;
            ImGuiImplOpenGL3.RenderDrawData((IntPtr)ImGui.GetDrawData().NativePtr);

            // 渲染后检查 surface 是否已被废弃
            var err = Egl.GetError();
            if (err != OpenTK.Graphics.Egl.ErrorCode.SUCCESS)
            {
                Logger.Warn(nameof(ImGuiEGLRenderer), $"EGL error after render: 0x{err:X}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiEGLRenderer), $"OnSwapBuffers error: {ex}");
            if (frameOpen)
            {
                try
                {
                    ImGui.EndFrame();
                }
                catch (Exception endFrameEx)
                {
                    Logger.Error(nameof(ImGuiEGLRenderer), $"EndFrame after render error failed: {endFrameEx}");
                }
            }
        }

        return EglHooks.CallOriginal(display, surface);
    }

    private static bool OverlayUiIsVisible()
    {
        try
        {
            return modmanager_overlay_ui_is_visible() != 0;
        }
        catch
        {
            return true;
        }
    }

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_is_visible")]
    private static extern int modmanager_overlay_ui_is_visible();

    private bool ShouldRenderWhenHidden()
    {
        try
        {
            return _shouldRenderWhenHidden?.Invoke() == true;
        }
        catch (Exception ex)
        {
            Logger.Warn(nameof(ImGuiEGLRenderer), $"Hidden overlay render predicate failed: {ex.Message}");
            return false;
        }
    }

    public static void SetRenderWhenHiddenPredicate(Func<bool>? predicate)
    {
        if (s_instance != null)
            s_instance._shouldRenderWhenHidden = predicate;
        else
            s_pendingShouldRenderWhenHidden = predicate;
    }

    private bool TryGetViewportSize(out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            EnsureGLBindings();
            GL.GetInteger(GetPName.Viewport, _viewport);
            width = _viewport[2];
            height = _viewport[3];
            return width > 0 && height > 0;
        }
        catch (Exception ex)
        {
            Logger.Warn(nameof(ImGuiEGLRenderer), $"Viewport query skipped: {ex.Message}");
            return false;
        }
    }

    private void EnsureGLBindings()
    {
        if (_glBindingsLoaded)
            return;

        GL.LoadBindings(new GLESBindingsContext());
        _glBindingsLoaded = true;
    }

    private void UpdatePlatformFrame(int width, int height)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(width, height);
        io.DisplayFramebufferScale = Vector2.One;

        var now = Stopwatch.GetTimestamp();
        if (_lastFrameTimestamp == 0)
        {
            io.DeltaTime = 1.0f / 60.0f;
        }
        else
        {
            var delta = (float)((double)(now - _lastFrameTimestamp) / Stopwatch.Frequency);
            io.DeltaTime = delta > 0.0f && delta < 1.0f ? delta : 1.0f / 60.0f;
        }
        _lastFrameTimestamp = now;
    }

    private bool InitImGui(IntPtr display, IntPtr surface)
    {
        if (_initialized) return true;
        if (_initFailed) return false;

        try
        {
            Logger.Info(nameof(ImGuiEGLRenderer), "Initializing ImGui with official backends...");
            EnsureGLBindings();
            if (TryGetViewportSize(out var width, out var height))
                Logger.Info(nameof(ImGuiEGLRenderer), $"Viewport size: {width}x{height}");

            // 创建 ImGui 上下文，并显式设置为当前上下文。
            var context = ImGui.CreateContext();
            if (context == IntPtr.Zero)
                throw new InvalidOperationException("ImGui.CreateContext returned null");

            ImGui.SetCurrentContext(context);
            var currentContext = ImGui.GetCurrentContext();
            if (currentContext == IntPtr.Zero)
                throw new InvalidOperationException("ImGui current context is null after CreateContext");

            var io = ImGui.GetIO();
            if ((IntPtr)io.NativePtr == IntPtr.Zero)
                throw new InvalidOperationException("ImGui.GetIO returned null");
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            // 设置缩放
            io.FontGlobalScale = 3.0f;

            AndroidImGuiFontLoader.EnsureLoaded(io);

            // 设置样式
            var style = ImGui.GetStyle();
            style.ScaleAllSizes(2.0f);
            ImGui.StyleColorsClassic();

            ImGuiImplOpenGL3.Init();

            Logger.Info(nameof(ImGuiEGLRenderer),
                "Touch input handled via forwarded motion queue; Android backend window queries disabled");

            _initialized = true;
            ImGuiInputHandler.IsInitialized = true;
            Logger.Info(nameof(ImGuiEGLRenderer),
                "ImGui initialized with official OpenGL3 + Android input backends");
            return true;
        }
        catch (Exception ex)
        {
            _initFailed = true;
            ImGuiInputHandler.IsInitialized = false;
            Logger.Error(nameof(ImGuiEGLRenderer), $"InitImGui failed permanently: {ex}");
            return false;
        }
    }

    private void BuildUI()
    {
        var callbacks = Volatile.Read(ref _renderCallbacks);
        foreach (var callback in callbacks)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(ImGuiEGLRenderer), $"OnRender callback failed: {ex}");
            }
        }
    }

    private void AddRenderCallback(Action callback)
    {
        lock (_renderCallbackLock)
        {
            var current = _renderCallbacks;
            var updated = new Action[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[^1] = callback;
            Volatile.Write(ref _renderCallbacks, updated);
        }
    }

    private void RemoveRenderCallback(Action callback)
    {
        lock (_renderCallbackLock)
        {
            var current = _renderCallbacks;
            var index = Array.LastIndexOf(current, callback);
            if (index < 0)
                return;

            if (current.Length == 1)
            {
                Volatile.Write(ref _renderCallbacks, Array.Empty<Action>());
                return;
            }

            var updated = new Action[current.Length - 1];
            if (index > 0)
                Array.Copy(current, 0, updated, 0, index);
            if (index < current.Length - 1)
                Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
            Volatile.Write(ref _renderCallbacks, updated);
        }
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

    public static void SetRenderWhenHiddenPredicate(Func<bool>? predicate)
        => ImGuiEGLRenderer.SetRenderWhenHiddenPredicate(predicate);

    /// <summary>每帧渲染事件</summary>
    public static event Action OnRender
    {
        add => ImGuiEGLRenderer.OnRender += value;
        remove => ImGuiEGLRenderer.OnRender -= value;
    }
}
