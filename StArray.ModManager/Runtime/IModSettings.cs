namespace StArray.ModManager.Runtime;

/// <summary>
/// 可选接口 —— Mod 实现此接口可提供自定义 ImGui 设置面板
/// </summary>
public interface IModSettings
{
    void OnGui();
}
