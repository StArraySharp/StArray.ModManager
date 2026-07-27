using ImGuiNET;

namespace StArray.ModManager.Behaviours;

/// <summary>
/// 仿 MonoBehaviour 的基类 —— 由 <see cref="BehaviourManager"/> 驱动生命周期，
/// 时机来自渲染帧（每个 Unity 逻辑帧末尾一次）。
///
/// 生命周期顺序与 Unity 对齐：
/// OnAwake → OnEnable → OnStart → (OnUpdate → OnLateUpdate → OnGUI)* → OnDisable → OnStop。
/// 每个阶段都会先对所有行为跑完，再进入下一阶段。
///
/// 回调抛出的异常按 Unity 的做法处理：记录后继续，<b>不会</b>自动禁用该行为，
/// 所以每帧都会照旧调用（也就照旧报错）。要止损请显式设置
/// <see cref="Enabled"/> = false 或调用 <see cref="BehaviourManager.Remove"/>。
/// </summary>
public abstract class GameBehaviour
{
    /// <summary>是否已调用过 OnAwake</summary>
    internal bool Awoken { get; set; }

    /// <summary>是否已调用过 OnStart</summary>
    internal bool Started { get; set; }

    /// <summary>是否已标记为销毁</summary>
    public bool IsDestroyed { get; internal set; }

    private bool _enabled = true;

    /// <summary>
    /// 对应 Unity 的 <c>enabled</c>。为 false 时不再收到
    /// OnStart / OnUpdate / OnLateUpdate / OnGUI，但仍会收到 OnStop。
    /// 切换会立即触发 OnEnable / OnDisable（与 Unity 一致）；
    /// 在 OnAwake 之前设置不触发，OnEnable 会在 OnAwake 之后统一派发。
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value || IsDestroyed) return;
            _enabled = value;
            if (!Awoken) return;
            if (value) BehaviourManager.SafeEnable(this);
            else BehaviourManager.SafeDisable(this);
        }
    }

    /// <summary>不触发副作用地读取启用状态，供调度器使用。</summary>
    internal bool EnabledRaw => _enabled;

    /// <summary>加入调度后立即调用一次（类似 Unity Awake）。</summary>
    public virtual void OnAwake() { }

    /// <summary>每次被启用时调用（类似 Unity OnEnable）。</summary>
    public virtual void OnEnable() { }

    /// <summary>首次启用后、第一次 OnUpdate 之前调用一次（类似 Unity Start）。</summary>
    public virtual void OnStart() { }

    /// <summary>每帧调用（类似 Unity Update），在所有 ImGui 窗口绘制之前。</summary>
    /// <param name="delta">上一帧到当前帧的时间间隔（秒）</param>
    public virtual void OnUpdate(float delta) { }

    /// <summary>每帧在所有行为的 OnUpdate 之后调用（类似 Unity LateUpdate）。</summary>
    /// <param name="delta">上一帧到当前帧的时间间隔（秒）</param>
    public virtual void OnLateUpdate(float delta) { }

    /// <summary>
    /// 每帧在 ImGui 主窗口渲染完毕后调用（类似 Unity OnGUI），可在此绘制额外 ImGui 控件。
    /// </summary>
    /// <param name="drawList">ImGui 背景绘制列表</param>
    public virtual void OnGUI(ImDrawListPtr drawList) { }

    /// <summary>每次被禁用时调用，销毁前也会调用一次（类似 Unity OnDisable）。</summary>
    public virtual void OnDisable() { }

    /// <summary>行为被销毁时调用一次（类似 Unity OnDestroy）。</summary>
    public virtual void OnStop() { }
}
