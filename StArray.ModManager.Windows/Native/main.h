#include <windows.h>
#include <psapi.h>
#include <dxgi1_4.h>
#include <d3d12.h>
#include <d3d11.h>
#include <d3d9.h>
#include <cassert>
#include <cstdarg>
#include <fstream>
using namespace std;

// ---- MinHook (submodule) ----
#include "minhook/include/MinHook.h"

// ---- cimgui (core ImGui C API) ----
#define CIMGUI_DEFINE_ENUMS_AND_STRUCTS
#include "cimgui.h"

// ---- kiero2 (submodule) ----
#include "kiero2/kiero.hpp"
#include "kiero2/kiero_d3d9.hpp"
#include "kiero2/kiero_d3d11.hpp"
#include "kiero2/kiero_d3d12.hpp"
#include "kiero2/kiero_opengl.hpp"
#include "kiero2/kiero_vulkan.hpp"

// ---- ImGui backend decls (linked from cimgui.dll) ----
struct ImGui_ImplDX12_InitInfo
{
    ID3D12Device*               Device = nullptr;
    ID3D12CommandQueue*         CommandQueue = nullptr;
    int                         NumFramesInFlight = 0;
    DXGI_FORMAT                 RTVFormat = DXGI_FORMAT_UNKNOWN;
    DXGI_FORMAT                 DSVFormat = DXGI_FORMAT_UNKNOWN;
    void*                       UserData = nullptr;
    ID3D12DescriptorHeap*       SrvDescriptorHeap = nullptr;
    void (*SrvDescriptorAllocFn)(ImGui_ImplDX12_InitInfo*, D3D12_CPU_DESCRIPTOR_HANDLE*, D3D12_GPU_DESCRIPTOR_HANDLE*) = nullptr;
    void (*SrvDescriptorFreeFn)(ImGui_ImplDX12_InitInfo*, D3D12_CPU_DESCRIPTOR_HANDLE, D3D12_GPU_DESCRIPTOR_HANDLE) = nullptr;
};

extern "C" {
    // Win32
    bool ImGui_ImplWin32_Init(void* hwnd);
    void ImGui_ImplWin32_NewFrame();
    void ImGui_ImplWin32_Shutdown();
    LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
    // D3D12
    bool ImGui_ImplDX12_Init(ImGui_ImplDX12_InitInfo* info);
    void ImGui_ImplDX12_NewFrame();
    void ImGui_ImplDX12_RenderDrawData(ImDrawData* draw_data, ID3D12GraphicsCommandList* cmdlist);
    bool ImGui_ImplDX12_CreateDeviceObjects();
    void ImGui_ImplDX12_Shutdown();
    // D3D11
    bool ImGui_ImplDX11_Init(ID3D11Device* device, ID3D11DeviceContext* ctx);
    void ImGui_ImplDX11_NewFrame();
    void ImGui_ImplDX11_RenderDrawData(ImDrawData* draw_data);
    void ImGui_ImplDX11_Shutdown();
    // D3D9
    bool ImGui_ImplDX9_Init(IDirect3DDevice9* device);
    void ImGui_ImplDX9_NewFrame();
    void ImGui_ImplDX9_RenderDrawData(ImDrawData* draw_data);
    void ImGui_ImplDX9_Shutdown();
}

// ---- Logging ----
char dlldir[320];
char* GetDirectoryFile(char* filename) {
    static char path[320];
    strcpy_s(path, dlldir);
    strcat_s(path, filename);
    return path;
}

inline void Log(const char* fmt, ...) {
    if (!fmt) return;
    char text[4096];
    va_list ap;
    va_start(ap, fmt);
    vsprintf_s(text, fmt, ap);
    va_end(ap);
    OutputDebugStringA(text); OutputDebugStringA("\n");
    HANDLE h = GetStdHandle(STD_OUTPUT_HANDLE);
    if (h && h != INVALID_HANDLE_VALUE) { DWORD w; WriteConsoleA(h, text, (DWORD)strlen(text), &w, NULL); WriteConsoleA(h, "\n", 1, &w, NULL); }
    ofstream f(GetDirectoryFile((PCHAR)"log.txt"), ios::app);
    if (f.is_open()) f << text << endl;
}

