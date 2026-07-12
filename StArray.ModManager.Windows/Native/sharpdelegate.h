extern "C" {
    typedef void* (__cdecl* ImGuiInitCallbackFn)(void);
    typedef void (__cdecl* ImGuiShutdownCallbackFn)(void);
    typedef void (__cdecl* ImGuiRenderCallbackFn)(void);

    struct ImGuiCallbacks
    {
        ImGuiInitCallbackFn init_callback;
        ImGuiShutdownCallbackFn shutdown_callback;
        ImGuiRenderCallbackFn render_callback;
    };

    extern ImGuiCallbacks imgui_callbacks;

    __declspec(dllexport) int __cdecl Init(
        ImGuiInitCallbackFn init_callback,
        ImGuiShutdownCallbackFn shutdown_callback,
        ImGuiRenderCallbackFn render_callback);
}
