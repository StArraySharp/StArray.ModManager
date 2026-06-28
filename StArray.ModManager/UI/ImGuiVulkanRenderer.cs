using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Native;
using StArray.ModManager.Manager;

namespace StArray.ModManager.UI;

/// <summary>
/// ImGui Vulkan 渲染器 —— Hook vkQueuePresentKHR 驱动渲染管线
/// 输入处理委托给 <see cref="ImGuiInputHandler"/>
/// </summary>
public sealed unsafe class ImGuiVulkanRenderer : IImGuiRenderer
{
    // ===== 静态单例 =====

    private static ImGuiVulkanRenderer? s_instance;

    /// <summary>渲染器单例（Install 之后可用）</summary>
    public static ImGuiVulkanRenderer Instance =>
        s_instance ?? throw new InvalidOperationException("Vulkan renderer not installed");

    /// <summary>安装 Vulkan 渲染器</summary>
    public static bool Install() => (s_instance = new ImGuiVulkanRenderer()).InstallInstance();

    private static Action? s_pendingOnRender;

    /// <summary>每帧渲染事件</summary>
    public static event Action OnRender
    {
        add
        {
            if (s_instance != null) s_instance._onRender += value;
            else s_pendingOnRender += value;
        }
        remove
        {
            if (s_instance != null) s_instance._onRender -= value;
            else s_pendingOnRender -= value;
        }
    }

    // ===== IImGuiRenderer =====

    private bool _initialized;
    private Action _onRender = () => { };

    event Action IImGuiRenderer.OnRender
    {
        add => _onRender += value;
        remove => _onRender -= value;
    }

    /// <summary>渲染器是否已初始化</summary>
    public bool IsInitialized => _initialized;

    bool IImGuiRenderer.Install() => InstallInstance();

    // ===== Vulkan 句柄（从 Hook 捕获） =====

    private IntPtr _instance;          // VkInstance
    private IntPtr _physicalDevice;    // VkPhysicalDevice
    private IntPtr _device;            // VkDevice
    private IntPtr _queue;             // VkQueue（图形队列）
    private uint _queueFamily;         // 队列族索引
    private uint _minImageCount = 2;   // swapchain 最少图像数

    private IntPtr _commandPool;       // 自建 VkCommandPool
    private IntPtr _commandBuffer;     // 自建 VkCommandBuffer（每帧复用）

    // 交换链尺寸
    private int _fbWidth, _fbHeight;

    // ===== Hook 原函数委托 =====

    private VkCreateInstanceDelegate? _prevCreateInstance;
    private VkCreateDeviceDelegate? _prevCreateDevice;
    private VkGetDeviceQueueDelegate? _prevGetDeviceQueue;
    private VkQueuePresentKHRDelegate? _prevQueuePresentKHR;

    // Vulkan 函数签名
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkCreateInstanceDelegate(IntPtr pCreateInfo, IntPtr pAllocator, out IntPtr pInstance);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkCreateDeviceDelegate(IntPtr physicalDevice, IntPtr pCreateInfo,
        IntPtr pAllocator, out IntPtr pDevice);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void VkGetDeviceQueueDelegate(IntPtr device, uint queueFamilyIndex,
        uint queueIndex, out IntPtr pQueue);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkQueuePresentKHRDelegate(IntPtr queue, IntPtr pPresentInfo);

