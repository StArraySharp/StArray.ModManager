/////////////////////
// D3D12/D3D11 HOOK ImGui (kiero2)
/////////////////////

#include "main.h"
#include "sharpdelegate.h"

ImGuiCallbacks imgui_callbacks = {};
bool g_BridgeMode = false;

typedef HRESULT(APIENTRY* PresentFn)(IDXGISwapChain*, UINT, UINT);
static PresentFn oPresent = nullptr;
typedef void(APIENTRY* ExecuteCommandListsFn)(ID3D12CommandQueue*, UINT, ID3D12CommandList*);
static ExecuteCommandListsFn oExecuteCommandLists = nullptr;

// D3D11 fallback (only if d3d11.h available)
#ifdef __d3d11_h__
typedef void(APIENTRY* D3D11DrawIndexedFn)(ID3D11DeviceContext*, UINT, UINT, INT);
static D3D11DrawIndexedFn oD3D11DrawIndexed = nullptr;
#endif

bool ShowMenu = false;
bool ImGui_Initialised = false;

namespace Process {
    DWORD ID; HANDLE Handle; HWND Hwnd; HMODULE Module; WNDPROC WndProc;
    int WindowWidth, WindowHeight; LPCSTR Title, ClassName, Path;
}

namespace DX12 {
    ID3D12Device* Device = nullptr;
    ID3D12DescriptorHeap* RTVHeap = nullptr, *SRVHeap = nullptr;
    ID3D12GraphicsCommandList* CmdList = nullptr;
    ID3D12CommandQueue* CmdQueue = nullptr;
    struct FCtx { ID3D12CommandAllocator* Alloc; ID3D12Resource* Res; D3D12_CPU_DESCRIPTOR_HANDLE Handle; };
    UINT BufCount = 0; FCtx* Frames = nullptr;
}
namespace DX11 { ID3D11Device* Device = nullptr; ID3D11DeviceContext* Context = nullptr; }
namespace DX9  { IDirect3DDevice9* Device = nullptr; }

LRESULT APIENTRY WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam) {
    if (ImGui_ImplWin32_WndProcHandler(hwnd, uMsg, wParam, lParam))
        return true;
    return CallWindowProc(Process::WndProc, hwnd, uMsg, wParam, lParam);
}

