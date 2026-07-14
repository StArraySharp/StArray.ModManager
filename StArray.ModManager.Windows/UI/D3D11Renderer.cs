using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using StArray.ModManager.UI;
using StArray.ModManager.Windows.Native;

namespace StArray.ModManager.Windows.UI;

public sealed unsafe class D3D11Renderer : IImGuiRenderer
{
    static D3D11Renderer? s_i;
    public static D3D11Renderer Instance => s_i ?? throw new("Not installed");
    public static bool Install() => (s_i = new D3D11Renderer()).InstallInstance();

    static Action? s_pending;
    public static event Action OnRender
    {
        add { if (s_i != null) s_i._onRender += value; else s_pending += value; }
        remove { if (s_i != null) s_i._onRender -= value; else s_pending -= value; }
    }
    Action _onRender = () => { };
    bool _ok;
    public bool IsInitialized => _ok;
    event Action IImGuiRenderer.OnRender { add => _onRender += value; remove => _onRender -= value; }
    bool IImGuiRenderer.Install() => InstallInstance();

    // ── 函数指针帮助 ──
    static void** Vtbl(nint p) => *(void***)p;

    // ── 委托 ──
    delegate int PresentDel(nint sc, int sync, int flags);
    delegate int ResizeDel(nint sc, int cnt, int w, int h, int fmt, int flags);
    PresentDel? _origPresent;
    ResizeDel? _origResize;

    nint _dev, _ctx;
    bool _inited;
    nint _rtv, _hwnd, _origWnd;

    bool InstallInstance()
    {
        try
        {
            HookHelper.Instance ??= new MinHook();

            var sc = CreateDummy();
            if (sc == 0) return Fail("dummy failed");
            var v = Vtbl(sc);
            nint pp = (nint)v[8], rp = (nint)v[13];
            Release(sc);

            _origPresent = Marshal.GetDelegateForFunctionPointer<PresentDel>(
                HookHelper.Instance.Hook(pp, Marshal.GetFunctionPointerForDelegate(new PresentDel(HookPresent))));
            _origResize = Marshal.GetDelegateForFunctionPointer<ResizeDel>(
                HookHelper.Instance.Hook(rp, Marshal.GetFunctionPointerForDelegate(new ResizeDel(HookResize))));

            if (s_pending != null) { _onRender += s_pending; s_pending = null; }
            _ok = true; Logger.Info(nameof(D3D11Renderer), "OK"); return true;
        }
        catch (Exception e) { return Fail($"Install: {e}"); }
    }

    int HookPresent(nint sc, int sync, int flags)
    {
        try
        {
            if (!_inited) Init(sc);

            if (_rtv != 0) { Release(_rtv); _rtv = 0; }

            // GetBuffer(0)
            nint bb = 0;
            var g = Guid_Texture2D;
            QI(sc, 9, &g, &bb);
            if (bb != 0)
            {
                // CreateRenderTargetView
                nint rtv = 0;
                ((delegate* unmanaged[Stdcall]<nint, nint, void*, nint*, void>)Vtbl(_dev)[20])(_dev, bb, null, &rtv);
                _rtv = rtv;
                Release(bb);
            }

            // Desc → DisplaySize
            var d = ReadDesc(sc);
            ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(d.Width, d.Height);

            ImGui_ImplDX11_NewFrame();
            ImGui_ImplWin32_NewFrame();
            ImGui.NewFrame();
            try { _onRender(); } catch (Exception ex) { Logger.Error(nameof(D3D11Renderer), $"Render: {ex}"); }
            ImGui.EndFrame(); ImGui.Render();

            if (_rtv != 0)
            {
                var r = _rtv;
                ((delegate* unmanaged[Stdcall]<nint, int, nint*, nint, void>)Vtbl(_ctx)[8])(_ctx, 1, &r, 0);
            }
            ImGui_ImplDX11_RenderDrawData(ImGui.GetDrawData());
        }
        catch (Exception ex) { Logger.Error(nameof(D3D11Renderer), $"Present: {ex}"); }
        return _origPresent!(sc, sync, flags);
    }

    int HookResize(nint sc, int cnt, int w, int h, int fmt, int flags)
    {
        if (_rtv != 0) { Release(_rtv); _rtv = 0; }
        return _origResize!(sc, cnt, w, h, fmt, flags);
    }

