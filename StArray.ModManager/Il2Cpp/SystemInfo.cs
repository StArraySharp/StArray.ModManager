namespace StArray.ModManager.Il2Cpp;

/// <summary>UnityEngine.SystemInfo — 图形设备类型检测</summary>
public static class UnitySystemInfo
{
    /// <summary>
    /// 通过 <c>SystemInfo.get_graphicsDeviceType()</c> 获取当前渲染器。
    /// 支持 Unity 2020+ 的 GraphicsDeviceType 枚举值。
    /// </summary>
    public static int GetGraphicsDeviceType()
    {
        var k = Il2CppAssembly.Get("UnityEngine.CoreModule.dll")?.GetClass("UnityEngine", "SystemInfo");
        var m = k?.GetMethod("get_graphicsDeviceType", 0);
        if (m == null) return -1;
        return m.InvokeUnbox<int>(0);
    }

    /// <summary>判断是否 D3D11（值为 2）</summary>
    public static bool IsD3D11 => GetGraphicsDeviceType() == 2;

    /// <summary>判断是否 D3D12（值为 3）</summary>
    public static bool IsD3D12 => GetGraphicsDeviceType() == 3;

    /// <summary>判断是否 D3D9（值为 0）</summary>
    public static bool IsD3D9 => GetGraphicsDeviceType() == 0;

    /// <summary>判断是否 OpenGLCore（值为 11）</summary>
    public static bool IsOpenGL => GetGraphicsDeviceType() == 11;

    /// <summary>判断是否 Vulkan（值为 13）</summary>
    public static bool IsVulkan => GetGraphicsDeviceType() == 13;
}
