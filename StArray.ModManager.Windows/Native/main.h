#include <windows.h>
#include <d3d11.h>
#include <cstdarg>
#include <fstream>

// ---- MinHook ----
#include "minhook/include/MinHook.h"

// ---- cimgui (core ImGui C API) ----
#define CIMGUI_DEFINE_ENUMS_AND_STRUCTS
#include "cimgui/cimgui.h"

// ImGui backend functions (defined in imgui_impl_*.cpp, compiled into this DLL)
bool ImGui_ImplWin32_Init(void* hwnd);
void ImGui_ImplWin32_NewFrame();
void ImGui_ImplWin32_Shutdown();
LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
bool ImGui_ImplDX11_Init(ID3D11Device* device, ID3D11DeviceContext* ctx);
void ImGui_ImplDX11_NewFrame();
void ImGui_ImplDX11_RenderDrawData(ImDrawData* draw_data);
void ImGui_ImplDX11_Shutdown();

// ---- Logging ----
inline char dlldir[320];
inline char* GetDirectoryFile(char* filename) {
    static char path[320];
    strcpy_s(path, dlldir);
    strcat_s(path, filename);
    return path;
}

inline bool IsIl2Cpp() {
    return GetModuleHandle("GameAssembly.dll") != nullptr;
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

    // Rotate: backup old log before first write
    static bool s_logRotated = false;
    if (!s_logRotated) {
        s_logRotated = true;
        char* logPath = GetDirectoryFile((PCHAR)"log.txt");
        char* bakPath = GetDirectoryFile((PCHAR)"lastlog.txt");
        DeleteFileA(bakPath);
        MoveFileA(logPath, bakPath);
    }

    std::ofstream f(GetDirectoryFile((PCHAR)"log.txt"), std::ios::app);
    if (f.is_open()) f << text << std::endl;
}

// Conditional debug logging: enabled in Debug builds, compiled out in Release
#ifndef NDEBUG
#define DEBUG_LOG(fmt, ...) Log("[DEBUG] " fmt, ##__VA_ARGS__)
#else
#define DEBUG_LOG(fmt, ...) ((void)0)
#endif

inline void DisableAll() { MH_DisableHook(MH_ALL_HOOKS); MH_Uninitialize(); }
