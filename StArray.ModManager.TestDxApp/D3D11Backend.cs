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

namespace StArray.ModManager.TestDxApp;

internal sealed unsafe class D3D11Backend : ITriangleBackend
{
    // ──────────────── 共享资源 ────────────────
    private IWindow _window = null!;
    private DXGI _dxgi = null!;
    private D3D11 _d3d11 = null!;
    private D3DCompiler _compiler = null!;

    private ComPtr<IDXGIFactory2> _factory;
    private ComPtr<IDXGISwapChain1> _swapchain;
    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _deviceContext;
    private ComPtr<ID3D11Buffer> _vertexBuffer;
    private ComPtr<ID3D11VertexShader> _vertexShader;
    private ComPtr<ID3D11PixelShader> _pixelShader;
    private ComPtr<ID3D11InputLayout> _inputLayout;

    private readonly float[] _backgroundColour = [0.1f, 0.1f, 0.1f, 1.0f];

    private static readonly float[] Vertices =
    [
        //    X      Y      Z     R     G     B     A
         0.0f,  0.5f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f,  // 顶部 — 红色
         0.5f, -0.5f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f,  // 右下 — 绿色
        -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f,  // 左下 — 蓝色
    ];

    private const uint VertexStride = 7 * sizeof(float);
    private const uint VertexOffset = 0;

    public void Load(IWindow window)
    {
        _window = window;

        // — 设置输入 —
        var input = window.CreateInput();
        foreach (var keyboard in input.Keyboards)
            keyboard.KeyDown += OnKeyDown;

        // — 获取 D3D API —
        _dxgi = DXGI.GetApi(window, forceDxvk: false);
        _d3d11 = D3D11.GetApi(window, forceDxvk: false);
        _compiler = D3DCompiler.GetApi();

        // — 创建 D3D11 设备 —
        SilkMarshal.ThrowHResult(
            _d3d11.CreateDevice(
                default(ComPtr<IDXGIAdapter>),
                D3DDriverType.Hardware,
                Software: default,
                (uint)0,
                null, 0,
                D3D11.SdkVersion,
                ref _device,
                null,
                ref _deviceContext));

        // — 创建交换链 —
        var swapChainDesc = new SwapChainDesc1
        {
            BufferCount = 2,
            Format = Format.FormatB8G8R8A8Unorm,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipDiscard,
            SampleDesc = new SampleDesc(1, 0),
        };

        _factory = _dxgi.CreateDXGIFactory<IDXGIFactory2>();

        SilkMarshal.ThrowHResult(
            _factory.CreateSwapChainForHwnd(
                _device,
                window.Native!.DXHandle!.Value,
                in swapChainDesc,
                null,
                ref Unsafe.NullRef<IDXGIOutput>(),
                ref _swapchain));

        // — 创建顶点缓冲 —
        var bufferDesc = new BufferDesc
        {
            ByteWidth = (uint)(Vertices.Length * sizeof(float)),
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.VertexBuffer,
        };

        fixed (float* vertexData = Vertices)
        {
            var subresourceData = new SubresourceData { PSysMem = vertexData };
            SilkMarshal.ThrowHResult(
                _device.CreateBuffer(in bufferDesc, in subresourceData, ref _vertexBuffer));
        }

        // — 编译着色器 —
        CompileShaders();
    }

