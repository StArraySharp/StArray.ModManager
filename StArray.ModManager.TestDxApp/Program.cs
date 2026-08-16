using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

#if NET481
using Silk.NET.Input.Glfw;
using Silk.NET.Windowing.Glfw;
#endif

// ──────────────── mono / net481：按绝对路径预加载原生 DLL ────────────────
// mono 下 LoadLibrary 的搜索路径基于 mono-sgen.exe 所在目录，而非工作目录，
// Silk.NET 的 GlfwPlatform.IsApplicable 探测会静默失败。这里用绝对路径预加载，
// 进程内模块缓存生效后，后续裸名加载即可命中。
PreloadNativeLibrary("glfw3.dll");
PreloadNativeLibrary("SDL2.dll");

[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
static extern IntPtr LoadLibraryW(string lpFileName);

static void PreloadNativeLibrary(string dll)
{
    var path = Path.Combine(AppContext.BaseDirectory, dll);
    if (File.Exists(path) && LoadLibraryW(path) != IntPtr.Zero)
        Console.WriteLine($"[preload] {dll} ok");
}

// ──────────────── 加载 Corehold 劫持的 winmm.dll 并调用 ────────────────
// Corehold 通过 winmm 代理注入：加载它自身的初始化逻辑（托管加载器 + .NET 10 运行时
// 自动下载）。调用标准导出 timeGetTime 触发其 DllMain/bootstrap。
var winmmPath = Path.Combine(AppContext.BaseDirectory, "winmm.dll");
if (File.Exists(winmmPath))
{
    var hWinmm = LoadLibraryW(winmmPath);
    if (hWinmm != IntPtr.Zero)
    {
        Console.WriteLine($"[winmm] loaded 0x{hWinmm:x}");
        var proc = GetProcAddress(hWinmm, "timeGetTime");
        if (proc != IntPtr.Zero)
        {
            var timeGetTime = Marshal.GetDelegateForFunctionPointer<TimeGetTimeDelegate>(proc);
            Console.WriteLine($"[winmm] timeGetTime() = {timeGetTime()}");
        }
        else
        {
            Console.WriteLine("[winmm] GetProcAddress(timeGetTime) failed");
        }
    }
    else
    {
        Console.WriteLine("[winmm] LoadLibrary failed");
    }
}
else
{
    Console.WriteLine($"[winmm] not found: {winmmPath}");
}

[DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

// net481 + mono 下 Silk.NET 的程序集探测（TryAdd 反射加载）不可靠，
// 直接无条件注册 GLFW 窗口平台（RegisterPlatform 幂等）。
#if NET481
GlfwWindowing.RegisterPlatform();
GlfwInput.RegisterPlatform();
#endif

// ──────────────── 背景颜色 ────────────────
var backgroundColour = new[] { 0.1f, 0.1f, 0.1f, 1.0f };

// ──────────────── 三角形顶点数据 (x, y, z, r, g, b, a) ────────────────
float[] vertices =
{
    //    X      Y      Z     R     G     B     A
         0.0f,  0.5f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f,  // 顶部 — 红色
         0.5f, -0.5f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f,  // 右下 — 绿色
        -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f,  // 左下 — 蓝色
};

var vertexStride = 7U * sizeof(float); // 3 (pos) + 4 (color)
var vertexOffset = 0U;

// ──────────────── HLSL 着色器源码 ────────────────
const string shaderSource = @"
struct vs_in {
    float3 pos : POS;
    float4 color : COL;
};

struct vs_out {
    float4 pos : SV_POSITION;
    float4 color : COL;
};

vs_out vs_main(vs_in input) {
    vs_out output = (vs_out)0;
    output.pos = float4(input.pos, 1.0);
    output.color = input.color;
    return output;
}

float4 ps_main(vs_out input) : SV_TARGET {
    return input.color;
}
";

// ──────────────── 创建窗口 ────────────────
var options = WindowOptions.Default;
options.Size = new Vector2D<int>(800, 600);
options.Title = "D3D11 Triangle - Silk.NET";
options.API = GraphicsAPI.None; // 关键：不使用 OpenGL
var window = Window.Create(options);

// ──────────────── 加载 D3D 库 ────────────────
DXGI dxgi = null!;
D3D11 d3d11 = null!;
D3DCompiler compiler = null!;

// ──────────────── D3D11 资源（在 OnLoad 中初始化） ────────────────
ComPtr<IDXGIFactory2> factory = default;
ComPtr<IDXGISwapChain1> swapchain = default;
ComPtr<ID3D11Device> device = default;
ComPtr<ID3D11DeviceContext> deviceContext = default;
ComPtr<ID3D11Buffer> vertexBuffer = default;
ComPtr<ID3D11VertexShader> vertexShader = default;
ComPtr<ID3D11PixelShader> pixelShader = default;
ComPtr<ID3D11InputLayout> inputLayout = default;

// ──────────────── 注册事件 ────────────────
window.Load += OnLoad;
window.Render += OnRender;
window.FramebufferResize += OnFramebufferResize;

// ──────────────── 运行窗口 ────────────────
window.Run();

// ──────────────── 清理资源 ────────────────
factory.Dispose();
swapchain.Dispose();
device.Dispose();
deviceContext.Dispose();
vertexBuffer.Dispose();
vertexShader.Dispose();
pixelShader.Dispose();
inputLayout.Dispose();
compiler.Dispose();
d3d11.Dispose();
dxgi.Dispose();
window.Dispose();

return;

// ════════════════════════════════════════════════════════════════
//  事件处理
// ════════════════════════════════════════════════════════════════

unsafe void OnLoad()
{
    // — 设置输入 —
    var input = window.CreateInput();
    foreach (var keyboard in input.Keyboards)
    {
        keyboard.KeyDown += OnKeyDown;
    }

    // — 获取 D3D API —
    dxgi = DXGI.GetApi(window, forceDxvk: false);
    d3d11 = D3D11.GetApi(window, forceDxvk: false);
    compiler = D3DCompiler.GetApi();

    // — 创建 D3D11 设备 —
    SilkMarshal.ThrowHResult(
        d3d11.CreateDevice(
            default(ComPtr<IDXGIAdapter>),
            D3DDriverType.Hardware,
            Software: default,
            (uint)0,
            null,
            0,
            D3D11.SdkVersion,
            ref device,
            null,
            ref deviceContext));

    // — 创建交换链 —
    var swapChainDesc = new SwapChainDesc1
    {
        BufferCount = 2,
        Format = Format.FormatB8G8R8A8Unorm,
        BufferUsage = DXGI.UsageRenderTargetOutput,
        SwapEffect = SwapEffect.FlipDiscard,
        SampleDesc = new SampleDesc(1, 0),
    };

    factory = dxgi.CreateDXGIFactory<IDXGIFactory2>();

    SilkMarshal.ThrowHResult(
        factory.CreateSwapChainForHwnd(
            device,
            window.Native!.DXHandle!.Value,
            in swapChainDesc,
            null,
            ref Unsafe.NullRef<IDXGIOutput>(),
            ref swapchain));

    // — 创建顶点缓冲 —
    var bufferDesc = new BufferDesc
    {
        ByteWidth = (uint)(vertices.Length * sizeof(float)),
        Usage = Usage.Default,
        BindFlags = (uint)BindFlag.VertexBuffer,
    };

    fixed (float* vertexData = vertices)
    {
        var subresourceData = new SubresourceData { PSysMem = vertexData };
        SilkMarshal.ThrowHResult(
            device.CreateBuffer(in bufferDesc, in subresourceData, ref vertexBuffer));
    }

    // — 编译着色器 —
    var shaderBytes = Encoding.ASCII.GetBytes(shaderSource);

    ComPtr<ID3D10Blob> vertexCode = default;
    ComPtr<ID3D10Blob> vertexErrors = default;
    HResult hr = compiler.Compile(
        in shaderBytes[0],
        (nuint)shaderBytes.Length,
        nameof(shaderSource),
        null,
        ref Unsafe.NullRef<ID3DInclude>(),
        "vs_main", "vs_5_0",
        0, 0,
        ref vertexCode,
        ref vertexErrors);

    if (hr.IsFailure)
    {
        if (vertexErrors.Handle is not null)
            Console.WriteLine(SilkMarshal.PtrToString((nint)vertexErrors.GetBufferPointer()));
        hr.Throw();
    }

    ComPtr<ID3D10Blob> pixelCode = default;
    ComPtr<ID3D10Blob> pixelErrors = default;
    hr = compiler.Compile(
        in shaderBytes[0],
        (nuint)shaderBytes.Length,
        nameof(shaderSource),
        null,
        ref Unsafe.NullRef<ID3DInclude>(),
        "ps_main", "ps_5_0",
        0, 0,
        ref pixelCode,
        ref pixelErrors);

    if (hr.IsFailure)
    {
        if (pixelErrors.Handle is not null)
            Console.WriteLine(SilkMarshal.PtrToString((nint)pixelErrors.GetBufferPointer()));
        hr.Throw();
    }

    // — 创建着色器对象 —
    SilkMarshal.ThrowHResult(
        device.CreateVertexShader(
            vertexCode.GetBufferPointer(),
            vertexCode.GetBufferSize(),
            ref Unsafe.NullRef<ID3D11ClassLinkage>(),
            ref vertexShader));

    SilkMarshal.ThrowHResult(
        device.CreatePixelShader(
            pixelCode.GetBufferPointer(),
            pixelCode.GetBufferSize(),
            ref Unsafe.NullRef<ID3D11ClassLinkage>(),
            ref pixelShader));

    // — Input Layout —
    fixed (byte* posSemantic = SilkMarshal.StringToMemory("POS"))
    fixed (byte* colSemantic = SilkMarshal.StringToMemory("COL"))
    {
        var inputElements = new InputElementDesc[]
        {
            new()
            {
                SemanticName = posSemantic,
                SemanticIndex = 0,
                Format = Format.FormatR32G32B32Float,
                InputSlot = 0,
                AlignedByteOffset = 0,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
            new()
            {
                SemanticName = colSemantic,
                SemanticIndex = 0,
                Format = Format.FormatR32G32B32A32Float,
                InputSlot = 0,
                AlignedByteOffset = 12,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
        };

        SilkMarshal.ThrowHResult(
            device.CreateInputLayout(
                in inputElements[0], 2,
                vertexCode.GetBufferPointer(),
                vertexCode.GetBufferSize(),
                ref inputLayout));
    }

    // — 清理编译中间产物 —
    vertexCode.Dispose();
    vertexErrors.Dispose();
    pixelCode.Dispose();
    pixelErrors.Dispose();
}

unsafe void OnRender(double deltaSeconds)
{
    // 获取当前帧缓冲
    using var framebuffer = swapchain.GetBuffer<ID3D11Texture2D>(0);

    // 创建渲染目标视图
    ComPtr<ID3D11RenderTargetView> renderTargetView = default;
    SilkMarshal.ThrowHResult(
        device.CreateRenderTargetView(framebuffer, null, ref renderTargetView));

    // 清空背景
    deviceContext.ClearRenderTargetView(renderTargetView, ref backgroundColour[0]);

    // 设置视口
    var viewport = new Viewport(0, 0, window.FramebufferSize.X, window.FramebufferSize.Y, 0, 1);
    deviceContext.RSSetViewports(1, in viewport);

    // 设置渲染目标
    deviceContext.OMSetRenderTargets(1, ref renderTargetView,
        ref Unsafe.NullRef<ID3D11DepthStencilView>());

    // 设置 IA 阶段
    deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
    deviceContext.IASetInputLayout(inputLayout);
    deviceContext.IASetVertexBuffers(0, 1, vertexBuffer, in vertexStride, in vertexOffset);

    // 设置着色器
    deviceContext.VSSetShader(vertexShader,
        ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
    deviceContext.PSSetShader(pixelShader,
        ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

    // 绘制三角形（3 个顶点）
    deviceContext.Draw(3, 0);

    // 呈现
    swapchain.Present(1, 0);

    // 释放本帧临时资源
    renderTargetView.Dispose();
}

void OnFramebufferResize(Vector2D<int> newSize)
{
    SilkMarshal.ThrowHResult(
        swapchain.ResizeBuffers(0, (uint)newSize.X, (uint)newSize.Y,
            Format.FormatB8G8R8A8Unorm, 0));
}

void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
{
    if (key == Key.Escape)
    {
        window.Close();
    }
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
delegate uint TimeGetTimeDelegate();
