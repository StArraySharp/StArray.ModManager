// Unified hook dispatcher — delegates to per-backend hook_*.cpp
#include "hook_common.h"

ImGuiCallbacks imgui_callbacks = {};
bool g_BridgeMode = false;
PresentFn oPresent = nullptr;
ExecuteCommandListsFn oExecuteCommandLists = nullptr;
bool ShowMenu = false, ImGui_Initialised = false;

HWND g_GameWindow = nullptr;
WNDPROC g_OriginalWndProc = nullptr;
static HMODULE g_Module = nullptr;

static int g_WndProcMsgCount = 0;
LRESULT APIENTRY WndProc(HWND h, UINT m, WPARAM w, LPARAM l) {
    if (ImGui_ImplWin32_WndProcHandler(h, m, w, l)) return true;
    return CallWindowProc(g_OriginalWndProc, h, m, w, l);
}

HRESULT APIENTRY hkPresent(IDXGISwapChain* sc, UINT sync, UINT flags) {
    if (!ImGui_Initialised) {
        if (imgui_callbacks.init_callback == nullptr) return oPresent(sc, sync, flags);
        DEBUG_LOG("hkPresent: first frame init, backend=%d", g_KieroBackend);
        {
            DXGI_SWAP_CHAIN_DESC sd; sc->GetDesc(&sd);
            g_GameWindow = sd.OutputWindow;
            DEBUG_LOG("hkPresent: OutputWindow=%p (%dx%d)", g_GameWindow, sd.BufferDesc.Width, sd.BufferDesc.Height);
        }
        switch (g_KieroBackend) {
            case 2: DX9::Init(sc);  break;
            case 1: DX11::Init(sc); break;
            case 0: DX12::Init(sc); break;
            case 3: /* OpenGL  - only Win32 init, C# handles backend */
            case 4: /* Vulkan  - only Win32 init, C# handles backend */
                imgui_callbacks.init_callback();
                ImGui_ImplWin32_Init(g_GameWindow);
                DEBUG_LOG("hkPresent: GL/VK init via C#, no native backend");
                break;
            default: DEBUG_LOG("hkPresent: unknown backend=%d, skipping init", g_KieroBackend); break;
        }
        g_OriginalWndProc = (WNDPROC)SetWindowLongPtr(g_GameWindow, GWLP_WNDPROC, (__int3264)(LONG_PTR)WndProc);
        ImGui_Initialised = true;
        if (g_OriginalWndProc)
            DEBUG_LOG("hkPresent: wndproc hooked old=%p new=%p hwnd=%p OK", g_OriginalWndProc, WndProc, g_GameWindow);
        else
            DEBUG_LOG("hkPresent: SetWindowLongPtr FAILED err=%lu hwnd=%p", GetLastError(), g_GameWindow);
        {
            RECT cr; GetClientRect(g_GameWindow, &cr);
            DXGI_SWAP_CHAIN_DESC sd; sc->GetDesc(&sd);
            DEBUG_LOG("hkPresent: clientRect=%dx%d backbuf=%dx%d",
                cr.right-cr.left, cr.bottom-cr.top,
                sd.BufferDesc.Width, sd.BufferDesc.Height);
        }
    }

    if (GetAsyncKeyState(VK_INSERT) & 1) { ShowMenu = !ShowMenu; DEBUG_LOG("hkPresent: ShowMenu toggled -> %d", ShowMenu); }

    ImGuiIO* io = igGetIO();
    { DXGI_SWAP_CHAIN_DESC sd; sc->GetDesc(&sd);
      io->DisplaySize.x = (float)sd.BufferDesc.Width; io->DisplaySize.y = (float)sd.BufferDesc.Height; }
    ImGui_ImplWin32_NewFrame();

    switch (g_KieroBackend) {
        case 4: case 3: // VK/GL: C# render only
            if (imgui_callbacks.render_callback) imgui_callbacks.render_callback(); break;
        case 2: DX9::Render(sc);  break;
        case 1: DX11::Render(sc); break;
        default: DX12::Render(sc); break;
    }
    return oPresent(sc, sync, flags);
}