// ---- Type helpers ----
#if defined _M_X64
typedef uint64_t uintx_t;
#elif defined _M_IX86
typedef uint32_t uintx_t;
#endif

// ---- Hook helpers (using kiero, tries all 5 backends) ----
static kiero::D3D9Output    g_KieroD3D9;
static kiero::D3D11Output   g_KieroD3D11;
static kiero::D3D12Output   g_KieroD3D12;
static kiero::OpenGLOutput  g_KieroOpenGL;
static kiero::VulkanOutput  g_KieroVulkan;
static int g_KieroBackend = -1; // 0=D3D9 1=D3D11 2=D3D12 3=OpenGL 4=Vulkan

inline bool HookInit() {
    MH_Initialize();

    #define TRY_KIERO(id, Impl, Out) \
        if (g_KieroBackend < 0) { \
            auto err = kiero::locate<kiero::Impl>(nullptr, &g_##Out); \
            if (err == kiero::Error_Nil) { g_KieroBackend = id; } \
        }

    TRY_KIERO(0, Implementation_D3D12, KieroD3D12);
    TRY_KIERO(1, Implementation_D3D11, KieroD3D11);
    TRY_KIERO(2, Implementation_D3D9,  KieroD3D9);
    TRY_KIERO(3, Implementation_OpenGL, KieroOpenGL);
    TRY_KIERO(4, Implementation_Vulkan, KieroVulkan);

    return g_KieroBackend >= 0;
}

// Per-backend vtable accessors
inline void* D3D12_DEV(int i)  { return g_KieroD3D12.device_methods[i]; }
inline void* D3D12_CQ(int i)   { return g_KieroD3D12.command_queue_methods[i]; }
inline void* D3D12_CL(int i)   { return g_KieroD3D12.command_list_methods[i]; }
inline void* D3D12_SWAP(int i) { return g_KieroD3D12.swapchain_methods[i]; }
inline void* D3D11_SWAP(int i) { return g_KieroD3D11.swapchain_methods[i]; }
inline void* D3D11_DEV(int i)  { return g_KieroD3D11.device_methods[i]; }
inline void* D3D11_CTX(int i)  { return g_KieroD3D11.context_methods[i]; }
inline void* D3D9_DEV(int i)   { return g_KieroD3D9.device_methods[i]; }

// Generic: pick based on active backend (D3D9/11/12 only for actual hooking)
inline void* KieroMethod(int d3d12Idx, int d3d11Idx, int d3d9Idx) {
    switch (g_KieroBackend) {
        case 0: return d3d12Idx >= 0 && (size_t)d3d12Idx < g_KieroD3D12.command_queue_methods.size() ? D3D12_CQ(d3d12Idx) : nullptr;
        case 1: return d3d11Idx >= 0 && (size_t)d3d11Idx < g_KieroD3D11.context_methods.size()      ? D3D11_CTX(d3d11Idx) : nullptr;
        case 2: return d3d9Idx  >= 0 && (size_t)d3d9Idx  < g_KieroD3D9.device_methods.size()         ? D3D9_DEV(d3d9Idx)   : nullptr;
        default: return nullptr;
    }
}

inline void* KieroSwap(int d3d12Idx, int d3d11Idx, int d3d9Idx) {
    switch (g_KieroBackend) {
        case 0: return d3d12Idx >= 0 && (size_t)d3d12Idx < g_KieroD3D12.swapchain_methods.size() ? D3D12_SWAP(d3d12Idx) : nullptr;
        case 1: return d3d11Idx >= 0 && (size_t)d3d11Idx < g_KieroD3D11.swapchain_methods.size() ? D3D11_SWAP(d3d11Idx) : nullptr;
        case 2: return d3d9Idx  >= 0 && (size_t)d3d9Idx  < g_KieroD3D9.device_methods.size()      ? D3D9_DEV(d3d9Idx)   : nullptr;
        default: return nullptr;
    }
}

inline void DisableAll() { MH_DisableHook(MH_ALL_HOOKS); MH_Uninitialize(); }
