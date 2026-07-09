// D3D11 hook: init + render
#include "hook_common.h"

namespace DX11 {
    ID3D11Device* Device = nullptr;
    ID3D11DeviceContext* Context = nullptr;
    ID3D11RenderTargetView* MainRTV = nullptr;
    bool Initialized = false;

    void Init(IDXGISwapChain* sc) {
        DEBUG_LOG("D3D11::Init: sc=%p, getting device...", sc);
        HRESULT hr = sc->GetDevice(__uuidof(ID3D11Device), (void**)&Device);
        if (FAILED(hr)) {
            IUnknown* unk = nullptr;
            if (SUCCEEDED(sc->GetDevice(IID_IUnknown, (void**)&unk)) && unk) {
                hr = unk->QueryInterface(__uuidof(ID3D11Device), (void**)&Device);
                unk->Release();
            }
        }
        if (FAILED(hr) || !Device) {
            DEBUG_LOG("D3D11::Init: GetDevice FAILED hr=0x%08X", hr); return;
        }
        Device->GetImmediateContext(&Context);
        DEBUG_LOG("D3D11::Init: device=%p context=%p", Device, Context);

        imgui_callbacks.init_callback();
        ImGui_ImplWin32_Init(g_GameWindow);
        ImGui_ImplDX11_Init(Device, Context);
        Initialized = true;
        DEBUG_LOG("D3D11::Init: complete");
    }

    void Render(IDXGISwapChain* sc) {
        // Recreate RTV each frame from current backbuffer (handles resize automatically)
        if (MainRTV) { MainRTV->Release(); MainRTV = nullptr; }
        ID3D11Texture2D* backBuf = nullptr;
        sc->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&backBuf);
        if (backBuf) {
            Device->CreateRenderTargetView(backBuf, nullptr, &MainRTV);
            backBuf->Release();
        }

        ImGui_ImplDX11_NewFrame();
        if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();
        if (MainRTV) Context->OMSetRenderTargets(1, &MainRTV, nullptr);
        ImGui_ImplDX11_RenderDrawData(igGetDrawData());
    }
}