// ---- Present Hook (D3D11 + D3D12) ----
HRESULT APIENTRY hkPresent(IDXGISwapChain3* pSwapChain, UINT SyncInterval, UINT Flags) {
    if (!ImGui_Initialised) {
        if (imgui_callbacks.init_callback == nullptr) return oPresent(pSwapChain, SyncInterval, Flags);

        if (g_KieroBackend == 2) { // D3D9
            // D3D9: get device from hook target's parent or swapchain
            IDirect3DSwapChain9* sc = nullptr;
            HRESULT hr = pSwapChain->QueryInterface(__uuidof(IDirect3DSwapChain9), (void**)&sc);
            if (SUCCEEDED(hr) && sc) {
                hr = sc->GetDevice(&DX9::Device);
                sc->Release();
            }
            if (FAILED(hr) || !DX9::Device) return oPresent(pSwapChain, SyncInterval, Flags);

            imgui_callbacks.init_callback();
            ImGui_ImplWin32_Init(Process::Hwnd);
            ImGui_ImplDX9_Init(DX9::Device);
            Process::WndProc = (WNDPROC)SetWindowLongPtr(Process::Hwnd, GWLP_WNDPROC, (__int3264)(LONG_PTR)WndProc);
            ImGui_Initialised = true;
        } else if (g_KieroBackend == 1) { // D3D11
            HRESULT hr = pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&DX11::Device);
            if (FAILED(hr)) return oPresent(pSwapChain, SyncInterval, Flags);
            DX11::Device->GetImmediateContext(&DX11::Context);

            imgui_callbacks.init_callback();
            ImGui_ImplWin32_Init(Process::Hwnd);
            ImGui_ImplDX11_Init(DX11::Device, DX11::Context);
            Process::WndProc = (WNDPROC)SetWindowLongPtr(Process::Hwnd, GWLP_WNDPROC, (__int3264)(LONG_PTR)WndProc);
            ImGui_Initialised = true;
        } else { // D3D12
            HRESULT hr = pSwapChain->GetDevice(__uuidof(ID3D12Device), (void**)&DX12::Device);
            if (FAILED(hr)) {
                IUnknown* unk = nullptr;
                if (SUCCEEDED(pSwapChain->GetDevice(IID_IUnknown, (void**)&unk)) && unk) {
                    hr = unk->QueryInterface(__uuidof(ID3D12Device), (void**)&DX12::Device);
                    unk->Release();
                }
            }
            if (FAILED(hr)) return oPresent(pSwapChain, SyncInterval, Flags);

            DXGI_SWAP_CHAIN_DESC sd; pSwapChain->GetDesc(&sd);
            DX12::BufCount = sd.BufferCount;
            DX12::Frames = new DX12::FCtx[DX12::BufCount];

            D3D12_DESCRIPTOR_HEAP_DESC srvDh = { D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, DX12::BufCount, D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE };
            if (DX12::Device->CreateDescriptorHeap(&srvDh, IID_PPV_ARGS(&DX12::SRVHeap)) != S_OK) return oPresent(pSwapChain, SyncInterval, Flags);

            ID3D12CommandAllocator* alloc;
            if (DX12::Device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&alloc)) != S_OK) return oPresent(pSwapChain, SyncInterval, Flags);
            for (UINT i = 0; i < DX12::BufCount; i++) DX12::Frames[i].Alloc = alloc;

            if (DX12::Device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, alloc, nullptr, IID_PPV_ARGS(&DX12::CmdList)) != S_OK ||
                DX12::CmdList->Close() != S_OK) return oPresent(pSwapChain, SyncInterval, Flags);

            D3D12_DESCRIPTOR_HEAP_DESC rtvDh = { D3D12_DESCRIPTOR_HEAP_TYPE_RTV, DX12::BufCount, D3D12_DESCRIPTOR_HEAP_FLAG_NONE, 1 };
            if (DX12::Device->CreateDescriptorHeap(&rtvDh, IID_PPV_ARGS(&DX12::RTVHeap)) != S_OK) return oPresent(pSwapChain, SyncInterval, Flags);

            UINT rtvSize = DX12::Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
            D3D12_CPU_DESCRIPTOR_HANDLE rtvH = DX12::RTVHeap->GetCPUDescriptorHandleForHeapStart();
            for (UINT i = 0; i < DX12::BufCount; i++) {
                ID3D12Resource* buf = nullptr; DX12::Frames[i].Handle = rtvH;
                pSwapChain->GetBuffer(i, IID_PPV_ARGS(&buf));
                DX12::Device->CreateRenderTargetView(buf, nullptr, rtvH);
                DX12::Frames[i].Res = buf; rtvH.ptr += rtvSize;
            }

            imgui_callbacks.init_callback();
            ImGui_ImplWin32_Init(Process::Hwnd);

            static int srvIdx = 0;
            ImGui_ImplDX12_InitInfo dx12Info = {};
            dx12Info.Device = DX12::Device; dx12Info.CommandQueue = DX12::CmdQueue;
            dx12Info.NumFramesInFlight = (int)DX12::BufCount; dx12Info.RTVFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
            dx12Info.SrvDescriptorHeap = DX12::SRVHeap;
            dx12Info.SrvDescriptorAllocFn = [](ImGui_ImplDX12_InitInfo* info, D3D12_CPU_DESCRIPTOR_HANDLE* cpu, D3D12_GPU_DESCRIPTOR_HANDLE* gpu) {
                UINT inc = info->Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
                D3D12_CPU_DESCRIPTOR_HANDLE c = info->SrvDescriptorHeap->GetCPUDescriptorHandleForHeapStart();
                c.ptr += inc * (++srvIdx);
                D3D12_GPU_DESCRIPTOR_HANDLE g = info->SrvDescriptorHeap->GetGPUDescriptorHandleForHeapStart();
                g.ptr += inc * srvIdx; *cpu = c; *gpu = g;
            };
            dx12Info.SrvDescriptorFreeFn = [](ImGui_ImplDX12_InitInfo*, D3D12_CPU_DESCRIPTOR_HANDLE, D3D12_GPU_DESCRIPTOR_HANDLE) {};
            ImGui_ImplDX12_Init(&dx12Info);
            ImGui_ImplDX12_CreateDeviceObjects();

            Process::WndProc = (WNDPROC)SetWindowLongPtr(Process::Hwnd, GWLP_WNDPROC, (__int3264)(LONG_PTR)WndProc);
            ImGui_Initialised = true;
        }
    }

    if (GetAsyncKeyState(VK_INSERT) & 1) ShowMenu = !ShowMenu;

    ImGuiIO* io = igGetIO();
    { DXGI_SWAP_CHAIN_DESC sd; pSwapChain->GetDesc(&sd);
      io->DisplaySize.x = (float)sd.BufferDesc.Width; io->DisplaySize.y = (float)sd.BufferDesc.Height; }
    io->MouseDrawCursor = ShowMenu;

    ImGui_ImplWin32_NewFrame();
    if (g_KieroBackend == 3 || g_KieroBackend == 4) {
        // OpenGL/Vulkan: C# render only, no GPU submit
    } else if (g_KieroBackend == 2) {
        ImGui_ImplDX9_NewFrame();
    } else if (g_KieroBackend == 1) {
        ImGui_ImplDX11_NewFrame();
    } else {
        if (!DX12::CmdQueue) return oPresent(pSwapChain, SyncInterval, Flags);
        ImGui_ImplDX12_NewFrame();
    }

    if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();

    if (g_KieroBackend >= 3) {
        // GL/VK: no GPU submit from native
    } else if (g_KieroBackend == 2) {
        ImGui_ImplDX9_RenderDrawData(igGetDrawData());
    } else if (g_KieroBackend == 1) {
        ImGui_ImplDX11_RenderDrawData(igGetDrawData());
    } else {
        auto& ctx = DX12::Frames[pSwapChain->GetCurrentBackBufferIndex()];
        ctx.Alloc->Reset();
        D3D12_RESOURCE_BARRIER bar = { D3D12_RESOURCE_BARRIER_TYPE_TRANSITION };
        bar.Transition = { ctx.Res, D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET };
        DX12::CmdList->Reset(ctx.Alloc, nullptr);
        DX12::CmdList->ResourceBarrier(1, &bar);
        DX12::CmdList->OMSetRenderTargets(1, &ctx.Handle, FALSE, nullptr);
        DX12::CmdList->SetDescriptorHeaps(1, &DX12::SRVHeap);
        ImGui_ImplDX12_RenderDrawData(igGetDrawData(), DX12::CmdList);
        bar.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
        bar.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
        DX12::CmdList->ResourceBarrier(1, &bar);
        DX12::CmdList->Close();
        DX12::CmdQueue->ExecuteCommandLists(1, reinterpret_cast<ID3D12CommandList* const*>(&DX12::CmdList));
    }
    return oPresent(pSwapChain, SyncInterval, Flags);
}

