namespace StArray.ModManager.Runtime;

/// <summary>
/// 可选接口 —— 复杂类型实现此接口可自定义在检查器中的绘制
/// </summary>
public interface IModSettingCustomDraw
{
    /// <summary>在检查器中绘制自定义 UI</summary>
    void DrawInspector();
}
