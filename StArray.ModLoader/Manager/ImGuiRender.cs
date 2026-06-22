using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.Egl;
using StArray.ModLoader.PInvoke;
using StArray.ModLoader.ImGui;
using StArray.ModLoader.Java;

namespace StArray.ModLoader.Manager;

/// <summary>
/// ImGui EGL 渲染器（使用官方 backends）
/// </summary>
public static unsafe class ImGuiRender
{
    private static bool _initialized;
    private static MotionInputProvider? _inputProvider;
    private static string _inputText = string.Empty;
    private static bool _wantTextInputLast;
    
    private static SwapBuffersDelegate? _prevSwapBuffersDelegate;
    delegate int SwapBuffersDelegate(IntPtr display, IntPtr surface);

    private static InitializeMotionEventDelegate _initializeMotionEvent;
    private static InitializeKeyEventDelegate _initializeKeyEvent;
    delegate int InitializeMotionEventDelegate(IntPtr self, IntPtr motionEvent, IntPtr message);
    delegate int InitializeKeyEventDelegate(IntPtr self, IntPtr keyEvent, IntPtr message);
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnSwapBuffers(IntPtr display, IntPtr surface)
    {
        try
        {
            // 使用 EGL 查询 surface 尺寸
            if (!EGL.QuerySurface(display, surface, EGL.EGL_WIDTH, out var width) ||
                !EGL.QuerySurface(display, surface, EGL.EGL_HEIGHT, out var height))
            {
                return _prevSwapBuffersDelegate(display, surface);
            }

            if (width <= 0 || height <= 0)
            {
                return _prevSwapBuffersDelegate(display, surface);
            }

            if (!_initialized)
            {
                InitImGui(display, surface);
            }

            // 使用官方 backend 的 NewFrame
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplAndroid.NewFrame();  // 现在有 ANativeWindow 了，可以调用
            
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);
            
            
            // 更新触摸输入到 ImGui
            if (_inputProvider != null)
            {
                _inputProvider.UpdateInput(io);
            }
            
            ImGuiNET.ImGui.NewFrame();
            
            // 构建 UI
            BuildUI();
            
            // IME: WantTextInput 上升沿→发送文本并弹键盘，下降沿→隐藏
            if (io.WantTextInput && !_wantTextInputLast)
            {
                ImeShow(_inputText);
            }
            else if (!io.WantTextInput && _wantTextInputLast)
            {
                ImeHide();
            }
            _wantTextInputLast = io.WantTextInput;
            
            // 渲染
            ImGuiNET.ImGui.Render();
            ImGuiImplOpenGL3.RenderDrawData((IntPtr)ImGuiNET.ImGui.GetDrawData().NativePtr);
        }
        catch (Exception ex)
        {
            AndroidLog.Error(nameof(ImGuiRender), $"OnSwapBuffers error: {ex}");
        }
        return _prevSwapBuffersDelegate(display, surface);
    }

    public static bool Install()
    {
        var eglLib = DL.dlopen("libEGL.so", DL.Flags.RTLD_GLOBAL);
        if (eglLib == IntPtr.Zero)
        {
            eglLib = DL.dlopen("libGLESv3.so", DL.Flags.RTLD_GLOBAL);
        }
        
        var glSwapBuffersPtr = NativeLibrary.GetExport(eglLib, "eglSwapBuffers");
        Dobby.Hook(glSwapBuffersPtr, typeof(ImGuiRender).GetMethod(nameof(OnSwapBuffers))!.MethodHandle.GetFunctionPointer(), out var prevSwapBuffers);
        _prevSwapBuffersDelegate = Marshal.GetDelegateForFunctionPointer<SwapBuffersDelegate>(prevSwapBuffers);
        
        string consumerSymbol = "_ZN7android13InputConsumer21initializeMotionEventEPNS_11MotionEventEPKNS_12InputMessageE";
        IntPtr consumerAddr = Dobby.SymbolResolver("libinput.so", consumerSymbol);
        Dobby.Hook(consumerAddr, typeof(ImGuiRender).GetMethod(nameof(OnTouchEvent))!.MethodHandle.GetFunctionPointer(),
            out var origin);
        _initializeMotionEvent = Marshal.GetDelegateForFunctionPointer<InitializeMotionEventDelegate>(origin);
        
        // Hook 按键事件
        string keySymbol = "_ZN7android13InputConsumer18initializeKeyEventEPNS_8KeyEventEPKNS_12InputMessageE";
        IntPtr keyAddr = Dobby.SymbolResolver("libinput.so", keySymbol);
        Dobby.Hook(keyAddr, typeof(ImGuiRender).GetMethod(nameof(OnKeyEvent))!.MethodHandle.GetFunctionPointer(),
            out var keyOrigin);
        _initializeKeyEvent = Marshal.GetDelegateForFunctionPointer<InitializeKeyEventDelegate>(keyOrigin);
        
        AndroidLog.Error(nameof(ImGuiRender), $"eglSwapBuffers hooked at 0x{glSwapBuffersPtr:X}");
        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnTouchEvent(IntPtr self, IntPtr motionEvent, IntPtr message)
    {
        // 先调用原函数初始化 MotionEvent，再传给 ImGui
        int result = _initializeMotionEvent(self, motionEvent, message);
        ImGuiImplAndroid.HandleInputEvent(self);
        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnKeyEvent(IntPtr self, IntPtr keyEvent, IntPtr message)
    {
        int result = _initializeKeyEvent(self, keyEvent, message);
        // IME 活跃时跳过：字符由 TextWatcher → C# AddInputCharacter 统一处理
        if (!_wantTextInputLast)
            ImGuiImplAndroid.HandleInputEvent(self);
        return result;
    }
    
    private static void InitImGui(IntPtr display, IntPtr surface)
    {
        if (_initialized) return;
        
        AndroidLog.Error(nameof(ImGuiRender), "Initializing ImGui with official backends...");
        
        // 从 EGL surface 获取 ANativeWindow
        if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width) ||
            !Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
        {
            AndroidLog.Error(nameof(ImGuiRender), "Failed to query EGL surface for initialization");
            return;
        }
        
        AndroidLog.Error(nameof(ImGuiRender), $"Surface size: {width}x{height}");
        
        // 创建 ImGui 上下文
        ImGuiNET.ImGui.CreateContext();
        
        var io = ImGuiNET.ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        
        // 设置缩放
        io.FontGlobalScale = 3.0f;
        
        // 加载中文字体作为默认字体（含 ASCII + CJK）
        string[] cjkPaths = [
            "/system/fonts/NotoSansSC-Regular.otf",
            "/system/fonts/DroidSansFallback.ttf",
            "/system/fonts/NotoSansCJK-Regular.ttc",
        ];
        bool cjkLoaded = false;
        foreach (var path in cjkPaths)
        {
            if (System.IO.File.Exists(path))
            {
                var cjk = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();
                io.Fonts.AddFontFromFileTTF(path, 16.0f, null, cjk);
                AndroidLog.Info(nameof(ImGuiRender), $"CJK font: {path}");
                cjkLoaded = true;
                break;
            }
        }
        if (!cjkLoaded)
            io.Fonts.AddFontDefault();  // 兜底
        
        // 设置样式
        var style = ImGuiNET.ImGui.GetStyle();
        style.ScaleAllSizes(2.0f);
        ImGuiNET.ImGui.StyleColorsClassic();
        
        // 初始化官方 backends
        // 使用 C# 实现获取 ANativeWindow* 初始化 Android backend
        var nativeWindow = Unity.UnitySurfaceHelper.GetUnityNativeWindow();
        if (nativeWindow != IntPtr.Zero)
        {
            ImGuiImplAndroid.Init(nativeWindow);
            AndroidLog.Error(nameof(ImGuiRender), $"ImGui_ImplAndroid_Init success with window: 0x{nativeWindow:X}");
        }
        else
        {
            AndroidLog.Error(nameof(ImGuiRender), "Failed to get Unity ANativeWindow, touch input may not work");
        }
        
        ImGuiImplOpenGL3.Init();
        
        // 创建输入提供者（使用 ImGui_ImplAndroid_HandleInputEvent）
        //_inputProvider = new MotionInputProvider();
        //_inputProvider.Start();
        
        AndroidLog.Error(nameof(ImGuiRender), "Touch input handled via ImGui_ImplAndroid");
        
        _initialized = true;
        AndroidLog.Error(nameof(ImGuiRender), "ImGui initialized with official OpenGL3 + Android input backends");
    }
    
    private static void BuildUI()
    {
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        
        if (ImGuiNET.ImGui.Begin("GLHooker - Official Backend"))
        {
            ImGuiNET.ImGui.TextColored(new Vector4(0.2f, 0.8f, 1.0f, 1.0f), "GLHooker v1.0.0");
            ImGuiNET.ImGui.Separator();
            
            ImGuiNET.ImGui.Text("Using Official ImGui Backends:");
            ImGuiNET.ImGui.BulletText("imgui_impl_opengl3");
            ImGuiNET.ImGui.BulletText("imgui_impl_android (Input)");
            ImGuiNET.ImGui.Spacing();
            
            // 显示 EGL 信息
            var display = EGL.GetCurrentDisplay();
            var surface = EGL.GetCurrentSurface(EGL.EGL_DRAW);
            
            if (EGL.QuerySurface(display, surface, EGL.EGL_WIDTH, out var w) &&
                EGL.QuerySurface(display, surface, EGL.EGL_HEIGHT, out var h))
            {
                ImGuiNET.ImGui.Text($"Surface: {w}x{h}");
            }
            
            ImGuiNET.ImGui.Spacing();
            
            if (ImGuiNET.ImGui.Button("Test Touch"))
            {
                AndroidLog.Error("ImGUI", "Touch button clicked!");
            }
            
            ImGuiNET.ImGui.SameLine();
            if (ImGuiNET.ImGui.Button("Set 60 FPS"))
            {
                SetTargetFrameRate(60);
            }

            ImGuiNET.ImGui.Spacing();
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("IME Test:");
            ImGuiNET.ImGui.InputText("##ime_input", ref _inputText, 256);
            ImGuiNET.ImGui.SameLine();
            ImGuiNET.ImGui.TextDisabled("(IME input test)");
            
            ImGuiNET.ImGui.Spacing();
            var ioFps = ImGuiNET.ImGui.GetIO();
            ImGuiNET.ImGui.Text($"FPS: {ioFps.Framerate:F1}");
            ImGuiNET.ImGui.Text($"Frame Time: {1000.0f / ioFps.Framerate:F2} ms");
        }
        ImGuiNET.ImGui.End();
    }

    // ===== IME 控制 (WantTextInput 驱动，一枪头模式) =====

    private static void ImeShow(string text)
    {
        var utils = new JavaClass("starray.android.modloader.ModManagerUtils");
        var jstr = JniHelperNative.NewString(text ?? "");
        var setTextMethod = utils.GetStaticMethodID("setInputText", "(Ljava/lang/String;)V");
        utils.CallStaticVoidMethod1(setTextMethod, jstr);
        JniHelperNative.DeleteLocalRef(jstr);
        var showMethod = utils.GetStaticMethodID("showSoftInput", "()V");
        utils.CallStaticVoidMethod0(showMethod);
        utils.Dispose();
        AndroidLog.Info(nameof(ImGuiRender), $"IME Show (text len={text?.Length ?? 0})");
    }

    private static void ImeHide()
    {
        int len = JniHelperNative.GetDataLength("ime_text");
        if (len > 0)
        {
            var ptr = JniHelperNative.GetDataBuffer("ime_text");
            var chars = new char[len];
            for (int i = 0; i < len; i++)
                chars[i] = (char)Marshal.ReadInt32(ptr, i * 4);
            _inputText = new string(chars);
        }
        else
        {
            _inputText = "";
        }
        var utils = new JavaClass("starray.android.modloader.ModManagerUtils");
        var hideMethod = utils.GetStaticMethodID("hideSoftInput", "()V");
        utils.CallStaticVoidMethod0(hideMethod);
        utils.Dispose();
        AndroidLog.Info(nameof(ImGuiRender), $"IME Hide (text=[{_inputText}])");
    }

    // ===== Unity API 调用 =====

    private static UnityResolve? _resolve;
    private static UnityResolve.Method? _vsyncMethod;
    private static UnityResolve.Method? _frameRateMethod;

    private static void SetTargetFrameRate(int fps)
    {
        try
        {
            if (_resolve == null)
            {
                _resolve = new UnityResolve();
                _resolve.InitIl2Cpp();
                
                var coreAsm = _resolve.GetAssembly("UnityEngine.CoreModule.dll");
                var qualityClass = coreAsm?.GetClass("UnityEngine", "QualitySettings");
                var appClass = coreAsm?.GetClass("UnityEngine", "Application");

                AndroidLog.Debug(nameof(ImGuiRender),$"class QualitySettings:{qualityClass.IsValid} Application:{appClass.IsValid}");
                _vsyncMethod = qualityClass?.GetMethod("set_vSyncCount");
                _frameRateMethod = appClass?.GetMethod("set_targetFrameRate");
                _resolve.DumpToFile("/sdcard/ModLoader");

                AndroidLog.Error(nameof(ImGuiRender),
                    $"vSyncCount: {(_vsyncMethod != null ? "OK" : "MISS")}, " +
                    $"targetFrameRate: {(_frameRateMethod != null ? "OK" : "MISS")}");
            }

            // 方式1: QualitySettings.vSyncCount = 0
            if (_vsyncMethod != null)
            {
                _vsyncMethod.InvokeStatic([0]);
                AndroidLog.Error(nameof(ImGuiRender), "QualitySettings.vSyncCount = 0");
            }

            // 方式2: Application.targetFrameRate = fps
            if (_frameRateMethod != null)
            {
                _frameRateMethod.InvokeStatic([fps]);
                AndroidLog.Error(nameof(ImGuiRender), $"Application.targetFrameRate = {fps}");
            }

            if (_vsyncMethod == null && _frameRateMethod == null)
                AndroidLog.Error(nameof(ImGuiRender), "No frame rate API found");
        }
        catch (Exception ex)
        {
            AndroidLog.Error(nameof(ImGuiRender), $"SetFPS error: {ex}");
        }
    }
}
