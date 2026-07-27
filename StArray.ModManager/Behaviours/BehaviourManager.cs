using ImGuiNET;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Behaviours;

/// <summary>
/// 管理所有 <see cref="GameBehaviour"/> 的生命周期。
/// 静态方法，无需实例化。
///
/// 用户回调一律在锁外执行 —— 它们可能反过来调用 <see cref="Add"/> / <see cref="Remove"/>。
/// 异常按 Unity 的做法处理：隔离到单个行为、记录、继续，不自动禁用。
/// </summary>
public static class BehaviourManager
{
    private static readonly List<GameBehaviour> _behaviours = new();
    private static readonly List<GameBehaviour> _pendingAdd = new();
    private static readonly List<GameBehaviour> _pendingRemove = new();
    private static readonly object _lock = new();

    /// <summary>当前活跃的行为列表（只读快照）</summary>
    public static IReadOnlyList<GameBehaviour> Behaviours
    {
        get { lock (_lock) return _behaviours.ToList(); }
    }

    /// <summary>活跃行为数量</summary>
    public static int Count { get { lock (_lock) return _behaviours.Count; } }

    /// <summary>
    /// 添加一个行为实例。下一帧调用 OnAwake / OnEnable，随后是 OnStart。
    /// </summary>
    public static T Add<T>(T behaviour) where T : GameBehaviour
    {
        lock (_lock)
        {
            _pendingAdd.Add(behaviour);
        }
        return behaviour;
    }

    /// <summary>
    /// 移除并销毁一个行为实例。下一帧调用 OnDisable / OnStop。
    /// </summary>
    public static void Remove(GameBehaviour behaviour)
    {
        lock (_lock)
        {
            _pendingRemove.Add(behaviour);
        }
    }

    /// <summary>
    /// 移除并销毁指定类型的所有行为。
    /// </summary>
    public static void RemoveAll<T>() where T : GameBehaviour
    {
        lock (_lock)
        {
            foreach (var b in _behaviours)
                if (b is T)
                    _pendingRemove.Add(b);
        }
    }

    /// <summary>
    /// 移除并销毁所有行为。
    /// </summary>
    public static void RemoveAll()
    {
        lock (_lock)
        {
            _pendingRemove.AddRange(_behaviours);
        }
    }

    /// <summary>
    /// 获取第一个指定类型的行为，若不存在返回 null。
    /// </summary>
    public static T? Get<T>() where T : GameBehaviour
    {
        lock (_lock)
        {
            return _behaviours.OfType<T>().FirstOrDefault();
        }
    }

    /// <summary>
    /// 获取所有指定类型的行为。
    /// </summary>
    public static List<T> GetAll<T>() where T : GameBehaviour
    {
        lock (_lock)
        {
            return _behaviours.OfType<T>().ToList();
        }
    }

    // ── 异常处理 ──

    /// <summary>
    /// 记录回调异常。与 Unity 一致：不禁用该行为，下一帧照旧调用，
    /// 因此一个坏掉的行为会持续报错 —— 这是有意的，止损由使用者决定。
    /// </summary>
    private static void LogFault(GameBehaviour b, string phase, Exception ex)
        => Logger.Error(nameof(BehaviourManager), $"{b.GetType().Name}.{phase}: {ex}");

    internal static void SafeEnable(GameBehaviour b)
    {
        try { b.OnEnable(); }
        catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnEnable), ex); }
    }

    internal static void SafeDisable(GameBehaviour b)
    {
        try { b.OnDisable(); }
        catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnDisable), ex); }
    }

    private static GameBehaviour[] Snapshot()
    {
        lock (_lock) return _behaviours.ToArray();
    }

    // ── 内部驱动（由 ModManagerUI 每帧调用） ──

    /// <summary>处理增删队列，派发 OnAwake/OnEnable 与 OnDisable/OnStop，在 Update 之前调用。</summary>
    internal static void ProcessPending()
    {
        GameBehaviour[] toRemove, toAdd;

        // 队列先取出再清空，之后所有回调都在锁外跑：
        // OnStop 里调用 Remove() 是很自然的写法，若在 foreach 中就会改到正在遍历的集合。
        lock (_lock)
        {
            if (_pendingRemove.Count == 0 && _pendingAdd.Count == 0) return;

            toRemove = _pendingRemove.ToArray();
            _pendingRemove.Clear();
            toAdd = _pendingAdd.ToArray();
            _pendingAdd.Clear();

            foreach (var b in toRemove)
                _behaviours.Remove(b);
            _behaviours.AddRange(toAdd);
        }

        foreach (var b in toRemove)
        {
            if (b.IsDestroyed) continue;
            b.IsDestroyed = true;
            if (b.Awoken && b.EnabledRaw) SafeDisable(b);
            if (b.Started)
            {
                try { b.OnStop(); }
                catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnStop), ex); }
            }
        }

        foreach (var b in toAdd)
        {
            if (b.IsDestroyed || b.Awoken) continue;
            b.Awoken = true; // 先置位：抛异常也不重复 Awake
            try { b.OnAwake(); }
            catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnAwake), ex); }
            if (b.EnabledRaw) SafeEnable(b);
        }
    }

    /// <summary>按 Unity 的阶段顺序派发 OnStart → OnUpdate → OnLateUpdate。</summary>
    internal static void Update(float delta)
    {
        var snapshot = Snapshot();

        // Unity 会先跑完所有 Start，再进入 Update
        foreach (var b in snapshot)
        {
            if (b.IsDestroyed || !b.EnabledRaw || b.Started) continue;
            b.Started = true; // 先置位：Start 只尝试一次，抛异常也不会每帧重来
            try { b.OnStart(); }
            catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnStart), ex); }
        }

        foreach (var b in snapshot)
        {
            if (b.IsDestroyed || !b.EnabledRaw || !b.Started) continue;
            try { b.OnUpdate(delta); }
            catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnUpdate), ex); }
        }

        foreach (var b in snapshot)
        {
            if (b.IsDestroyed || !b.EnabledRaw || !b.Started) continue;
            try { b.OnLateUpdate(delta); }
            catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnLateUpdate), ex); }
        }
    }

    /// <summary>对所有行为调用 OnGUI（ImGui 窗口渲染之后）。</summary>
    internal static void GUI(ImDrawListPtr drawList)
    {
        foreach (var b in Snapshot())
        {
            if (b.IsDestroyed || !b.EnabledRaw || !b.Started) continue;
            try { b.OnGUI(drawList); }
            catch (Exception ex) { LogFault(b, nameof(GameBehaviour.OnGUI), ex); }
        }
    }
}
