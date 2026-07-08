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
inline char dlldir[320];
inline char* GetDirectoryFile(char* filename) {
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

// Conditional debug logging: enabled in Debug builds, compiled out in Release
#ifndef NDEBUG
#define DEBUG_LOG(fmt, ...) Log("[DEBUG] " fmt, ##__VA_ARGS__)
#else
#define DEBUG_LOG(fmt, ...) ((void)0)
#endif

// ---- Type helpers ----
#if defined _M_X64
typedef uint64_t uintx_t;
#elif defined _M_IX86
typedef uint32_t uintx_t;
#endif

// ---- kiero2 globals (populated by MainThread) ----
static kiero::D3D9Output    g_KieroD3D9;
static kiero::D3D11Output   g_KieroD3D11;
static kiero::D3D12Output   g_KieroD3D12;
static kiero::OpenGLOutput  g_KieroOpenGL;
static kiero::VulkanOutput  g_KieroVulkan;

// Per-backend vtable accessors
inline void* D3D12_DEV(int i)  { return g_KieroD3D12.device_methods[i]; }
inline void* D3D12_CQ(int i)   { return g_KieroD3D12.command_queue_methods[i]; }
inline void* D3D12_CL(int i)   { return g_KieroD3D12.command_list_methods[i]; }
inline void* D3D12_SWAP(int i) { return g_KieroD3D12.swapchain_methods[i]; }
inline void* D3D11_SWAP(int i) { return g_KieroD3D11.swapchain_methods[i]; }
inline void* D3D11_DEV(int i)  { return g_KieroD3D11.device_methods[i]; }
inline void* D3D11_CTX(int i)  { return g_KieroD3D11.context_methods[i]; }
inline void* D3D9_DEV(int i)   { return g_KieroD3D9.device_methods[i]; }


inline void DisableAll() { MH_DisableHook(MH_ALL_HOOKS); MH_Uninitialize(); }
