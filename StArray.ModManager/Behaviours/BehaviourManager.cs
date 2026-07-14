using ImGuiNET;

namespace StArray.ModManager.Behaviours;

/// <summary>
/// 管理所有 <see cref="GameBehaviour"/> 的生命周期。
/// 静态方法，无需实例化。
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
    /// 添加一个行为实例。如果未启动，将在下一帧调用 OnStart。
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
    /// 移除并销毁一个行为实例。下一帧调用 OnStop。
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

    // ── 内部驱动（由 ModManagerUI 每帧调用） ──

    /// <summary>处理增删队列，调用 OnStart / OnStop，在 Update 之前调用。</summary>
    internal static void ProcessPending()
    {
        lock (_lock)
        {
            // 先删
            foreach (var b in _pendingRemove)
            {
                if (_behaviours.Remove(b))
                {
                    if (b.Started)
                        b.OnStop();
                    b.IsDestroyed = true;
                }
            }
            _pendingRemove.Clear();

            // 再加
            foreach (var b in _pendingAdd)
                _behaviours.Add(b);
            _pendingAdd.Clear();
        }
    }

    /// <summary>对所有行为调用 OnStart（仅未启动的），然后调用 OnUpdate。</summary>
    internal static void Update(float delta)
    {
        GameBehaviour[] snapshot;
        lock (_lock)
        {
            snapshot = _behaviours.ToArray();
        }

        foreach (var b in snapshot)
        {
            if (b.IsDestroyed) continue;

            if (!b.Started)
            {
                b.OnStart();
                b.Started = true;
            }

            b.OnUpdate(delta);
        }
    }

    /// <summary>对所有行为调用 OnGUI（ImGui 窗口渲染之后）。</summary>
    internal static void GUI(ImDrawListPtr drawList)
    {
        GameBehaviour[] snapshot;
        lock (_lock)
        {
            snapshot = _behaviours.ToArray();
        }

        foreach (var b in snapshot)
        {
            if (b.IsDestroyed) continue;
            b.OnGUI(drawList);
        }
    }
}
