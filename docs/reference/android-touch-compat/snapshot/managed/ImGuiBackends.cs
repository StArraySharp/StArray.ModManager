using System.Runtime.InteropServices;

namespace StArray.ModManager.Android.Native;

/// <summary>
/// ImGui Android Backend (imgui_impl_android.cpp)
/// </summary>
public static class ImGuiImplAndroid
{
    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplAndroid_Init")]
    public static extern bool Init(IntPtr window);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplAndroid_Shutdown")]
    public static extern void Shutdown();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplAndroid_NewFrame")]
    public static extern void NewFrame();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplAndroid_HandleInputEvent")]
    public static extern int HandleInputEvent(IntPtr inputEvent);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_imgui_drain_forwarded_motion_events")]
    public static extern int DrainForwardedMotionEvents();
}

/// <summary>
/// ImGui OpenGL3 Backend (imgui_impl_opengl3.cpp)
/// </summary>
public static class ImGuiImplOpenGL3
{
    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_Init")]
    public static extern bool Init(string glslVersion = "#version 300 es");

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_Shutdown")]
    public static extern void Shutdown();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_NewFrame")]
    public static extern void NewFrame();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_RenderDrawData")]
    public static extern void RenderDrawData(IntPtr drawData);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_CreateFontsTexture")]
    public static extern bool CreateFontsTexture();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_DestroyFontsTexture")]
    public static extern void DestroyFontsTexture();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_CreateDeviceObjects")]
    public static extern bool CreateDeviceObjects();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplOpenGL3_DestroyDeviceObjects")]
    public static extern void DestroyDeviceObjects();
}

/// <summary>
/// ImGui Vulkan Backend (imgui_impl_vulkan.cpp)
/// </summary>
public static class ImGuiImplVulkan
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InitInfo
    {
        public IntPtr Instance;
        public IntPtr PhysicalDevice;
        public IntPtr Device;
        public uint QueueFamily;
        public IntPtr Queue;
        public IntPtr DescriptorPool;
        public IntPtr RenderPass;
        public uint MinImageCount;
        public uint ImageCount;
        public int MSAASamples;
        public IntPtr Allocator;
        public IntPtr CheckVkResultFn;
        public uint MinAllocationSize;
    }

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_Init")]
    public static extern bool Init(ref InitInfo info);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_Shutdown")]
    public static extern void Shutdown();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_NewFrame")]
    public static extern void NewFrame();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_RenderDrawData")]
    public static extern void RenderDrawData(IntPtr drawData, IntPtr commandBuffer, IntPtr pipeline);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_CreateFontsTexture")]
    public static extern bool CreateFontsTexture();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_DestroyFontsTexture")]
    public static extern void DestroyFontsTexture();

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_SetMinImageCount")]
    public static extern void SetMinImageCount(uint minImageCount);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_AddTexture")]
    public static extern IntPtr AddTexture(IntPtr sampler, IntPtr imageView, uint imageLayout);

    [DllImport("starray_modmanager", EntryPoint = "ImGui_ImplVulkan_RemoveTexture")]
    public static extern void RemoveTexture(IntPtr descriptorSet);
}
