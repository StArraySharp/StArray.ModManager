// D3D9 hook: init + render
#include "hook_common.h"

namespace DX9 {
    IDirect3DDevice9* Device = nullptr;

    void Init(IDXGISwapChain* sc) {
        DEBUG_LOG("D3D9::Init: sc=%p, querying IDirect3DSwapChain9...", sc);
        IDirect3DSwapChain9* swap = nullptr;
        HRESULT hr = sc->QueryInterface(__uuidof(IDirect3DSwapChain9), (void**)&swap);
        if (SUCCEEDED(hr) && swap) { hr = swap->GetDevice(&Device); swap->Release(); }
        if (FAILED(hr) || !Device) { DEBUG_LOG("D3D9::Init: FAILED hr=0x%08X device=%p", hr, Device); return; }
        DEBUG_LOG("D3D9::Init: device=%p OK", Device);
        imgui_callbacks.init_callback();
        ImGui_ImplWin32_Init(g_GameWindow);
        ImGui_ImplDX9_Init(Device);
        DEBUG_LOG("D3D9::Init: complete");
    }

    void Render(IDXGISwapChain*) {
        // Save viewport + scissor state (game may clip our ImGui rendering)
        D3DVIEWPORT9 savedVp;
        Device->GetViewport(&savedVp);
        DWORD savedScissor = FALSE;
        Device->GetRenderState(D3DRS_SCISSORTESTENABLE, &savedScissor);

        ImGui_ImplDX9_NewFrame();
        if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();

        // Set full viewport + disable scissor
        ImGuiIO* io = igGetIO();
        D3DVIEWPORT9 vp = { 0, 0, (DWORD)io->DisplaySize.x, (DWORD)io->DisplaySize.y, 0.f, 1.f };
        Device->SetViewport(&vp);
        Device->SetRenderState(D3DRS_SCISSORTESTENABLE, FALSE);

        ImGui_ImplDX9_RenderDrawData(igGetDrawData());

        // Restore
        Device->SetViewport(&savedVp);
        Device->SetRenderState(D3DRS_SCISSORTESTENABLE, savedScissor);
    }
}
