namespace StArray.ModManager.Mono;

/// <summary>UnityEngine.SystemInfo — 通过 Mono 反射读取图形设备类型</summary>
public static unsafe class MonoSystemInfo
{
    /// <summary>
    /// 通过 Mono 反射调用 <c>UnityEngine.SystemInfo.get_graphicsDeviceType()</c>。
    /// 需要提前 mono_jit_init + 加载 UnityEngine.CoreModule。
    /// 返回 Unity 2020+ 的 GraphicsDeviceType 枚举值：
    ///   0 = Direct3D9, 2 = Direct3D11, 3 = Direct3D12,
    ///   11 = OpenGLCore, 13 = Vulkan
    ///   失败返回 -1。
    /// </summary>
    public static int GetGraphicsDeviceType()
    {
        // 先通过名称查找已加载的 UnityEngine.CoreModule
        var img = MonoFunctions.MonoImageLoaded("UnityEngine.CoreModule.dll");
        if (img == 0)
        {
            // 尝试从 UnityEngine.CoreModule 程序集加载
            var asm = MonoAssembly.Get("UnityEngine.CoreModule.dll");
            if (asm == null) return -1;
            img = MonoFunctions.MonoAssemblyGetImage(asm.Ptr);
            if (img == 0) return -1;
        }

        var k = MonoFunctions.MonoClassFromName(img, "UnityEngine", "SystemInfo");
        if (k == 0) return -1;

        var m = MonoFunctions.MonoClassGetMethodFromName(k, "get_graphicsDeviceType", 0);
        if (m == 0) return -1;

        nint exc = 0;
        var ret = MonoFunctions.MonoRuntimeInvoke(m, 0, null, out exc);
        if (ret == 0) return -1;

        // 返回类型是值类型（System.Int32），需要拆箱
        var unboxed = MonoFunctions.MonoObjectUnbox(ret);
        if (unboxed == 0) return -1;
        return *(int*)unboxed;
    }

    /// <summary>判断是否 D3D11</summary>
    public static bool IsD3D11 => GetGraphicsDeviceType() == 2;

    /// <summary>判断是否 D3D12</summary>
    public static bool IsD3D12 => GetGraphicsDeviceType() == 3;

    /// <summary>判断是否 D3D9</summary>
    public static bool IsD3D9 => GetGraphicsDeviceType() == 0;

    /// <summary>判断是否 OpenGLCore</summary>
    public static bool IsOpenGL => GetGraphicsDeviceType() == 11;

    /// <summary>判断是否 Vulkan</summary>
    public static bool IsVulkan => GetGraphicsDeviceType() == 13;
}
