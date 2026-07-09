// D3D11 manual hook — dummy device vtable, no kiero2 dependency
#include "hook_common.h"

namespace DX11 {
    // Vtable indices (IDXGISwapChain, 18 methods)
    enum { VT_PRESENT = 8, VT_RESIZE_BUFFERS = 13, VT_COUNT = 18 };

    static void*     g_VTable[VT_COUNT] = {};
    static PresentFn g_OriginalPresent = nullptr;

    ID3D11Device*           Device = nullptr;
    ID3D11DeviceContext*    Context = nullptr;
    ID3D11RenderTargetView* MainRTV = nullptr;
    bool                    Initialized = false;

    // ---- Extract vtable from dummy device + swapchain ----
    static bool ExtractVTable() {
        DEBUG_LOG("DX11: creating dummy device for vtable...");

        WNDCLASSA wc = {};
        wc.lpfnWndProc = DefWindowProcA;
        wc.hInstance = GetModuleHandle(nullptr);
        wc.lpszClassName = "DX11Dummy";
        RegisterClassA(&wc);
        HWND hwnd = CreateWindowA("DX11Dummy", "", 0, 0, 0, 1, 1,
            nullptr, nullptr, wc.hInstance, nullptr);

        DXGI_SWAP_CHAIN_DESC sd = {};
        sd.BufferCount = 1;
        sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        sd.BufferDesc.Width = 1; sd.BufferDesc.Height = 1;
        sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        sd.OutputWindow = hwnd;
        sd.SampleDesc.Count = 1;
        sd.Windowed = TRUE;

        IDXGISwapChain*   dummySC = nullptr;
        ID3D11Device*     dummyDev = nullptr;
        ID3D11DeviceContext* dummyCtx = nullptr;

        HRESULT hr = D3D11CreateDeviceAndSwapChain(
            nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
            nullptr, 0, D3D11_SDK_VERSION,
            &sd, &dummySC, &dummyDev, nullptr, &dummyCtx);

        if (FAILED(hr)) {
            DEBUG_LOG("DX11: D3D11CreateDeviceAndSwapChain FAILED 0x%08X", hr);
            DestroyWindow(hwnd); UnregisterClassA("DX11Dummy", wc.hInstance);
            return false;
        }

        memcpy(g_VTable, *(void***)dummySC, VT_COUNT * sizeof(void*));
        dummyCtx->Release(); dummyDev->Release(); dummySC->Release();
        DestroyWindow(hwnd); UnregisterClassA("DX11Dummy", wc.hInstance);

        DEBUG_LOG("DX11: vtable OK Present=%p Resize=%p",
            g_VTable[VT_PRESENT], g_VTable[VT_RESIZE_BUFFERS]);
        return true;
    }

    // ---- Init (called on first Present frame) ----
    void Init(IDXGISwapChain* sc) {
        DEBUG_LOG("DX11::Init: sc=%p", sc);
        HRESULT hr = sc->GetDevice(__uuidof(ID3D11Device), (void**)&Device);
        if (FAILED(hr)) {
            IUnknown* unk = nullptr;
            if (SUCCEEDED(sc->GetDevice(IID_IUnknown, (void**)&unk)) && unk) {
                hr = unk->QueryInterface(__uuidof(ID3D11Device), (void**)&Device);
                unk->Release();
            }
        }
        if (FAILED(hr) || !Device) {
            DEBUG_LOG("DX11::Init: GetDevice FAILED hr=0x%08X", hr); return;
        }
        Device->GetImmediateContext(&Context);
        DEBUG_LOG("DX11::Init: device=%p context=%p", Device, Context);

        // Get game window from swapchain (g_GameWindow not set by main hkPresent for D3D11)
        {
            DXGI_SWAP_CHAIN_DESC sd;
            sc->GetDesc(&sd);
            g_GameWindow = sd.OutputWindow;
            DEBUG_LOG("DX11::Init: OutputWindow=%p (%dx%d)", g_GameWindow, sd.BufferDesc.Width, sd.BufferDesc.Height);
        }
        imgui_callbacks.init_callback();
        ImGui_ImplWin32_Init(g_GameWindow);
        g_OriginalWndProc = (WNDPROC)SetWindowLongPtr(g_GameWindow, GWLP_WNDPROC, (__int3264)(LONG_PTR)WndProc);
        ImGui_ImplDX11_Init(Device, Context);
        Initialized = true;
        DEBUG_LOG("DX11::Init: complete");
    }

    // ---- Present hook (self-contained) ----
    HRESULT APIENTRY hkPresent(IDXGISwapChain* sc, UINT sync, UINT flags) {
        if (!Initialized) Init(sc);

        // Recreate RTV each frame (handles resize)
        if (MainRTV) { MainRTV->Release(); MainRTV = nullptr; }
        ID3D11Texture2D* bb = nullptr;
        sc->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&bb);
        if (bb) { Device->CreateRenderTargetView(bb, nullptr, &MainRTV); bb->Release(); }

        // DisplaySize from swapchain desc (override Win32_NewFrame client rect)
        DXGI_SWAP_CHAIN_DESC sd; sc->GetDesc(&sd);
        ImGuiIO* io = igGetIO();
        io->DisplaySize.x = (float)sd.BufferDesc.Width;
        io->DisplaySize.y = (float)sd.BufferDesc.Height;

        ImGui_ImplDX11_NewFrame();
        if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();
        if (MainRTV) Context->OMSetRenderTargets(1, &MainRTV, nullptr);
        ImGui_ImplDX11_RenderDrawData(igGetDrawData());

        return g_OriginalPresent(sc, sync, flags);
    }

    // ---- Install hooks ----
    bool Install() {
        MH_Initialize();
        if (!ExtractVTable()) return false;

        MH_CreateHook(g_VTable[VT_PRESENT], (void*)hkPresent, (void**)&g_OriginalPresent);
        MH_EnableHook(g_VTable[VT_PRESENT]);
        DEBUG_LOG("DX11: Present hook installed via manual vtable");
        return true;
    }
}
