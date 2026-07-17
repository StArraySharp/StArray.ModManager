namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>
/// 统一后端图形设备检测 — 通过反射调用 UnityEngine.SystemInfo.get_graphicsDeviceType。
/// Unity 2020+ GraphicsDeviceType 枚举值：
///   0 = Direct3D9, 2 = Direct3D11, 3 = Direct3D12,
///   11 = OpenGLCore, 13 = Vulkan
/// </summary>
public static class GraphicsDevice
{
    /// <summary>获取图形设备类型。失败返回 -1。</summary>
    public static int GetGraphicsDeviceType()
    {
        var sysInfo = RuntimeObject.New("UnityEngine", "UnityEngine", "SystemInfo");
        if (sysInfo == null) return -1;
        return sysInfo.Value.InvokeUnbox<int>("get_graphicsDeviceType", 0);
    }

    public static bool IsD3D9 => GetGraphicsDeviceType() == 0;
    public static bool IsD3D11 => GetGraphicsDeviceType() == 2;
    public static bool IsD3D12 => GetGraphicsDeviceType() == 3;
    public static bool IsOpenGL => GetGraphicsDeviceType() == 11;
    public static bool IsVulkan => GetGraphicsDeviceType() == 13;
}
