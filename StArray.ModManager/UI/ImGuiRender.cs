using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.Egl;
using OpenTK.Graphics.ES30;
using StArray.ModManager.Java;
using StArray.ModManager.Manager;
using StArray.ModManager.PInvoke;
using StArray.ModManager.Unity;

namespace StArray.ModManager.UI;

/// <summary>
/// ImGui EGL 渲染器（骨架——输入实现待重写）
/// </summary>
public static unsafe class ImGuiRender
{
    private static bool _initialized;

    private static SwapBuffersDelegate? _prevSwapBuffersDelegate;
    delegate int SwapBuffersDelegate(IntPtr display, IntPtr surface);

    private static InitializeMotionEventDelegate _initializeMotionEvent;
    private static InitializeKeyEventDelegate _initializeKeyEvent;
    delegate int InitializeMotionEventDelegate(IntPtr self, IntPtr motionEvent, IntPtr message);
    delegate int InitializeKeyEventDelegate(IntPtr self, IntPtr keyEvent, IntPtr message);

    public static event Action OnRender = () => { };
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnSwapBuffers(IntPtr display, IntPtr surface)
    {
        try
        {
            if (!Egl.QuerySurface(display, surface, Egl.WIDTH, out var width) ||
                !Egl.QuerySurface(display, surface, Egl.HEIGHT, out var height))
                return _prevSwapBuffersDelegate!(display, surface);

            if (width <= 0 || height <= 0)
                return _prevSwapBuffersDelegate!(display, surface);

            if (!_initialized)
                InitImGui(display, surface);

            // TODO: input processing here

            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplAndroid.NewFrame();

            var io = ImGuiNET.ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);

            ImGuiNET.ImGui.NewFrame();
            BuildUI();
            ImGuiNET.ImGui.Render();
            ImGuiImplOpenGL3.RenderDrawData((IntPtr)ImGuiNET.ImGui.GetDrawData().NativePtr);

            // ImGui 渲染后应用 GL 状态（此时不会被覆盖）
            ApplyGLState();
        }
        catch (Exception ex)
        {
            AndroidLog.Error(nameof(ImGuiRender), $"OnSwapBuffers error: {ex}");
        }
        return _prevSwapBuffersDelegate!(display, surface);
    }

    public static bool Install()
    {
        var eglLib = DL.dlopen("libEgl.so", DL.Flags.RTLD_GLOBAL);
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
        ImGuiImplAndroid.HandleInputEvent(self);
        return result;
    }
    
    private static void InitImGui(IntPtr display, IntPtr surface)
    {
        if (_initialized) return;
        GL.LoadBindings(new GLESBindingsContext());
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
        ImGuiNET.ImGui.StyleColorsDark();

        // 编译彩虹着色器
        InitRainbowShader();

        // 初始化官方 backends
        // 使用 C# 实现获取 ANativeWindow* 初始化 Android backend
        var nativeWindow = Unity.UnitySurfaceHelper.GetUnityNativeWindow();
        if (nativeWindow != IntPtr.Zero)
        {
            ImGuiImplAndroid.Init(nativeWindow);
            AndroidLog.Error(nameof(ImGuiRender),
                $"ImGui_ImplAndroid_Init success: 0x{nativeWindow:X}");
        }

        ImGuiImplOpenGL3.Init();

        _initialized = true;
        AndroidLog.Error(nameof(ImGuiRender), "ImGui initialized");
    }

    // ===== GL 功能调试面板 =====

    // basic toggles
    private static bool _cbDepth, _cbStencil, _cbBlend, _cbCull, _cbScissor, _cbDither;
    private static bool _cbMultisample, _cbPolygonOffset;
    // advanced
    private static bool _cbWireframe, _cbColorWrite, _cbAlphaTest;
    // parameters
    private static int _blendSrc = 0, _blendDst = 0;
    private static int _depthFunc;
    private static int _cullMode;
    private static int _stencilFunc, _stencilOp;
    private static float _lineWidth = 1f, _pointSize = 1f;
    private static float _polyOffsetFactor = 1f, _polyOffsetUnits = 1f;
    private static System.Numerics.Vector4 _clearColor = new(0.45f, 0.55f, 0.60f, 1.0f);
    private static bool _cbR, _cbG, _cbB, _cbA;
    private static int _selectedTest;

    // GL info (refreshed each frame)
    private static string _glVendor = "", _glRenderer = "", _glVersion = "", _glExt = "";
    private static int _glError, _viewportX, _viewportY, _viewportW, _viewportH;

    // rainbow shader
    private static int _rainbowProg;
    private static int _rainbowBgIdx;  // uniform: bg texture slot
    private static bool _rainbowReady;
    private static bool _rainbowActive;
    private static IntPtr _rbBindPtr, _rbUnbindPtr;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void DrawCb(IntPtr parentList, IntPtr cmd);

    // blend func names
    static readonly string[] BlendNames = [
        "GL_ZERO", "GL_ONE", "GL_SRC_COLOR", "GL_ONE_MINUS_SRC_COLOR",
        "GL_DST_COLOR", "GL_ONE_MINUS_DST_COLOR", "GL_SRC_ALPHA", "GL_ONE_MINUS_SRC_ALPHA",
        "GL_DST_ALPHA", "GL_ONE_MINUS_DST_ALPHA", "GL_SRC_ALPHA_SATURATE"
    ];
    static readonly int[] BlendValues = [0, 1, 0x0300, 0x0301, 0x0306, 0x0307, 0x0302, 0x0303, 0x0304, 0x0305, 0x0308];

    static readonly string[] DepthFuncNames = ["GL_NEVER", "GL_LESS", "GL_EQUAL", "GL_LEQUAL", "GL_GREATER", "GL_NOTEQUAL", "GL_GEQUAL", "GL_ALWAYS"];
    static readonly int[] DepthFuncValues = [0x0200, 0x0201, 0x0202, 0x0203, 0x0204, 0x0205, 0x0206, 0x0207];

    static readonly string[] CullNames = ["GL_BACK", "GL_FRONT", "GL_FRONT_AND_BACK"];
    static readonly int[] CullValues = [0x0405, 0x0404, 0x0408];

    static readonly string[] StencilFuncNames = ["GL_NEVER", "GL_LESS", "GL_LEQUAL", "GL_GREATER", "GL_GEQUAL", "GL_EQUAL", "GL_NOTEQUAL", "GL_ALWAYS"];

    private static void BuildUI()
    {
        OnRender?.Invoke();

        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(520, 700), ImGuiCond.FirstUseEver);

        if (ImGuiNET.ImGui.Begin("GL Debug Panel"))
        {
            if (ImGuiNET.ImGui.BeginTabBar("GLTabs"))
            {
                DrawGLCapsTab();
                DrawGLParamsTab();
                DrawGLInfoTab();
                DrawShaderTab();
                ImGuiNET.ImGui.EndTabBar();
            }
        }
        ImGuiNET.ImGui.End();
    }

    static void DrawGLCapsTab()
    {
        if (!ImGuiNET.ImGui.BeginTabItem("Caps")) return;

        ImGuiNET.ImGui.TextColored(new Vector4(0.2f, 1f, 0.4f, 1f), "GL Capability Toggles");
        ImGuiNET.ImGui.Separator();

        ImGuiNET.ImGui.Checkbox("GL_DEPTH_TEST",     ref _cbDepth);
        ImGuiNET.ImGui.Checkbox("GL_STENCIL_TEST",   ref _cbStencil);
        ImGuiNET.ImGui.Checkbox("GL_BLEND",          ref _cbBlend);
        ImGuiNET.ImGui.Checkbox("GL_CULL_FACE",      ref _cbCull);
        ImGuiNET.ImGui.Checkbox("GL_SCISSOR_TEST",   ref _cbScissor);
        ImGuiNET.ImGui.Checkbox("GL_DITHER",         ref _cbDither);
        ImGuiNET.ImGui.Checkbox("GL_MULTISAMPLE",    ref _cbMultisample);
        ImGuiNET.ImGui.Checkbox("GL_POLYGON_OFFSET", ref _cbPolygonOffset);

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();
        ImGuiNET.ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.2f, 1f), "Advanced");

        ImGuiNET.ImGui.Checkbox("Color Write Mask",  ref _cbColorWrite);
        if (_cbColorWrite)
        {
            ImGuiNET.ImGui.Indent();
            ImGuiNET.ImGui.Checkbox("R", ref _cbR); ImGuiNET.ImGui.SameLine();
            ImGuiNET.ImGui.Checkbox("G", ref _cbG); ImGuiNET.ImGui.SameLine();
            ImGuiNET.ImGui.Checkbox("B", ref _cbB); ImGuiNET.ImGui.SameLine();
            ImGuiNET.ImGui.Checkbox("A", ref _cbA);
            ImGuiNET.ImGui.Unindent();
        }
        ImGuiNET.ImGui.Checkbox("Alpha Test",        ref _cbAlphaTest);
        ImGuiNET.ImGui.Checkbox("Wireframe",         ref _cbWireframe);

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        // visual feedback
        ImGuiNET.ImGui.Text("Clear Color:");
        ImGuiNET.ImGui.ColorEdit4("##clear", ref _clearColor);

        int active = 0;
        if (_cbDepth) active++; if (_cbStencil) active++; if (_cbBlend) active++;
        if (_cbCull) active++; if (_cbScissor) active++; if (_cbDither) active++;
        if (_cbMultisample) active++; if (_cbPolygonOffset) active++;
        ImGuiNET.ImGui.Text($"Active: {active}/8");

        ImGuiNET.ImGui.EndTabItem();
    }

    static void DrawGLParamsTab()
    {
        if (!ImGuiNET.ImGui.BeginTabItem("Params")) return;

        ImGuiNET.ImGui.TextColored(new Vector4(0.2f, 1f, 0.4f, 1f), "GL Parameters");
        ImGuiNET.ImGui.Separator();

        // Blend Func
        ImGuiNET.ImGui.Text("Blend Func:");
        ImGuiNET.ImGui.Combo("Src", ref _blendSrc, BlendNames, BlendNames.Length);
        ImGuiNET.ImGui.Combo("Dst", ref _blendDst, BlendNames, BlendNames.Length);

        ImGuiNET.ImGui.Spacing();
        // Depth Func
        ImGuiNET.ImGui.Text("Depth Func:");
        ImGuiNET.ImGui.Combo("##depth", ref _depthFunc, DepthFuncNames, DepthFuncNames.Length);

        ImGuiNET.ImGui.Spacing();
        // Cull Face Mode
        ImGuiNET.ImGui.Text("Cull Face:");
        ImGuiNET.ImGui.Combo("##cull", ref _cullMode, CullNames, CullNames.Length);

        ImGuiNET.ImGui.Spacing();
        // Stencil
        ImGuiNET.ImGui.Text("Stencil Func:");
        ImGuiNET.ImGui.Combo("##sf", ref _stencilFunc, StencilFuncNames, StencilFuncNames.Length);

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        // Line width / point size
        ImGuiNET.ImGui.SliderFloat("Line Width", ref _lineWidth, 0.5f, 10f);
        ImGuiNET.ImGui.SliderFloat("Point Size", ref _pointSize, 1f, 64f);

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        // Polygon Offset
        ImGuiNET.ImGui.Text("Polygon Offset:");
        ImGuiNET.ImGui.SliderFloat("Factor", ref _polyOffsetFactor, -10f, 10f);
        ImGuiNET.ImGui.SliderFloat("Units",  ref _polyOffsetUnits,  -10f, 10f);

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        // Color Write Mask
        ImGuiNET.ImGui.Text("Color Write Mask:");
        ImGuiNET.ImGui.Checkbox("R", ref _cbR); ImGuiNET.ImGui.SameLine();
        ImGuiNET.ImGui.Checkbox("G", ref _cbG); ImGuiNET.ImGui.SameLine();
        ImGuiNET.ImGui.Checkbox("B", ref _cbB); ImGuiNET.ImGui.SameLine();
        ImGuiNET.ImGui.Checkbox("A", ref _cbA);

        ImGuiNET.ImGui.EndTabItem();
    }

    static void DrawGLInfoTab()
    {
        if (!ImGuiNET.ImGui.BeginTabItem("Info")) return;

        ImGuiNET.ImGui.TextColored(new Vector4(0.2f, 1f, 0.4f, 1f), "GL Context Info");
        ImGuiNET.ImGui.Separator();

        ImGuiNET.ImGui.Text($"Vendor:   {_glVendor}");
        ImGuiNET.ImGui.Text($"Renderer: {_glRenderer}");
        ImGuiNET.ImGui.Text($"Version:  {_glVersion}");

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        ImGuiNET.ImGui.Text($"Viewport: {_viewportX}, {_viewportY}, {_viewportW}x{_viewportH}");

        var io = ImGuiNET.ImGui.GetIO();
        ImGuiNET.ImGui.Text($"Display:  {io.DisplaySize.X:F0}x{io.DisplaySize.Y:F0}");
        ImGuiNET.ImGui.Text($"FB Scale: {io.DisplayFramebufferScale.X:F2}x{io.DisplayFramebufferScale.Y:F2}");

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        ImGuiNET.ImGui.Text($"GL Error: 0x{_glError:X}");
        ImGuiNET.ImGui.Text($"FPS: {io.Framerate:F1}  ({1000f/io.Framerate:F2}ms)");
        ImGuiNET.ImGui.Text($"Delta Time: {io.DeltaTime:F4}");
        ImGuiNET.ImGui.Text($"WantCapture: M={io.WantCaptureMouse} K={io.WantCaptureKeyboard} T={io.WantTextInput}");

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();

        // UI Scale
        ImGuiNET.ImGui.Text("UI Scale:");
        float scale = io.FontGlobalScale;
        if (ImGuiNET.ImGui.SliderFloat("##scale", ref scale, 1f, 5f))
            io.FontGlobalScale = scale;

        var style = ImGuiNET.ImGui.GetStyle();
        ImGuiNET.ImGui.Spacing();
        // Slider width (affects scrollbar + slider grab)
        ImGuiNET.ImGui.Text("Scrollbar Width:");
        float grab = style.GrabMinSize;
        if (ImGuiNET.ImGui.SliderFloat("##grab", ref grab, 5f, 60f))
            style.GrabMinSize = grab;

        float scrollW = style.ScrollbarSize;
        if (ImGuiNET.ImGui.SliderFloat("Scrollbar Size", ref scrollW, 10f, 60f))
            style.ScrollbarSize = scrollW;

        ImGuiNET.ImGui.Separator();
        ImGuiNET.ImGui.Text("GL State Queries:");
        ImGuiNET.ImGui.Text($"  DepthTest:  {GL.IsEnabled(EnableCap.DepthTest)}");
        ImGuiNET.ImGui.Text($"  StencilTest:{GL.IsEnabled(EnableCap.StencilTest)}");
        ImGuiNET.ImGui.Text($"  Blend:      {GL.IsEnabled(EnableCap.Blend)}");
        ImGuiNET.ImGui.Text($"  CullFace:   {GL.IsEnabled(EnableCap.CullFace)}");
        ImGuiNET.ImGui.Text($"  ScissorTest:{GL.IsEnabled(EnableCap.ScissorTest)}");
        ImGuiNET.ImGui.Text($"  Dither:     {GL.IsEnabled(EnableCap.Dither)}");

        ImGuiNET.ImGui.EndTabItem();
    }

    static void DrawShaderTab()
    {
        if (!ImGuiNET.ImGui.BeginTabItem("Shader")) return;

        ImGuiNET.ImGui.TextColored(new Vector4(1f, 0.5f, 0f, 1f), "Rainbow Text Shader");
        ImGuiNET.ImGui.Separator();

        if (!_rainbowReady)
        {
            ImGuiNET.ImGui.TextColored(new Vector4(1, 0, 0, 1), "Shader not compiled!");
        }
        else
        {
            var dl = ImGuiNET.ImGui.GetWindowDrawList();
            var pos = ImGuiNET.ImGui.GetCursorScreenPos();

            // 1. bind rainbow shader → 后续 AddText 用七彩渲染
            dl.AddCallback(_rbBindPtr, IntPtr.Zero);

            // 2. draw text (白色 + 彩虹着色器 = 七彩文字)
            dl.AddText(pos + new Vector2(0, 0),  0xFFFFFFFF, "Hello Rainbow World!");
            dl.AddText(pos + new Vector2(0, 35), 0xFFFFFFFF, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            dl.AddText(pos + new Vector2(0, 70), 0xFFFFFFFF, "あいうえお かきくけこ");

            // 3. unbind → 恢复 ImGui 默认着色器
            dl.AddCallback(_rbUnbindPtr, IntPtr.Zero);

            ImGuiNET.ImGui.Dummy(new Vector2(400, 105));
        }

        ImGuiNET.ImGui.Spacing();
        ImGuiNET.ImGui.Separator();
        ImGuiNET.ImGui.Text($"Program: {_rainbowProg}");
        ImGuiNET.ImGui.Checkbox("Rainbow active", ref _rainbowActive);
        ImGuiNET.ImGui.EndTabItem();
    }

    /// <summary> 每帧应用 GL 状态 + 刷新上下文信息 </summary>
    private static void ApplyGLState()
    {
        // === Caps ===
        Toggle(EnableCap.DepthTest,      _cbDepth);
        Toggle(EnableCap.StencilTest,    _cbStencil);
        Toggle(EnableCap.Blend,          _cbBlend);
        Toggle(EnableCap.CullFace,       _cbCull);
        Toggle(EnableCap.ScissorTest,    _cbScissor);
        Toggle(EnableCap.Dither,         _cbDither);
        ToggleRaw((EnableCap)0x809D, _cbMultisample);     // GL_MULTISAMPLE = 0x809D
        ToggleRaw((EnableCap)0x8037, _cbPolygonOffset);   // GL_POLYGON_OFFSET_FILL = 0x8037

        // === Params ===
        if (_cbBlend)
            GL.BlendFunc((BlendingFactorSrc)BlendValues[_blendSrc],
                         (BlendingFactorDest)BlendValues[_blendDst]);

        GL.DepthFunc((DepthFunction)DepthFuncValues[_depthFunc]);
        GL.CullFace((TriangleFace)CullValues[_cullMode]);

        if (_cbPolygonOffset)
            GL.PolygonOffset(_polyOffsetFactor, _polyOffsetUnits);

        GL.LineWidth(_lineWidth);
        // GL.PointSize not in ES 3.0 core — use only if available

        // Color write mask
        if (_cbColorWrite)
            GL.ColorMask(_cbR, _cbG, _cbB, _cbA);
        else
            GL.ColorMask(true, true, true, true);

        // === Visual feedback: 右下角清色块 ===
        if (_cbBlend || _cbColorWrite)
        {
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(800, 100, 100, 100);
            GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.ScissorTest);
        }

        // === Refresh GL Info (only once per frame, cached) ===
        if (_glVendor.Length == 0)
        {
            _glVendor   = GL.GetString(StringName.Vendor)   ?? "n/a";
            _glRenderer = GL.GetString(StringName.Renderer) ?? "n/a";
            _glVersion  = GL.GetString(StringName.Version)  ?? "n/a";
        }

        // Viewport & Error (every frame)
        var vp = new int[4];
        GL.GetInteger(GetPName.Viewport, vp);
        _viewportX = vp[0]; _viewportY = vp[1]; _viewportW = vp[2]; _viewportH = vp[3];

        _glError = (int)GL.GetError();
    }

    private static void Toggle(EnableCap cap, bool on)
    {
        if (on) GL.Enable(cap); else GL.Disable(cap);
    }

    private static void ToggleRaw(EnableCap cap, bool on)
    {
        if (on) GL.Enable(cap); else GL.Disable(cap);
    }

    // ===== 彩虹渐变着色器 =====

    static void InitRainbowShader()
    {
        const string vs = @"#version 300 es
uniform mat4 ProjMtx;
in vec2 Position;
in vec2 UV;
in vec4 Color;
out vec2 Frag_UV;
out vec4 Frag_Color;
void main() {
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
}";

        const string fs = @"#version 300 es
precision mediump float;
in vec2 Frag_UV;
in vec4 Frag_Color;
out vec4 outColor;
uniform sampler2D Texture;
vec3 hsl2rgb(float h,float s,float l){
    vec3 r=abs(mod(h*6.0+vec3(0,4,2),6.0)-3.0)-1.0;
    return l+s*(clamp(r,0.0,1.0)-0.5)*(1.0-abs(2.0*l-1.0));
}
void main(){
    float glyph=texture(Texture,Frag_UV).a;
    float hue=Frag_UV.x*0.7+gl_FragCoord.y/800.0*0.3;
    vec3 rainbow=hsl2rgb(fract(hue),0.85,0.55);
    outColor=vec4(rainbow*Frag_Color.rgb,glyph*Frag_Color.a);
}";

        int vsId = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vsId, vs);
        GL.CompileShader(vsId);
        if (!CheckCompile(vsId, "VS")) return;

        int fsId = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fsId, fs);
        GL.CompileShader(fsId);
        if (!CheckCompile(fsId, "FS")) return;

        _rainbowProg = GL.CreateProgram();
        GL.AttachShader(_rainbowProg, vsId);
        GL.AttachShader(_rainbowProg, fsId);
        GL.LinkProgram(_rainbowProg);
        if (!CheckLink(_rainbowProg)) return;

        GL.DeleteShader(vsId);
        GL.DeleteShader(fsId);

        _rainbowReady = true;

        // 创建 bind/unbind 回调（持有 delegate 引用防止 GC）
        DrawCb cbBind = (_, _) => { GL.UseProgram(_rainbowProg); _rainbowActive = true; };
        DrawCb cbUnbind = (_, _) => { GL.UseProgram(0); _rainbowActive = false; };
        _rbBindPtr = Marshal.GetFunctionPointerForDelegate(cbBind);
        _rbUnbindPtr = Marshal.GetFunctionPointerForDelegate(cbUnbind);

        // keep delegates alive
        GC.KeepAlive(cbBind);
        GC.KeepAlive(cbUnbind);

        AndroidLog.Info(nameof(ImGuiRender), "Rainbow shader compiled");
    }

    static bool CheckCompile(int shader, string tag)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            AndroidLog.Error(nameof(ImGuiRender), $"Shader {tag}: {log}");
            return false;
        }
        return true;
    }

    static bool CheckLink(int prog)
    {
        GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetProgramInfoLog(prog);
            AndroidLog.Error(nameof(ImGuiRender), $"Link: {log}");
            GL.DeleteProgram(prog);
            return false;
        }
        return true;
    }
}