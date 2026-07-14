using ImGuiNET;

namespace StArray.ModManager.Behaviours;

/// <summary>
/// 仿 MonoBehaviour 的基类 —— 由 <see cref="BehaviourManager"/> 驱动生命周期。
/// 继承此类即可获得 OnStart / OnUpdate / OnGUI / OnStop 回调。
/// </summary>
public abstract class GameBehaviour
{
    /// <summary>是否已调用过 OnStart</summary>
    internal bool Started { get; set; }

    /// <summary>是否已标记为销毁</summary>
    public bool IsDestroyed { get; internal set; }

    /// <summary>
    /// 行为首次激活时调用一次（类似 Unity Start）。
    /// </summary>
    public virtual void OnStart() { }

    /// <summary>
    /// 每帧调用（类似 Unity Update），在所有 ImGui 窗口绘制之前。
    /// </summary>
    /// <param name="delta">上一帧到当前帧的时间间隔（秒）</param>
    public virtual void OnUpdate(float delta) { }

    /// <summary>
    /// 每帧在 ImGui 主窗口渲染完毕后调用（类似 Unity OnGUI），可在此绘制额外 ImGui 控件。
    /// </summary>
    /// <param name="drawList">ImGui 背景绘制列表</param>
    public virtual void OnGUI(ImDrawListPtr drawList) { }

    /// <summary>
    /// 行为被销毁时调用一次（类似 Unity OnDestroy）。
    /// </summary>
    public virtual void OnStop() { }
}
