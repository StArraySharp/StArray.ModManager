using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>
/// 统一后端图形设备检测 — 自动选择 Mono/Il2Cpp 路径。
/// Unity 2020+ GraphicsDeviceType 枚举值：
///   0 = Direct3D9, 2 = Direct3D11, 3 = Direct3D12,
///   11 = OpenGLCore, 13 = Vulkan
/// </summary>
public static class GraphicsDevice
{
    /// <summary>获取图形设备类型。失败返回 -1。</summary>
    public static int GetGraphicsDeviceType()
    {
        return RuntimeManager.Backend switch
        {
            RuntimeBackend.Il2Cpp => UnitySystemInfo.GetGraphicsDeviceType(),
            RuntimeBackend.Mono => MonoSystemInfo.GetGraphicsDeviceType(),
            _ => -1,
        };
    }

    public static bool IsD3D9 => GetGraphicsDeviceType() == 0;
    public static bool IsD3D11 => GetGraphicsDeviceType() == 2;
    public static bool IsD3D12 => GetGraphicsDeviceType() == 3;
    public static bool IsOpenGL => GetGraphicsDeviceType() == 11;
    public static bool IsVulkan => GetGraphicsDeviceType() == 13;
}