DWORD WINAPI MainThread(LPVOID) {
    DEBUG_LOG("MainThread: waiting for foreground window...");
    while (true) {
        DWORD pid; GetWindowThreadProcessId(GetForegroundWindow(), &pid);
        if (GetCurrentProcessId() == pid) {
            // kiero will give us the real OutputWindow from swapchain desc;
            // this foreground window is just for debug + sanity
            HWND fg = GetForegroundWindow();
            char buf[MAX_PATH];
            GetWindowTextA(fg, buf, sizeof(buf));
            DEBUG_LOG("MainThread: foreground hwnd=%p title=\"%s\"", fg, buf);
            break;
        }
        Sleep(100);
    }

    // ---- kiero2 backend detection (explicit locate, no macros) ----
    MH_Initialize();
    {
        kiero::D3D12Output   d3d12;
        kiero::D3D11Output   d3d11;
        kiero::D3D9Output    d3d9;
        kiero::OpenGLOutput  gl;
        kiero::VulkanOutput  vk;

        if      (kiero::locate<kiero::Implementation_D3D12>(nullptr, &d3d12) == kiero::Error_Nil)
            { g_KieroBackend = 0; g_KieroD3D12 = d3d12; DEBUG_LOG("MainThread: backend=D3D12"); }
        else if (kiero::locate<kiero::Implementation_D3D11>(nullptr, &d3d11) == kiero::Error_Nil)
            { g_KieroBackend = 1; g_KieroD3D11 = d3d11; DEBUG_LOG("MainThread: backend=D3D11"); }
        else if (kiero::locate<kiero::Implementation_D3D9>(nullptr, &d3d9) == kiero::Error_Nil)
            { g_KieroBackend = 2; g_KieroD3D9  = d3d9;  DEBUG_LOG("MainThread: backend=D3D9"); }
        else if (kiero::locate<kiero::Implementation_OpenGL>(nullptr, &gl) == kiero::Error_Nil)
            { g_KieroBackend = 3; g_KieroOpenGL  = gl;   DEBUG_LOG("MainThread: backend=OpenGL"); }
        else if (kiero::locate<kiero::Implementation_Vulkan>(nullptr, &vk) == kiero::Error_Nil)
            { g_KieroBackend = 4; g_KieroVulkan  = vk;   DEBUG_LOG("MainThread: backend=Vulkan"); }
        else {
            DEBUG_LOG("MainThread: no backend detected!");
            return 1;
        }
    }
    DEBUG_LOG("MainThread: backend=%d (0=D3D12 1=D3D11 2=D3D9 3=GL 4=VK)", g_KieroBackend);
    if (g_KieroBackend == 0) {
        void* t = D3D12_CQ(10);
        DEBUG_LOG("MainThread: D3D12 CQ[10]=%p (ExecuteCommandLists)", t);
        if (t) { MH_CreateHook(t, (LPVOID)hkExecuteCommandLists, (void**)&oExecuteCommandLists); MH_EnableHook(t); }
    }
    void* prTarget = g_KieroBackend == 0 ? D3D12_SWAP(8) : g_KieroBackend == 1 ? D3D11_SWAP(8) : D3D9_DEV(17);
    DEBUG_LOG("MainThread: Present target=%p, installing hook...", prTarget);
    if (prTarget) { MH_CreateHook(prTarget, (LPVOID)hkPresent, (void**)&oPresent); MH_EnableHook(prTarget); }
    DEBUG_LOG("MainThread: hooks installed, oPresent=%p", oPresent);
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl Init(ImGuiInitCallbackFn ic, ImGuiShutdownCallbackFn sc, ImGuiRenderCallbackFn rc) {
    DEBUG_LOG("Init: bridge mode, init_cb=%p shutdown_cb=%p render_cb=%p", ic, sc, rc);
    imgui_callbacks = { ic, sc, rc }; g_BridgeMode = true;
    CreateThread(nullptr, 0, MainThread, nullptr, 0, nullptr);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE h, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(h); g_Module = h;
        GetModuleFileNameA(h, dlldir, 512);
        for (size_t i = strlen(dlldir); i > 0; i--) { if (dlldir[i] == '\\') { dlldir[i+1] = 0; break; } }
        DEBUG_LOG("DllMain: ATTACH module=%p dir=%s", h, dlldir);
    } else if (r == DLL_PROCESS_DETACH) {
        DEBUG_LOG("DllMain: DETACH, shutting down");
        if (g_BridgeMode && imgui_callbacks.shutdown_callback) imgui_callbacks.shutdown_callback();
        DisableAll(); FreeLibraryAndExitThread(h, TRUE);
    }
    return TRUE;
}
