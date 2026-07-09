// Shared hook state and helpers
#pragma once
#include "main.h"
#include "sharpdelegate.h"

extern ImGuiCallbacks imgui_callbacks;
extern bool g_BridgeMode;

typedef HRESULT(APIENTRY* PresentFn)(IDXGISwapChain*, UINT, UINT);
extern PresentFn oPresent;
typedef void(APIENTRY* ExecuteCommandListsFn)(ID3D12CommandQueue*, UINT, ID3D12CommandList*);
extern ExecuteCommandListsFn oExecuteCommandLists;

extern bool ShowMenu;
extern bool ImGui_Initialised;

// ---- window handle (from swapchain desc via kiero) ----
extern HWND g_GameWindow;
extern WNDPROC g_OriginalWndProc;

namespace DX12 {
    extern ID3D12Device* Device;
    extern ID3D12DescriptorHeap* RTVHeap, *SRVHeap;
    extern ID3D12GraphicsCommandList* CmdList;
    extern ID3D12CommandQueue* CmdQueue;
    struct FCtx { ID3D12CommandAllocator* Alloc; ID3D12Resource* Res; D3D12_CPU_DESCRIPTOR_HANDLE Handle; };
    extern UINT BufCount; extern FCtx* Frames;
    void Init(IDXGISwapChain* sc);
    void Render(IDXGISwapChain* sc);
}
namespace DX11 { extern ID3D11Device* Device; extern ID3D11DeviceContext* Context; extern ID3D11RenderTargetView* MainRTV;
    extern bool Initialized;
    bool Install(); }
namespace DX9  { extern IDirect3DDevice9* Device;
    void Init(IDXGISwapChain* sc); void Render(IDXGISwapChain* sc); }

LRESULT APIENTRY WndProc(HWND h, UINT m, WPARAM w, LPARAM l);
void APIENTRY hkExecuteCommandLists(ID3D12CommandQueue*, UINT, ID3D12CommandList*);
HRESULT APIENTRY hkPresent(IDXGISwapChain*, UINT, UINT);
