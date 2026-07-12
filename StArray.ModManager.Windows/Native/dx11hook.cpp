// DX11 Present hook — cimgui-based ImGui overlay
#include "hook_common.h"

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

            device->Release();
        }

        if (Initialised) {
            ImGui_ImplDX11_NewFrame();
            ImGui_ImplWin32_NewFrame();
            if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();
            ImGui_ImplDX11_RenderDrawData(igGetDrawData());
        }

        return OriginalPresent(swapChain, syncInterval, flags);
    }

    bool InstallHook() {
        DEBUG_LOG("DX11Hook::InstallHook: creating dummy device...");

        WNDCLASSEXW wc = {};
        wc.cbSize = sizeof(wc);
        wc.lpfnWndProc = DefWindowProcW;
        wc.hInstance = GetModuleHandleW(nullptr);
        wc.lpszClassName = L"DX11HookDummy";
        RegisterClassExW(&wc);

        HWND dummy = CreateWindowExW(0, L"DX11HookDummy", L"", WS_POPUP,
            0, 0, 1, 1, nullptr, nullptr, wc.hInstance, nullptr);

        DXGI_SWAP_CHAIN_DESC scd = {};
        scd.BufferCount = 1;
        scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        scd.BufferDesc.Width = 1;
        scd.BufferDesc.Height = 1;
        scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        scd.OutputWindow = dummy;
        scd.SampleDesc.Count = 1;
        scd.Windowed = TRUE;

        ID3D11Device* tmpDevice = nullptr;
        IDXGISwapChain* tmpSwapChain = nullptr;
        ID3D11DeviceContext* tmpContext = nullptr;

        bool ok = false;
        if (SUCCEEDED(D3D11CreateDeviceAndSwapChain(
                nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
                nullptr, 0, D3D11_SDK_VERSION,
                &scd, &tmpSwapChain, &tmpDevice, nullptr, &tmpContext))) {

            MH_Initialize();
            void** vt = *(void***)tmpSwapChain;
            ok = (MH_CreateHook(vt[8], (void*)HookPresent, (void**)&OriginalPresent) == MH_OK)
              && (MH_EnableHook(vt[8]) == MH_OK);
            DEBUG_LOG("DX11Hook::InstallHook: Present hook %s", ok ? "OK" : "FAILED");

            tmpSwapChain->Release();
            tmpContext->Release();
            tmpDevice->Release();
        } else {
            DEBUG_LOG("DX11Hook::InstallHook: D3D11CreateDeviceAndSwapChain FAILED");
        }

        DestroyWindow(dummy);
        UnregisterClassW(L"DX11HookDummy", wc.hInstance);
        return ok;
    }
}