    // ===== 自用 Vulkan 函数（通过 dlsym 加载） =====

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkCreateCommandPoolDelegate(IntPtr device, IntPtr pCreateInfo,
        IntPtr pAllocator, out IntPtr pCommandPool);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkAllocateCommandBuffersDelegate(IntPtr device, IntPtr pAllocateInfo,
        out IntPtr pCommandBuffers);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkBeginCommandBufferDelegate(IntPtr commandBuffer, IntPtr pBeginInfo);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkEndCommandBufferDelegate(IntPtr commandBuffer);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkQueueSubmitDelegate(IntPtr queue, uint submitCount,
        IntPtr pSubmits, IntPtr fence);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkQueueWaitIdleDelegate(IntPtr queue);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkResetCommandPoolDelegate(IntPtr device, IntPtr commandPool, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkCreateFenceDelegate(IntPtr device, IntPtr pCreateInfo,
        IntPtr pAllocator, out IntPtr pFence);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkWaitForFencesDelegate(IntPtr device, uint fenceCount,
        IntPtr pFences, int waitAll, ulong timeout);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkResetFencesDelegate(IntPtr device, uint fenceCount, IntPtr pFences);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void VkDestroyFenceDelegate(IntPtr device, IntPtr fence, IntPtr pAllocator);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VkGetPhysicalDeviceSurfaceCapabilitiesKHRDelegate(
        IntPtr physicalDevice, IntPtr surface, IntPtr pSurfaceCapabilities);

    // 已加载的自用函数指针
    private IntPtr _pfnCreateCommandPool;
    private IntPtr _pfnAllocateCommandBuffers;
    private IntPtr _pfnBeginCommandBuffer;
    private IntPtr _pfnEndCommandBuffer;
    private IntPtr _pfnQueueSubmit;
    private IntPtr _pfnQueueWaitIdle;
    private IntPtr _pfnResetCommandPool;
    private IntPtr _pfnCreateFence;
    private IntPtr _pfnWaitForFences;
    private IntPtr _pfnResetFences;
    private IntPtr _pfnDestroyFence;

    private IntPtr _renderFence;       // 每帧渲染完成同步栅栏

    // ===== 安装 =====

    private bool InstallInstance()
    {
        // 加载 libvulkan.so
        var vulkanLib = DL.dlopen("libvulkan.so", DL.Flags.RTLD_GLOBAL | DL.Flags.RTLD_NOW);
        if (vulkanLib == IntPtr.Zero)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), "Failed to load libvulkan.so");
            return false;
        }

        // 解析 Vulkan 函数地址
        if (!ResolveVulkanFunctions(vulkanLib))
            return false;

        // Hook vkCreateInstance — 捕获 VkInstance
        var addrCreateInstance = Dobby.SymbolResolver("libvulkan.so", "vkCreateInstance");
        if (addrCreateInstance != IntPtr.Zero)
        {
            Dobby.Hook(addrCreateInstance,
                typeof(ImGuiVulkanRenderer).GetMethod(nameof(OnCreateInstance))!
                    .MethodHandle.GetFunctionPointer(),
                out var orig);
            _prevCreateInstance = Marshal.GetDelegateForFunctionPointer<VkCreateInstanceDelegate>(orig);
            Logger.Info(nameof(ImGuiVulkanRenderer), "Hooked vkCreateInstance");
        }

        // Hook vkCreateDevice — 捕获 physicalDevice + device
        var addrCreateDevice = Dobby.SymbolResolver("libvulkan.so", "vkCreateDevice");
        if (addrCreateDevice != IntPtr.Zero)
        {
            Dobby.Hook(addrCreateDevice,
                typeof(ImGuiVulkanRenderer).GetMethod(nameof(OnCreateDevice))!
                    .MethodHandle.GetFunctionPointer(),
                out var orig);
            _prevCreateDevice = Marshal.GetDelegateForFunctionPointer<VkCreateDeviceDelegate>(orig);
            Logger.Info(nameof(ImGuiVulkanRenderer), "Hooked vkCreateDevice");
        }

        // Hook vkGetDeviceQueue — 捕获 queue + queueFamily
        var addrGetDeviceQueue = Dobby.SymbolResolver("libvulkan.so", "vkGetDeviceQueue");
        if (addrGetDeviceQueue != IntPtr.Zero)
        {
            Dobby.Hook(addrGetDeviceQueue,
                typeof(ImGuiVulkanRenderer).GetMethod(nameof(OnGetDeviceQueue))!
                    .MethodHandle.GetFunctionPointer(),
                out var orig);
            _prevGetDeviceQueue = Marshal.GetDelegateForFunctionPointer<VkGetDeviceQueueDelegate>(orig);
            Logger.Info(nameof(ImGuiVulkanRenderer), "Hooked vkGetDeviceQueue");
        }

        // Hook vkQueuePresentKHR — 渲染帧触发器
        var addrQueuePresentKHR = Dobby.SymbolResolver("libvulkan.so", "vkQueuePresentKHR");
        if (addrQueuePresentKHR == IntPtr.Zero)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), "vkQueuePresentKHR not found in libvulkan.so");
            return false;
        }
        Dobby.Hook(addrQueuePresentKHR,
            typeof(ImGuiVulkanRenderer).GetMethod(nameof(OnQueuePresentKHR))!
                .MethodHandle.GetFunctionPointer(),
            out var origPresent);
        _prevQueuePresentKHR =
            Marshal.GetDelegateForFunctionPointer<VkQueuePresentKHRDelegate>(origPresent);

        // 输入 Hook
        ImGuiInputHandler.InstallHooks();

        // 回放缓存的静态事件订阅
        if (s_pendingOnRender != null)
        {
            _onRender += s_pendingOnRender;
            s_pendingOnRender = null;
        }

        Logger.Error(nameof(ImGuiVulkanRenderer),
            $"Vulkan hooks installed (instance:{addrCreateInstance:X} device:{addrCreateDevice:X} " +
            $"queue:{addrGetDeviceQueue:X} present:{addrQueuePresentKHR:X})");
        return true;
    }

    private bool ResolveVulkanFunctions(IntPtr vulkanLib)
    {
        _pfnCreateCommandPool = DL.dlsym(vulkanLib, "vkCreateCommandPool");
        _pfnAllocateCommandBuffers = DL.dlsym(vulkanLib, "vkAllocateCommandBuffers");
        _pfnBeginCommandBuffer = DL.dlsym(vulkanLib, "vkBeginCommandBuffer");
        _pfnEndCommandBuffer = DL.dlsym(vulkanLib, "vkEndCommandBuffer");
        _pfnQueueSubmit = DL.dlsym(vulkanLib, "vkQueueSubmit");
        _pfnQueueWaitIdle = DL.dlsym(vulkanLib, "vkQueueWaitIdle");
        _pfnResetCommandPool = DL.dlsym(vulkanLib, "vkResetCommandPool");
        _pfnCreateFence = DL.dlsym(vulkanLib, "vkCreateFence");
        _pfnWaitForFences = DL.dlsym(vulkanLib, "vkWaitForFences");
        _pfnResetFences = DL.dlsym(vulkanLib, "vkResetFences");
        _pfnDestroyFence = DL.dlsym(vulkanLib, "vkDestroyFence");

        if (_pfnCreateCommandPool == IntPtr.Zero ||
            _pfnAllocateCommandBuffers == IntPtr.Zero ||
            _pfnBeginCommandBuffer == IntPtr.Zero ||
            _pfnEndCommandBuffer == IntPtr.Zero ||
            _pfnQueueSubmit == IntPtr.Zero)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), "Failed to resolve essential Vulkan functions");
            return false;
        }

        Logger.Info(nameof(ImGuiVulkanRenderer), "Vulkan utility functions resolved");
        return true;
    }

    // ===== Hook 回调 (UnmanagedCallersOnly) =====

    /// <summary>vkCreateInstance Hook 回调</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnCreateInstance(IntPtr pCreateInfo, IntPtr pAllocator, IntPtr* pInstance)
    {
        var self = s_instance!;
        int result = self._prevCreateInstance!(pCreateInfo, pAllocator, out IntPtr instance);
        *pInstance = instance;

        if (result == 0 /* VK_SUCCESS */ && self._instance == IntPtr.Zero)
        {
            self._instance = instance;
            Logger.Info(nameof(ImGuiVulkanRenderer), $"Captured VkInstance: 0x{instance:X}");
        }

        return result;
    }

    /// <summary>vkCreateDevice Hook 回调</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnCreateDevice(IntPtr physicalDevice, IntPtr pCreateInfo,
        IntPtr pAllocator, IntPtr* pDevice)
    {
        var self = s_instance!;
        self._physicalDevice = physicalDevice;

        int result = self._prevCreateDevice!(physicalDevice, pCreateInfo, pAllocator, out IntPtr device);
        *pDevice = device;

        if (result == 0 /* VK_SUCCESS */)
        {
            self._device = device;
            Logger.Info(nameof(ImGuiVulkanRenderer),
                $"Captured VkDevice: 0x{device:X} (PhysicalDevice: 0x{physicalDevice:X})");
        }

        return result;
    }

    /// <summary>vkGetDeviceQueue Hook 回调</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void OnGetDeviceQueue(IntPtr device, uint queueFamilyIndex,
        uint queueIndex, IntPtr* pQueue)
    {
        var self = s_instance!;
        self._prevGetDeviceQueue!(device, queueFamilyIndex, queueIndex, out IntPtr queue);
        *pQueue = queue;

        // 只捕获第一个图形队列（queueIndex == 0 且尚未捕获）
        if (self._queue == IntPtr.Zero && queueIndex == 0)
        {
            self._queue = queue;
            self._queueFamily = queueFamilyIndex;
            Logger.Info(nameof(ImGuiVulkanRenderer),
                $"Captured VkQueue: 0x{queue:X} (family={queueFamilyIndex})");
        }
    }

    /// <summary>vkQueuePresentKHR Hook 回调</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static int OnQueuePresentKHR(IntPtr queue, IntPtr pPresentInfo)
    {
        var self = s_instance!;
        try
        {
            // 首次调用：初始化 ImGui Vulkan 后端
            if (!self._initialized)
            {
                self.InitImGuiVulkan(queue);
            }

            // 同步上一帧的渲染栅栏
            self.WaitForRenderFence();

            // 获取显示尺寸（从 present info 中提取 swapchain 尺寸，或使用缓存）
            self.UpdateDisplaySize(pPresentInfo);

            // === 渲染帧 ===
            ImGuiImplVulkan.NewFrame();
            ImGuiImplAndroid.NewFrame();

            var io = ImGui.GetIO();
            io.DisplaySize = new Vector2(self._fbWidth, self._fbHeight);

            ImGui.NewFrame();
            self.BuildUI();
            ImGuiInputHandler.UpdateIme();
            ImGui.Render();

            // 录制 Vulkan 命令
            self.SubmitImGuiDrawCommands();

        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), $"OnQueuePresentKHR error: {ex}");
        }

        return self._prevQueuePresentKHR!(queue, pPresentInfo);
    }

    // ===== ImGui Vulkan 初始化 =====

    private void InitImGuiVulkan(IntPtr presentQueue)
    {
        if (_initialized) return;
        if (_device == IntPtr.Zero || _physicalDevice == IntPtr.Zero)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer),
                "Vulkan device handles not yet captured — deferring init");
            return;
        }

        // 如果 vkGetDeviceQueue 未捕获到 queue，使用 presentQueue
        if (_queue == IntPtr.Zero)
        {
            _queue = presentQueue;
            Logger.Warn(nameof(ImGuiVulkanRenderer),
                $"Using present queue as render queue: 0x{presentQueue:X}");
        }

        Logger.Error(nameof(ImGuiVulkanRenderer),
            $"Initializing ImGui Vulkan backend... Device=0x{_device:X} " +
            $"PhysicalDevice=0x{_physicalDevice:X} Queue=0x{_queue:X} Family={_queueFamily}");

        // 创建 ImGui 上下文
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.FontGlobalScale = 3.0f;

        // 加载中文字体
        LoadCJKFont(io);

        // 加载 FontAwesome 嵌入式资源字体
        LoadEmbeddedFontAwesome(io);

        // 设置样式
        var style = ImGui.GetStyle();
        style.ScaleAllSizes(2.0f);
        ImGui.StyleColorsClassic();

        // 创建命令池
        if (!CreateCommandPool())
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), "Failed to create Vulkan command pool");
            return;
        }

        // 创建渲染栅栏
        var createFence = Marshal.GetDelegateForFunctionPointer<VkCreateFenceDelegate>(_pfnCreateFence);
        createFence(_device, IntPtr.Zero, IntPtr.Zero, out _renderFence);

        // 初始化 Vulkan backend
        var initInfo = new ImGuiImplVulkan.InitInfo
        {
            Instance = _instance,
            PhysicalDevice = _physicalDevice,
            Device = _device,
            QueueFamily = _queueFamily,
            Queue = _queue,
            DescriptorPool = IntPtr.Zero,     // backend 自行创建
            RenderPass = IntPtr.Zero,          // 无需外部 render pass
            MinImageCount = _minImageCount,
            ImageCount = _minImageCount,
            MSAASamples = 1,                   // VK_SAMPLE_COUNT_1_BIT
            Allocator = IntPtr.Zero,
            CheckVkResultFn = IntPtr.Zero,
            MinAllocationSize = 1024 * 1024    // 1 MB
        };

        if (!ImGuiImplVulkan.Init(ref initInfo))
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), "ImGui_ImplVulkan_Init failed");
            return;
        }

        // 创建字体纹理
        ImGuiImplVulkan.CreateFontsTexture();

        // 初始化 Android 输入 backend
        var nativeWindow = AndroidUtils.GetUnityNativeWindow();
        if (nativeWindow != IntPtr.Zero)
        {
            ImGuiImplAndroid.Init(nativeWindow);
            Logger.Error(nameof(ImGuiVulkanRenderer),
                $"ImGui_ImplAndroid_Init success: 0x{nativeWindow:X}");
        }

        _initialized = true;
        Logger.Error(nameof(ImGuiVulkanRenderer),
            "ImGui Vulkan backend initialized successfully");
    }

    // ===== 命令池 & 命令缓冲 =====

    private bool CreateCommandPool()
    {
        // VkCommandPoolCreateInfo
        // sType=VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO(0)
        // flags=VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT(1)
        // queueFamilyIndex=_queueFamily
        Span<int> poolCi = [0, 1, (int)_queueFamily];
        fixed (int* pCi = poolCi)
        {
            var createPool = Marshal.GetDelegateForFunctionPointer<VkCreateCommandPoolDelegate>(
                _pfnCreateCommandPool);
            int result = createPool(_device, (IntPtr)pCi, IntPtr.Zero, out _commandPool);
            if (result != 0)
            {
                Logger.Error(nameof(ImGuiVulkanRenderer),
                    $"vkCreateCommandPool failed: {result}");
                return false;
            }
        }

        // 分配一个命令缓冲 (VkCommandBufferAllocateInfo)
        Span<IntPtr> allocInfo = stackalloc IntPtr[4];
        allocInfo[0] = 0;         // sType
        allocInfo[1] = IntPtr.Zero;        // pNext
        allocInfo[2] = _commandPool;       // commandPool
        allocInfo[3] = 1;          // commandBufferCount = VK_COMMAND_BUFFER_LEVEL_PRIMARY(0) | 1<<16

        var allocateBuffers = Marshal.GetDelegateForFunctionPointer<VkAllocateCommandBuffersDelegate>(
            _pfnAllocateCommandBuffers);
        int allocResult = allocateBuffers(_device,
            (IntPtr)Unsafe.AsPointer(ref allocInfo[0]), out _commandBuffer);
        if (allocResult != 0)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer),
                $"vkAllocateCommandBuffers failed: {allocResult}");
            return false;
        }

        Logger.Info(nameof(ImGuiVulkanRenderer),
            $"Command pool & buffer created (pool=0x{_commandPool:X} buf=0x{_commandBuffer:X})");
        return true;
    }

    private void SubmitImGuiDrawCommands()
    {
        if (_commandBuffer == IntPtr.Zero) return;

        var drawData = ImGui.GetDrawData();
        if (drawData.NativePtr == IntPtr.Zero.ToPointer()) return;

        // 重置命令池
        var resetPool = Marshal.GetDelegateForFunctionPointer<VkResetCommandPoolDelegate>(
            _pfnResetCommandPool);
        resetPool(_device, _commandPool, 0);

        // 开始录制命令缓冲
        // VkCommandBufferBeginInfo: sType=42, flags=0
        Span<int> beginInfo = [42, 0, 0];
        fixed (int* pBi = beginInfo)
        {
            var begin = Marshal.GetDelegateForFunctionPointer<VkBeginCommandBufferDelegate>(
                _pfnBeginCommandBuffer);
            int br = begin(_commandBuffer, (IntPtr)pBi);
            if (br != 0)
            {
                Logger.Error(nameof(ImGuiVulkanRenderer),
                    $"vkBeginCommandBuffer failed: {br}");
                return;
            }
        }

        // 录制 ImGui 渲染命令
        ImGuiImplVulkan.RenderDrawData((IntPtr)drawData.NativePtr, _commandBuffer, IntPtr.Zero);

        // 结束录制
        var end = Marshal.GetDelegateForFunctionPointer<VkEndCommandBufferDelegate>(
            _pfnEndCommandBuffer);
        int er = end(_commandBuffer);
        if (er != 0)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer),
                $"vkEndCommandBuffer failed: {er}");
            return;
        }

        // 提交到队列（带栅栏同步）
        // VkSubmitInfo: sType=4, wait/信号量=0, commandBufferCount=1, pCommandBuffers=&cmdBuf
        Span<IntPtr> submitInfo = stackalloc IntPtr[8];
        submitInfo[0] = 4;            // sType = VK_STRUCTURE_TYPE_SUBMIT_INFO
        submitInfo[1] = IntPtr.Zero;           // pNext
        submitInfo[2] = IntPtr.Zero;           // waitSemaphoreCount = 0
        submitInfo[3] = IntPtr.Zero;           // pWaitSemaphores
        submitInfo[4] = IntPtr.Zero;           // pWaitDstStageMask
        submitInfo[5] = 1;             // commandBufferCount = 1
        var cmdBuf = _commandBuffer;           // 先拷贝到局部变量才能取地址
        submitInfo[6] = (IntPtr)(&cmdBuf);     // pCommandBuffers
        submitInfo[7] = IntPtr.Zero;           // signalSemaphoreCount = 0

        var submit = Marshal.GetDelegateForFunctionPointer<VkQueueSubmitDelegate>(
            _pfnQueueSubmit);
        int sr = submit(_queue, 1,
            (IntPtr)Unsafe.AsPointer(ref submitInfo[0]), _renderFence);
        if (sr != 0)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer),
                $"vkQueueSubmit failed: {sr}");
        }
    }

    private void WaitForRenderFence()
    {
        if (_renderFence == IntPtr.Zero) return;

        var wait = Marshal.GetDelegateForFunctionPointer<VkWaitForFencesDelegate>(
            _pfnWaitForFences);
        var reset = Marshal.GetDelegateForFunctionPointer<VkResetFencesDelegate>(
            _pfnResetFences);

        // 非阻塞检查：超时 0 表示不等待
        int result = wait(_device, 1, _renderFence, 1 /* waitAll */, 0);
        if (result != 0 /* VK_SUCCESS */)
            return; // 上一帧还未完成，跳过（防止管线堆积）

        reset(_device, 1, _renderFence);
    }

    private void UpdateDisplaySize(IntPtr pPresentInfo)
    {
        // VkPresentInfoKHR 结构：
        // sType, pNext, waitSemaphoreCount, pWaitSemaphores,
        // swapchainCount, pSwapchains, pImageIndices, pResults
        // swapchainCount 在偏移 4*IntPtr 处，pSwapchains 在偏移 5*IntPtr 处

        // 使用 swapchain 查询 surface 能力来获取尺寸（如果 surface 可用）
        if (_fbWidth > 0 && _fbHeight > 0) return; // 已有缓存尺寸

        // 默认值（Unity Android 典型分辨率）
        _fbWidth = 1080;
        _fbHeight = 2400;
    }

    // ===== 字体加载 =====

    private static void LoadCJKFont(ImGuiIOPtr io)
    {
        string[] cjkPaths = ["/system/fonts/NotoSansCJK-Regular.ttc"];
        bool loaded = false;
        foreach (var path in cjkPaths)
        {
            if (File.Exists(path))
            {
                var range = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();
                io.Fonts.AddFontFromFileTTF(path, 16.0f, null, range);
                Logger.Info(nameof(ImGuiVulkanRenderer), $"CJK font: {path}");
                loaded = true;
                break;
            }
        }
        if (!loaded)
            io.Fonts.AddFontDefault();
    }

    private static void LoadEmbeddedFontAwesome(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(ImGuiVulkanRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream("StArray.ModManager.Resources.fa-solid-900.ttf");
            if (stream == null) return;

            var data = new byte[stream.Length];
            stream.ReadExactly(data);

            var ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);

            io.Fonts.AddFontFromMemoryTTF(ptr, data.Length, 16.0f, IntPtr.Zero,
                io.Fonts.GetGlyphRangesDefault());
            io.Fonts.Build();
            Logger.Info(nameof(ImGuiVulkanRenderer), $"FA font loaded ({data.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ImGuiVulkanRenderer), $"FA font: {ex.Message}");
        }
    }

    private void BuildUI()
    {
        _onRender?.Invoke();
    }

    // ===== 清理 =====

    ~ImGuiVulkanRenderer()
    {
        if (_renderFence != IntPtr.Zero && _pfnDestroyFence != IntPtr.Zero)
        {
            var destroy = Marshal.GetDelegateForFunctionPointer<VkDestroyFenceDelegate>(_pfnDestroyFence);
            destroy(_device, _renderFence, IntPtr.Zero);
        }
    }
}
