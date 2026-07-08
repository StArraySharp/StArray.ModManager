// D3D12 hook: init + render + ExecuteCommandLists
#include "hook_common.h"

namespace DX12 {
    ID3D12Device* Device = nullptr;
    ID3D12DescriptorHeap* RTVHeap = nullptr, *SRVHeap = nullptr;
    ID3D12GraphicsCommandList* CmdList = nullptr;
    ID3D12CommandQueue* CmdQueue = nullptr;
    UINT BufCount = 0; FCtx* Frames = nullptr;

    void Init(IDXGISwapChain* sc) {
        DEBUG_LOG("D3D12::Init: starting, sc=%p", sc);
        HRESULT hr = sc->GetDevice(__uuidof(ID3D12Device), (void**)&Device);
        if (FAILED(hr)) {
            IUnknown* unk = nullptr;
            if (SUCCEEDED(sc->GetDevice(IID_IUnknown, (void**)&unk)) && unk) {
                hr = unk->QueryInterface(__uuidof(ID3D12Device), (void**)&Device); unk->Release();
            }
        }
        if (FAILED(hr)) { DEBUG_LOG("D3D12::Init: GetDevice FAILED hr=0x%08X", hr); return; }
        DXGI_SWAP_CHAIN_DESC sd; sc->GetDesc(&sd);
        BufCount = sd.BufferCount; Frames = new FCtx[BufCount];
        DEBUG_LOG("D3D12::Init: device=%p bufCount=%u wxh=%ux%u", Device, BufCount, sd.BufferDesc.Width, sd.BufferDesc.Height);

        D3D12_DESCRIPTOR_HEAP_DESC srvDh = { D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, BufCount, D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE };
        if (Device->CreateDescriptorHeap(&srvDh, IID_PPV_ARGS(&SRVHeap)) != S_OK) { DEBUG_LOG("D3D12::Init: SRV heap FAILED"); return; }
        DEBUG_LOG("D3D12::Init: SRV heap=%p", SRVHeap);

        ID3D12CommandAllocator* alloc;
        if (Device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&alloc)) != S_OK) { DEBUG_LOG("D3D12::Init: CommandAllocator FAILED"); return; }
        for (UINT i = 0; i < BufCount; i++) Frames[i].Alloc = alloc;

        if (Device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, alloc, nullptr, IID_PPV_ARGS(&CmdList)) != S_OK || CmdList->Close() != S_OK) {
            DEBUG_LOG("D3D12::Init: CommandList FAILED"); return;
        }
        DEBUG_LOG("D3D12::Init: CmdList=%p", CmdList);

        D3D12_DESCRIPTOR_HEAP_DESC rtvDh = { D3D12_DESCRIPTOR_HEAP_TYPE_RTV, BufCount, D3D12_DESCRIPTOR_HEAP_FLAG_NONE, 1 };
        if (Device->CreateDescriptorHeap(&rtvDh, IID_PPV_ARGS(&RTVHeap)) != S_OK) { DEBUG_LOG("D3D12::Init: RTV heap FAILED"); return; }
        DEBUG_LOG("D3D12::Init: RTV heap=%p", RTVHeap);

        UINT rtvSize = Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
        D3D12_CPU_DESCRIPTOR_HANDLE rtvH = RTVHeap->GetCPUDescriptorHandleForHeapStart();
        for (UINT i = 0; i < BufCount; i++) {
            ID3D12Resource* buf = nullptr; Frames[i].Handle = rtvH;
            sc->GetBuffer(i, IID_PPV_ARGS(&buf)); Device->CreateRenderTargetView(buf, nullptr, rtvH);
            Frames[i].Res = buf; rtvH.ptr += rtvSize;
        }
        DEBUG_LOG("D3D12::Init: %u RTVs created", BufCount);

        imgui_callbacks.init_callback();
        ImGui_ImplWin32_Init(g_GameWindow);

        static int srvIdx = 0;
        ImGui_ImplDX12_InitInfo info = {};
        info.Device = Device; info.CommandQueue = CmdQueue;
        info.NumFramesInFlight = (int)BufCount; info.RTVFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
        info.SrvDescriptorHeap = SRVHeap;
        info.SrvDescriptorAllocFn = [](ImGui_ImplDX12_InitInfo* i, D3D12_CPU_DESCRIPTOR_HANDLE* c, D3D12_GPU_DESCRIPTOR_HANDLE* g) {
            UINT inc = i->Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            *c = i->SrvDescriptorHeap->GetCPUDescriptorHandleForHeapStart(); c->ptr += inc * (++srvIdx);
            *g = i->SrvDescriptorHeap->GetGPUDescriptorHandleForHeapStart(); g->ptr += inc * srvIdx;
        };
        info.SrvDescriptorFreeFn = [](ImGui_ImplDX12_InitInfo*, D3D12_CPU_DESCRIPTOR_HANDLE, D3D12_GPU_DESCRIPTOR_HANDLE) {};
        ImGui_ImplDX12_Init(&info);
        ImGui_ImplDX12_CreateDeviceObjects();
        DEBUG_LOG("D3D12::Init: ImGui DX12 backend initialized, CmdQueue=%p", CmdQueue);
    }

    void Render(IDXGISwapChain* sc) {
        if (!CmdQueue) { DEBUG_LOG("D3D12::Render: CmdQueue is null, skipping"); return; }
        ImGui_ImplDX12_NewFrame();
        if (imgui_callbacks.render_callback) imgui_callbacks.render_callback();

        static UINT frameIdx = 0;
        auto& ctx = Frames[frameIdx++ % BufCount];
        ctx.Alloc->Reset();
        D3D12_RESOURCE_BARRIER bar = { D3D12_RESOURCE_BARRIER_TYPE_TRANSITION };
        bar.Transition = { ctx.Res, D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET };
        CmdList->Reset(ctx.Alloc, nullptr);
        CmdList->ResourceBarrier(1, &bar);
        CmdList->OMSetRenderTargets(1, &ctx.Handle, FALSE, nullptr);
        CmdList->SetDescriptorHeaps(1, &SRVHeap);
        ImGui_ImplDX12_RenderDrawData(igGetDrawData(), CmdList);
        bar.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
        bar.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
        CmdList->ResourceBarrier(1, &bar);
        CmdList->Close();
        CmdQueue->ExecuteCommandLists(1, reinterpret_cast<ID3D12CommandList* const*>(&CmdList));
    }
}

void APIENTRY hkExecuteCommandLists(ID3D12CommandQueue* queue, UINT n, ID3D12CommandList* lists) {
    if (!DX12::CmdQueue) { DX12::CmdQueue = queue; DEBUG_LOG("hkExecuteCommandLists: CmdQueue captured=%p", queue); }
    oExecuteCommandLists(queue, n, lists);
}