// ---- D3D12 ExecuteCommandLists Hook ----
void APIENTRY hkExecuteCommandLists(ID3D12CommandQueue* queue, UINT n, ID3D12CommandList* lists) {
    if (!DX12::CmdQueue) DX12::CmdQueue = queue;
    oExecuteCommandLists(queue, n, lists);
}

// ---- Main Thread ----
DWORD WINAPI MainThread(LPVOID) {
    while (true) {
        DWORD pid; GetWindowThreadProcessId(GetForegroundWindow(), &pid);
        if (GetCurrentProcessId() == pid) {
            Process::ID = GetCurrentProcessId();
            Process::Handle = GetCurrentProcess();
            Process::Hwnd = GetForegroundWindow();
            RECT r; GetWindowRect(Process::Hwnd, &r);
            Process::WindowWidth = r.right - r.left;
            Process::WindowHeight = r.bottom - r.top;
            char buf[MAX_PATH];
            GetWindowTextA(Process::Hwnd, buf, sizeof(buf)); Process::Title = buf;
            GetClassNameA(Process::Hwnd, buf, sizeof(buf)); Process::ClassName = buf;
            GetModuleFileNameExA(Process::Handle, NULL, buf, sizeof(buf)); Process::Path = buf;
            break;
        }
        Sleep(100);
    }
    if (!HookInit()) return 1;

    // Hook based on detected backend
    if (g_KieroBackend == 0) { // D3D12
        void* eqTarget = D3D12_CQ(10);
        if (eqTarget) { MH_CreateHook(eqTarget, (LPVOID)hkExecuteCommandLists, (void**)&oExecuteCommandLists); MH_EnableHook(eqTarget); }
    }
    // D3D11/D3D12/D3D9: hook Present
    void* prTarget = KieroSwap(8, 8, 17);
    if (prTarget) { MH_CreateHook(prTarget, (LPVOID)hkPresent, (void**)&oPresent); MH_EnableHook(prTarget); }
    return 0;
}

// ---- C# Bridge ----
extern "C" __declspec(dllexport) int __cdecl Init(ImGuiInitCallbackFn ic, ImGuiShutdownCallbackFn sc, ImGuiRenderCallbackFn rc) {
    imgui_callbacks = { ic, sc, rc }; g_BridgeMode = true;
    CreateThread(nullptr, 0, MainThread, nullptr, 0, nullptr);
    return 0;
}

// ---- DllMain ----
BOOL APIENTRY DllMain(HMODULE hMod, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hMod); Process::Module = hMod;
        GetModuleFileNameA(hMod, dlldir, 512);
        for (size_t i = strlen(dlldir); i > 0; i--) { if (dlldir[i] == '\\') { dlldir[i + 1] = 0; break; } }
    } else if (reason == DLL_PROCESS_DETACH) {
        if (g_BridgeMode && imgui_callbacks.shutdown_callback) imgui_callbacks.shutdown_callback();
        DisableAll(); FreeLibraryAndExitThread(hMod, TRUE);
    }
    return TRUE;
}
