// P/Invoke exports — C# interop entry points
#include "hook_common.h"

static int g_Backend = 0; // 0=DX11

// ---- Global state (declared extern in hook_common.h) ----
ImGuiCallbacks imgui_callbacks = {};
bool ImGui_Initialised = false;
HWND g_GameWindow = nullptr;
WNDPROC g_OriginalWndProc = nullptr;

extern "C" __declspec(dllexport) int __cdecl SetBackend(int backend) {
    g_Backend = backend;
    return 0;
}

static DWORD WINAPI HookThread(LPVOID) {
    if (g_Backend == 1) {
        DX11Hook::InstallHook();
    }
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl Init(
    ImGuiInitCallbackFn ic, ImGuiShutdownCallbackFn sc, ImGuiRenderCallbackFn rc)
{
    imgui_callbacks = { ic, sc, rc };
    CreateThread(nullptr, 0, HookThread, nullptr, 0, nullptr);
    return 0;
}

// Soft disable/enable: unhook Present + WndProc without tearing down MinHook/ImGui,
// so the manager can be re-enabled later in the same session.
extern "C" __declspec(dllexport) int __cdecl DisableHooks() {
    DX11Hook::UninstallWndProc();
    return MH_DisableHook(MH_ALL_HOOKS);
}

extern "C" __declspec(dllexport) int __cdecl EnableHooks() {
    int r = MH_EnableHook(MH_ALL_HOOKS);
    DX11Hook::InstallWndProc();
    return r;
}


// ---- DLL Entry Point ----
BOOL APIENTRY DllMain(HMODULE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(h);
    } else if (r == DLL_PROCESS_DETACH) {
        if (imgui_callbacks.shutdown_callback) imgui_callbacks.shutdown_callback();
        DisableAll();
    }
    return TRUE;
}