    void Init(nint sc)
    {
        // sc->GetDevice → dev
        nint dev = 0;
        nint ctx = 0;
        var g = Guid_Device;
        QI(sc, 7, &g, &dev);
        _dev = dev;
        // dev->GetImmediateContext → ctx
        g = Guid_Context;
        QI(dev, 14, &g, &ctx);
        _ctx = ctx;

        // GetDesc → hwnd
        var d = ReadDesc(sc);
        _hwnd = d.OutputWindow;
        Logger.Info(nameof(D3D11Renderer), $"dev={_dev:X} ctx={_ctx:X} hwnd={_hwnd:X}");

        // WndProc
        _wndDel = WndHook;
        _origWnd = Win32Native.SetWindowLongPtrW(_hwnd, -4, Marshal.GetFunctionPointerForDelegate(_wndDel));

        // ImGui
        ImGui.CreateContext();
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        ImGui_ImplWin32_Init(_hwnd);
        ImGui_ImplDX11_Init(_dev, _ctx);
        _inited = true; Logger.Info(nameof(D3D11Renderer), "ImGui ready");
    }

    // ── QI helper (COM QueryInterface-like via vtable index) ──
    static void QI(nint p, int idx, void* guid, void* outPtr) =>
        ((delegate* unmanaged[Stdcall]<nint, void*, void*, int>)Vtbl(p)[idx])(p, guid, outPtr);

    // ── WndProc ──
    delegate nint WndDel(nint h, uint m, nint w, nint l);
    WndDel? _wndDel;
    nint WndHook(nint h, uint m, nint w, nint l)
    {
        if (ImGui_ImplWin32_WndProcHandler(h, m, w, l) != 0) return 1;
        return Win32Native.CallWindowProcW(_origWnd, h, m, w, l);
    }

    static Win32Native.DXGISwapChainDesc ReadDesc(nint sc)
    {
        var d = new Win32Native.DXGISwapChainDesc();
        ((delegate* unmanaged[Stdcall]<nint, Win32Native.DXGISwapChainDesc*, int>)Vtbl(sc)[12])(sc, &d);
        return d;
    }

    // ── Dummy swapchain ──
    static nint CreateDummy()
    {
        var hwnd = Win32.CreateDummyWindow();
        var sd = new Win32Native.DXGISwapChainDesc { Width = 1, Height = 1, BufferUsage = 0x20, BufferCount = 1, SampleCount = 1, OutputWindow = hwnd };
        // Format = 28 (DXGI_FORMAT_R8G8B8A8_UNORM)
        sd.Format = 28;
        var hr = Win32Native.D3D11CreateDeviceAndSwapChain(
            0, 1, 0, 0, 0, 0, 7,
            ref sd, out var sc, out var dev, out var ctx);
        if (hr != 0) return 0;
        if (ctx != 0) Release(ctx);
        if (dev != 0) Release(dev);
        Win32.DestroyWindow(hwnd);
        return sc;
    }

    static void Release(nint p) =>
        ((delegate* unmanaged[Stdcall]<nint, uint>)Vtbl(p)[2])(p);

    // ── GUIDs ──
    static Guid Guid_Device => new("db6f6ddb-ac77-4e88-8253-819df9bbf140");
    static Guid Guid_Context => new("c0bfa96c-e089-44fb-8eaf-26f87994ae84");
    static Guid Guid_Texture2D => new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    bool Fail(string m) { Logger.Error(nameof(D3D11Renderer), m); return false; }

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern bool ImGui_ImplWin32_Init(nint hwnd);
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern void ImGui_ImplWin32_NewFrame();
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern nint ImGui_ImplWin32_WndProcHandler(nint h, uint m, nint w, nint l);
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern bool ImGui_ImplDX11_Init(nint d, nint ctx);
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern void ImGui_ImplDX11_NewFrame();
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    static extern void ImGui_ImplDX11_RenderDrawData(ImDrawDataPtr dd);
}

static class Win32
{
    public delegate nint WndProc(nint h, uint m, nint w, nint l);

    static WndProc? _dummyWnd;
    static nint _dummyHwnd;

    public static nint CreateDummyWindow()
    {
        var inst = Win32Native.GetModuleHandleW(null);
        _dummyWnd = (h, m, wp, lp) => Win32Native.DefWindowProcW(h, m, wp, lp);
        var wc = new Win32Native.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Win32Native.WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_dummyWnd),
            hInstance = inst,
            lpszClassName = "D3D11HookDummy",
        };
        Win32Native.RegisterClassExW(ref wc);
        _dummyHwnd = Win32Native.CreateWindowExW(0, "D3D11HookDummy", "", 0, 0, 0, 1, 1, 0, 0, inst, 0);
        return _dummyHwnd;
    }

    public static void DestroyWindow(nint h)
    {
        Win32Native.DestroyWindow(h);
        Win32Native.UnregisterClassW("D3D11HookDummy", Win32Native.GetModuleHandleW(null));
    }
}


