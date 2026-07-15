// DX11 Present hook — cimgui-based ImGui overlay
#include "hook_common.h"
#include "cimgui.h"
#include "kiero.hpp"
#include "kiero_d3d11.hpp"

typedef HRESULT(APIENTRY* PresentFn)(IDXGISwapChain*, UINT, UINT);

namespace DX11Hook {
    IDXGISwapChain* SwapChain = nullptr;
    ID3D11DeviceContext* DeviceContext = nullptr;

    static PresentFn OriginalPresent = nullptr;
    static bool Initialised = false;

    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l) {
        if (ImGui_ImplWin32_WndProcHandler(h, m, w, l))
            return true;

        if (m == WM_SIZE && w != SIZE_MINIMIZED && SwapChain) {
            SwapChain->ResizeBuffers(0, LOWORD(l), HIWORD(l), DXGI_FORMAT_UNKNOWN, 0);

            ID3D11Device* device = nullptr;
            if (SUCCEEDED(SwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&device))) {
                ID3D11DeviceContext* context = nullptr;
                device->GetImmediateContext(&context);
                if (context) {
                    D3D11_VIEWPORT vp = {};
                    vp.Width = (FLOAT)LOWORD(l);
                    vp.Height = (FLOAT)HIWORD(l);
                    vp.MinDepth = 0.0f;
                    vp.MaxDepth = 1.0f;
                    context->RSSetViewports(1, &vp);
                    context->Release();
                }
                device->Release();
            }
            // Fall through to let the game handle WM_SIZE too
        }

        return CallWindowProcW(g_OriginalWndProc, h, m, w, l);
    }

    ID3D11RenderTargetView *MainRTV = nullptr;

    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags) {
        if (!Initialised) {
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain->GetDesc(&desc);
            g_GameWindow = desc.OutputWindow;

            ID3D11Device* device = nullptr;
            swapChain->GetDevice(__uuidof(ID3D11Device), (void**)&device);
            device->GetImmediateContext(&DeviceContext);
            if (imgui_callbacks.init_callback) imgui_callbacks.init_callback();

            if (ImGui_ImplWin32_Init(g_GameWindow) && ImGui_ImplDX11_Init(device, DeviceContext)) {
                SwapChain = swapChain;
                g_OriginalWndProc = (WNDPROC)SetWindowLongPtrW(g_GameWindow,
                    GWLP_WNDPROC, (LONG_PTR)HookWndProc);
                Initialised = true;
                ImGui_Initialised = true;
                DEBUG_LOG("DX11Hook: ImGui initialised, hwnd=%p", g_GameWindow);
            }
        }

        if (Initialised) {
            /*if (MainRTV) { MainRTV->Release(); MainRTV = nullptr; }
            ID3D11Texture2D* bb = nullptr;
            swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&bb);
            ID3D11Device* device = nullptr;
            swapChain->GetDevice(__uuidof(ID3D11Device), (void**)&device);
            if (bb) { device->CreateRenderTargetView(bb, nullptr, &MainRTV); bb->Release(); }
            */

            ImGui_ImplDX11_NewFrame();
            ImGui_ImplWin32_NewFrame();
            auto io = igGetIO();
            DXGI_SWAP_CHAIN_DESC desc;
            swapChain->GetDesc(&desc);
            io->DisplaySize.x = desc.BufferDesc.Width;
            io->DisplaySize.y = desc.BufferDesc.Height;
            if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();
            ImGui_ImplDX11_RenderDrawData(igGetDrawData());

            //if (MainRTV) DeviceContext->OMSetRenderTargets(1, &MainRTV, nullptr);
        }

        return OriginalPresent(swapChain, syncInterval, flags);
    }

    bool InstallHook() {
        DEBUG_LOG("DX11Hook::InstallHook: locating D3D11 methods via kiero...");

        kiero::D3D11Output output;
        auto err = kiero::locate<kiero::Implementation_D3D11>(nullptr, &output);
        if (err != kiero::Error_Nil) {
            DEBUG_LOG("DX11Hook::InstallHook: kiero locate failed (err=%d)", err);
            return false;
        }

        if (output.swapchain_methods.size() <= 8 || !output.swapchain_methods[8]) {
            DEBUG_LOG("DX11Hook::InstallHook: Present method not found in vtable");
            return false;
        }

        auto presentAddr = output.swapchain_methods[8];
        DEBUG_LOG("DX11Hook::InstallHook: Present found at %p", presentAddr);

        MH_Initialize();
        if (MH_CreateHook(presentAddr, (void*)HookPresent, (void**)&OriginalPresent) != MH_OK
            || MH_EnableHook(presentAddr) != MH_OK) {
            DEBUG_LOG("DX11Hook::InstallHook: MinHook failed");
            return false;
        }

        DEBUG_LOG("DX11Hook::InstallHook: hook installed successfully");
        return true;
    }
}