    private void CompileShaders()
    {
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
        var shaderBytes = Encoding.ASCII.GetBytes(shaderSource);

        // — vs —
        ComPtr<ID3D10Blob> vertexCode = default;
        ComPtr<ID3D10Blob> vertexErrors = default;
        var hr = _compiler.Compile(
            in shaderBytes[0], (nuint)shaderBytes.Length,
            "shaderSource", null,
            ref Unsafe.NullRef<ID3DInclude>(),
            "vs_main", "vs_5_0", 0, 0,
            ref vertexCode, ref vertexErrors);

        if (hr.IsFailure)
        {
            if (vertexErrors.Handle is not null)
                Console.WriteLine(SilkMarshal.PtrToString((nint)vertexErrors.GetBufferPointer()));
            hr.Throw();
        }

        // — ps —
        ComPtr<ID3D10Blob> pixelCode = default;
        ComPtr<ID3D10Blob> pixelErrors = default;
        hr = _compiler.Compile(
            in shaderBytes[0], (nuint)shaderBytes.Length,
            "shaderSource", null,
            ref Unsafe.NullRef<ID3DInclude>(),
            "ps_main", "ps_5_0", 0, 0,
            ref pixelCode, ref pixelErrors);

        if (hr.IsFailure)
        {
            if (pixelErrors.Handle is not null)
                Console.WriteLine(SilkMarshal.PtrToString((nint)pixelErrors.GetBufferPointer()));
            hr.Throw();
        }

        // — 创建着色器对象 —
        SilkMarshal.ThrowHResult(
            _device.CreateVertexShader(
                vertexCode.GetBufferPointer(), vertexCode.GetBufferSize(),
                ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _vertexShader));

        SilkMarshal.ThrowHResult(
            _device.CreatePixelShader(
                pixelCode.GetBufferPointer(), pixelCode.GetBufferSize(),
                ref Unsafe.NullRef<ID3D11ClassLinkage>(), ref _pixelShader));

        // — Input Layout —
        fixed (byte* posSemantic = SilkMarshal.StringToMemory("POS"))
        fixed (byte* colSemantic = SilkMarshal.StringToMemory("COL"))
        {
            var inputElements = new InputElementDesc[]
            {
                new()
                {
                    SemanticName = posSemantic, SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0, AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0,
                },
                new()
                {
                    SemanticName = colSemantic, SemanticIndex = 0,
                    Format = Format.FormatR32G32B32A32Float,
                    InputSlot = 0, AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0,
                },
            };

            SilkMarshal.ThrowHResult(
                _device.CreateInputLayout(
                    in inputElements[0], 2,
                    vertexCode.GetBufferPointer(), vertexCode.GetBufferSize(),
                    ref _inputLayout));
        }

        vertexCode.Dispose();
        vertexErrors.Dispose();
        pixelCode.Dispose();
        pixelErrors.Dispose();
    }

    public void Render(double deltaSeconds)
    {
        // 获取当前帧缓冲
        using var framebuffer = _swapchain.GetBuffer<ID3D11Texture2D>(0);

        // 创建渲染目标视图
        ComPtr<ID3D11RenderTargetView> renderTargetView = default;
        SilkMarshal.ThrowHResult(
            _device.CreateRenderTargetView(framebuffer, null, ref renderTargetView));

        // 清空背景
        _deviceContext.ClearRenderTargetView(renderTargetView, ref _backgroundColour[0]);

        // 设置视口
        var viewport = new Viewport(0, 0, _window.FramebufferSize.X, _window.FramebufferSize.Y, 0, 1);
        _deviceContext.RSSetViewports(1, in viewport);

        // 设置渲染目标
        _deviceContext.OMSetRenderTargets(1, ref renderTargetView,
            ref Unsafe.NullRef<ID3D11DepthStencilView>());

        // 设置 IA
        _deviceContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        _deviceContext.IASetInputLayout(_inputLayout);
        _deviceContext.IASetVertexBuffers(0, 1, _vertexBuffer, in VertexStride, in VertexOffset);

        // 着色器
        _deviceContext.VSSetShader(_vertexShader,
            ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);
        _deviceContext.PSSetShader(_pixelShader,
            ref Unsafe.NullRef<ComPtr<ID3D11ClassInstance>>(), 0);

        // 绘制
        _deviceContext.Draw(3, 0);

        // 呈现
        _swapchain.Present(1, 0);

        renderTargetView.Dispose();
    }

    public void Resize(Vector2D<int> newSize)
    {
        SilkMarshal.ThrowHResult(
            _swapchain.ResizeBuffers(0, (uint)newSize.X, (uint)newSize.Y,
                Format.FormatB8G8R8A8Unorm, 0));
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (key == Key.Escape)
            _window.Close();
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _vertexShader.Dispose();
        _pixelShader.Dispose();
        _inputLayout.Dispose();
        _swapchain.Dispose();
        _deviceContext.Dispose();
        _device.Dispose();
        _factory.Dispose();
        _compiler.Dispose();
        _d3d11.Dispose();
        _dxgi.Dispose();
    }
}
