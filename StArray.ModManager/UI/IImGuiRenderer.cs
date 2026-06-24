namespace StArray.ModManager.UI;

/// <summary>
/// ImGui 渲染器接口 —— 抽象渲染管线，允许替换不同的渲染后端
/// </summary>
public interface IImGuiRenderer
{
    /// <summary>
    /// 是否已完成初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 安装 Hook 并准备渲染管线
    /// </summary>
    bool Install();

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
