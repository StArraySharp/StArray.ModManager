using ImGuiNET;

namespace StArray.ModLoader.Manager;

/// <summary>
/// 平台输入提供者接口
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// 更新 ImGui IO 的输入状态
    /// </summary>
    void UpdateInput(ImGuiIOPtr io);
}
