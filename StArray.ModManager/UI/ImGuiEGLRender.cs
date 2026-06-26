using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.Egl;
using StArray.ModManager.Native;
using StArray.ModManager.Manager;

namespace StArray.ModManager.UI;

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

    private SwapBuffersDelegate? _prevSwapBuffersDelegate;
    private delegate int SwapBuffersDelegate(IntPtr display, IntPtr surface);

    event Action IImGuiRenderer.OnRender
    {
        add => _onRender += value;
        remove => _onRender -= value;
    }

    public bool IsInitialized => _initialized;

    /// <summary> 实例安装（实现 IImGuiRenderer） </summary>
    bool IImGuiRenderer.Install() => InstallInstance();

    private bool InstallInstance()
    {
        var eglLib = DL.dlopen("libEGL.so", DL.Flags.RTLD_GLOBAL);
        if (eglLib == IntPtr.Zero)
        {
            eglLib = DL.dlopen("libGLESv3.so", DL.Flags.RTLD_GLOBAL);
        }

        var glSwapBuffersPtr = NativeLibrary.GetExport(eglLib, "eglSwapBuffers");
        Dobby.Hook(glSwapBuffersPtr,
            typeof(ImGuiEGLRenderer).GetMethod(nameof(OnSwapBuffers))!.MethodHandle.GetFunctionPointer(),
            out var prevSwapBuffers);
        _prevSwapBuffersDelegate = Marshal.GetDelegateForFunctionPointer<SwapBuffersDelegate>(prevSwapBuffers);

        // 输入 Hook 委托给静态处理器
        ImGuiInputHandler.InstallHooks();

        // 回放 Install 之前缓存的静态事件订阅
        if (s_pendingOnRender != null)
        {
            _onRender += s_pendingOnRender;
            s_pendingOnRender = null;
        }

        Logger.Error(nameof(ImGuiEGLRenderer), $"eglSwapBuffers hooked at 0x{glSwapBuffersPtr:X}");
        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
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
                return self._prevSwapBuffersDelegate!(display, surface);
            }

            if (width <= 0 || height <= 0)
            {
                return self._prevSwapBuffersDelegate!(display, surface);
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

        return self._prevSwapBuffersDelegate!(display, surface);
    }

    private void InitImGui(IntPtr display, IntPtr surface)
    {
        if (_initialized) return;

        Logger.Error(nameof(ImGuiEGLRenderer), "Initializing ImGui with official backends...");

        // 从 EGL surface 获取 ANativeWindow
        if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width) ||
            !Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
        {
            Logger.Error(nameof(ImGuiEGLRenderer), "Failed to query EGL surface for initialization");
            return;
        }

        Logger.Error(nameof(ImGuiEGLRenderer), $"Surface size: {width}x{height}");

        // 创建 ImGui 上下文
        ImGui.CreateContext();
        ImGuiInputHandler.IsInitialized = true;

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        // 设置缩放
        io.FontGlobalScale = 3.0f;

        // 加载中文字体作为默认字体（含 ASCII + CJK）
        string[] cjkPaths = [
            "/system/fonts/NotoSansCJK-Regular.ttc",
        ];
        bool cjkLoaded = false;
        foreach (var path in cjkPaths)
        {
            if (File.Exists(path))
            {
                var cjk = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();
                io.Fonts.AddFontFromFileTTF(path, 16.0f, null, cjk);
                Logger.Info(nameof(ImGuiEGLRenderer), $"CJK font: {path}");
                cjkLoaded = true;
                break;
            }
        }
        if (!cjkLoaded)
            io.Fonts.AddFontDefault(); // 兜底

        // 合并 FontAwesome 图标字体（merge mode，私用区 U+E005~U+F8FF）
        LoadFontAwesome(io);

        // 设置样式
        var style = ImGui.GetStyle();
        style.ScaleAllSizes(2.0f);
        ImGui.StyleColorsClassic();

        // 初始化官方 backends
        var nativeWindow = AndroidUtils.GetUnityNativeWindow();
        if (nativeWindow != IntPtr.Zero)
        {
            ImGuiImplAndroid.Init(nativeWindow);
            Logger.Error(nameof(ImGuiEGLRenderer),
                $"ImGui_ImplAndroid_Init success with window: 0x{nativeWindow:X}");
        }
        else
        {
            Logger.Error(nameof(ImGuiEGLRenderer),
                "Failed to get Unity ANativeWindow, touch input may not work");
        }

        ImGuiImplOpenGL3.Init();

        Logger.Error(nameof(ImGuiEGLRenderer),
            "Touch input handled via ImGui_ImplAndroid");

        _initialized = true;
        Logger.Error(nameof(ImGuiEGLRenderer),
            "ImGui initialized with official OpenGL3 + Android input backends");
    }

    private void BuildUI()
    {
        _onRender?.Invoke();
    }

    /// <summary>
    /// 从嵌入资源加载 FontAwesome 图标字体，合并到 CJK 字体 atlas
    /// </summary>
    private static void LoadFontAwesome(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(ImGuiEGLRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(
                "StArray.ModManager.Resources.fa-solid-900.ttf");
            if (stream == null) return;

            var ttf = new byte[stream.Length];
            stream.ReadExactly(ttf);
            var ptr = Marshal.AllocHGlobal(ttf.Length);
            Marshal.Copy(ttf, 0, ptr, ttf.Length);

            // merge mode: 追加到已有字体，不替换
            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg->MergeMode = 1;

            // FontAwesome 7 图标范围（私用区）
            ushort[] iconRange = [0xe005, 0xf8ff, 0];

            fixed (ushort* r = iconRange)
                io.Fonts.AddFontFromMemoryTTF(ptr, ttf.Length, 16f, cfg, (IntPtr)r);

            io.Fonts.Build();
            Logger.Info(nameof(ImGuiEGLRenderer),
                $"FontAwesome merged ({ttf.Length} bytes)");
        }
        catch { /* 找不到资源则静默跳过 */ }
    }
}

/// <summary>
/// ImGuiEGLRender 静态外观 —— 为原生宿主提供与旧版 API 兼容的入口
/// 内部委托给 <see cref="ImGuiEGLRenderer"/> 单例
/// </summary>
public static class ImGuiEGLRender
{
    public static bool Install() => ImGuiEGLRenderer.Install();

    public static event Action OnRender
    {
        add => ImGuiEGLRenderer.OnRender += value;
        remove => ImGuiEGLRenderer.OnRender -= value;
    }
}
