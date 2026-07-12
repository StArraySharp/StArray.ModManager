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


// ---- DLL Entry Point ----
BOOL APIENTRY DllMain(HMODULE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(h);
        /*SetBackend(1);
        Init([] {
            igCreateContext(NULL);
            return (void*)nullptr;
        }, []{ igDestroyContext(igGetCurrentContext());},
        [] {
            igNewFrame();
            igBegin("Dear ImGui", NULL, 0);
            igText("text");
            igEnd();
            igEndFrame();
            igRender();
        });*/
    } else if (r == DLL_PROCESS_DETACH) {
        if (imgui_callbacks.shutdown_callback) imgui_callbacks.shutdown_callback();
        DisableAll();
    }
    return TRUE;
}

// ---- P/Invoke Exports ----
