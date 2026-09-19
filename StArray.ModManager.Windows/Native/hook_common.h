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
    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);
}

namespace DX11Hook {
    extern IDXGISwapChain* SwapChain;
    extern ID3D11DeviceContext* DeviceContext;

    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);

    // wndproc subclass control (soft disable/enable)
    void InstallWndProc();
    void UninstallWndProc();
}

namespace DX12Hook {
    extern IDXGISwapChain* SwapChain;
    extern ID3D12Device* Device;
    extern ID3D12CommandQueue* CommandQueue;

    bool InstallHook();
    HRESULT HookPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
    LRESULT APIENTRY HookWndProc(HWND h, UINT m, WPARAM w, LPARAM l);
}
