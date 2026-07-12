// Shared hook state and helpers
#pragma once
#include "main.h"
#include <d3d9.h>
#include <d3d11.h>
#include <d3d12.h>

typedef void* (*ImGuiInitCallbackFn)();
typedef void  (*ImGuiShutdownCallbackFn)();
typedef void  (*ImGuiRenderCallbackFn)();
struct ImGuiCallbacks { ImGuiInitCallbackFn init_callback; ImGuiShutdownCallbackFn shutdown_callback; ImGuiRenderCallbackFn render_callback; };
extern ImGuiCallbacks imgui_callbacks;

extern bool ImGui_Initialised;

extern HWND g_GameWindow;
extern WNDPROC g_OriginalWndProc;

namespace DX9Hook {
    //vars

    //hook funcs
    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);
}

namespace DX11Hook {
    //vars
    extern IDXGISwapChain* SwapChain;
    extern ID3D11DeviceContext* DeviceContext;
    //more

    //hook funcs
    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);
}

namespace DX12Hook {
    //vars
    extern IDXGISwapChain* SwapChain;
    extern ID3D12Device* Device;
    extern ID3D12CommandQueue* CommandQueue;
    //more

    //hook funcs
    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);
    //more

}