using ImGuiNET;

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
    /// 初始化 ImGui 上下文 + 加载嵌入式字体 (文泉驿正黑 + FontAwesome 7 图标)
    /// </summary>
    void InitImGui()
    {
        ImGui.SetCurrentContext(ImGui.CreateContext());
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        ImGuiFonts.Build(io.Fonts);
        ImGuiFonts.LogAtlas(io.Fonts);
    }

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
