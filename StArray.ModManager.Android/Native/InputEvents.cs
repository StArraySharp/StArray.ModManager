using System.Diagnostics;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

/// <summary>一次完整触摸事件的快照。所有字段在广播时即已从原生事件读出。</summary>
/// <param name="Action">主动作，已去除指针索引</param>
/// <param name="PointerIndex">指针索引</param>
/// <param name="PointerId">稳定的指针 ID，比索引更适合跨事件跟踪多指输入</param>
/// <param name="EventTimeNanos">事件发生时刻，纳秒，时钟源为 CLOCK_MONOTONIC</param>
/// <param name="X">触点 X 坐标（像素）</param>
/// <param name="Y">触点 Y 坐标（像素）</param>
public readonly record struct TouchEventInfo(
    AndroidInput.MotionAction Action,
    int PointerIndex,
    int PointerId,
    long EventTimeNanos,
    float X,
    float Y)
{
    // Source compatibility for mods built against the first broadcast API.
    public TouchEventInfo(
        AndroidInput.MotionAction action,
        int pointerIndex,
        long eventTimeNanos,
        float x,
        float y)
        : this(action, pointerIndex, -1, eventTimeNanos, x, y)
    {
    }
}

/// <summary>
/// 只包含异步输入所需字段的原始触摸快照。
/// </summary>
/// <remarks>
/// 该事件不读取坐标，且只广播 Down/Up/Cancel。高 KPS 时订阅方不需要为 Move
/// 事件读取坐标或创建完整手势快照。
/// </remarks>
public readonly record struct TouchTimestampInfo(
    AndroidInput.MotionAction Action,
    int PointerId,
    long EventTimeNanos);

/// <summary>
/// 输入事件广播点。原生输入 Hook 只负责解析一次事件，订阅方在自己的队列中异步处理。
/// </summary>
/// <remarks>
/// 回调运行在 Android 输入分发线程，不是 Unity 主线程。订阅方只能做廉价的值类型快照
/// 和入队操作，不能访问 Unity 对象或执行 IL2CPP 游戏逻辑。
/// </remarks>
public static class InputEvents
{
    private const string LogTag = nameof(InputEvents);
    private const long DuplicateWindowMilliseconds = 8L;

    private static Action<TouchEventInfo>? s_onTouch;
    private static Action<TouchTimestampInfo>? s_onTouchTimestamp;
    private static int s_touchSubscriberCount;
    private static int s_touchTimestampSubscriberCount;
    private static int s_faultLogged;

    private static readonly object DedupLock = new();
    private static int s_lastRawAction;
    private static int s_lastPointerIndex;
    private static int s_lastPointerCount;
    private static int s_lastPointerId;
    private static long s_lastEventTimeNanos;
    private static long s_lastDispatchTicks;

    /// <summary>是否已有任一类订阅者。</summary>
    public static bool HasSubscribers =>
        Volatile.Read(ref s_touchSubscriberCount) > 0
        || Volatile.Read(ref s_touchTimestampSubscriberCount) > 0;

    /// <summary>完整触摸事件广播，保留坐标和 Move 事件。</summary>
    public static event Action<TouchEventInfo>? OnTouch
    {
        add
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onTouch += value;
                Interlocked.Increment(ref s_touchSubscriberCount);
            }
        }
        remove
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onTouch -= value;
                Interlocked.Decrement(ref s_touchSubscriberCount);
            }
        }
    }

    /// <summary>
    /// 异步输入时间戳快速广播，只处理 Down、PointerDown、Up、PointerUp 和 Cancel。
    /// </summary>
    public static event Action<TouchTimestampInfo>? OnTouchTimestamp
    {
        add
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onTouchTimestamp += value;
                Interlocked.Increment(ref s_touchTimestampSubscriberCount);
            }
        }
        remove
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onTouchTimestamp -= value;
                Interlocked.Decrement(ref s_touchTimestampSubscriberCount);
            }
        }
    }

    /// <summary>
    /// 从原生 AInputEvent 解析并广播。输入事件只在这里读取一次，避免每个 Mod 重复访问
    /// 原生对象；重复的同一事件只在广播层过滤一次。
    /// </summary>
    internal static void RaiseFrom(nint inputEvent)
    {
        Action<TouchEventInfo>? handlers;
        Action<TouchTimestampInfo>? timestampHandlers;
        lock (DedupLock)
        {
            handlers = s_onTouch;
            timestampHandlers = s_onTouchTimestamp;
        }

        if ((handlers == null && timestampHandlers == null) || inputEvent == 0)
            return;

        try
        {
            if (AndroidInput.AInputEvent_getType(inputEvent) != AndroidInput.EventType.Motion)
                return;

            int rawAction = AndroidInput.AMotionEvent_getAction(inputEvent);
            AndroidInput.MotionAction action = AndroidInput.GetMainAction(rawAction);
            int pointerIndex = AndroidInput.GetPointerIndex(rawAction);
            bool timestampAction = action is AndroidInput.MotionAction.Down
                or AndroidInput.MotionAction.PointerDown
                or AndroidInput.MotionAction.Up
                or AndroidInput.MotionAction.PointerUp
                or AndroidInput.MotionAction.Cancel;

            // AsyncInput subscribes only to the timestamp channel. Do not
            // inspect Move coordinates, pointer IDs, or pointer counts on the
            // hot path when no full-gesture subscriber exists.
            if (!timestampAction && handlers == null)
                return;

            int pointerCount = AndroidInput.AMotionEvent_getPointerCount(inputEvent);
            long eventTimeNanos = AndroidInput.AMotionEvent_getEventTime(inputEvent);
            int pointerId = action == AndroidInput.MotionAction.Cancel
                ? -1
                : AndroidInput.AMotionEvent_getPointerId(inputEvent, pointerIndex);

            if (IsDuplicate(
                    rawAction,
                    pointerIndex,
                    pointerCount,
                    pointerId,
                    eventTimeNanos))
                return;

            if (timestampHandlers != null && timestampAction)
            {
                DispatchTimestampHandlers(
                    timestampHandlers,
                    new TouchTimestampInfo(action, pointerId, eventTimeNanos));
            }

            if (handlers == null)
                return;

            TouchEventInfo info = new(
                action,
                pointerIndex,
                pointerId,
                eventTimeNanos,
                AndroidInput.AMotionEvent_getX(inputEvent, pointerIndex),
                AndroidInput.AMotionEvent_getY(inputEvent, pointerIndex));
            DispatchTouchHandlers(handlers, info);
        }
        catch (Exception exception)
        {
            LogOnce($"Failed to read native input event: {exception}");
        }
    }

    private static bool IsDuplicate(
        int rawAction,
        int pointerIndex,
        int pointerCount,
        int pointerId,
        long eventTimeNanos)
    {
        long now = Stopwatch.GetTimestamp();
        long windowTicks = Math.Max(
            1L,
            Stopwatch.Frequency * DuplicateWindowMilliseconds / 1000L);

        lock (DedupLock)
        {
            long elapsed = now - s_lastDispatchTicks;
            bool sameEventPayload = s_lastRawAction == rawAction
                && s_lastPointerIndex == pointerIndex
                && s_lastPointerCount == pointerCount
                && s_lastPointerId == pointerId
                && s_lastEventTimeNanos == eventTimeNanos;
            bool duplicate = sameEventPayload
                && elapsed >= 0L
                && elapsed <= windowTicks;

            if (duplicate)
                return true;

            s_lastRawAction = rawAction;
            s_lastPointerIndex = pointerIndex;
            s_lastPointerCount = pointerCount;
            s_lastPointerId = pointerId;
            s_lastEventTimeNanos = eventTimeNanos;
            s_lastDispatchTicks = now;
            return false;
        }
    }

    private static void DispatchTimestampHandlers(
        Action<TouchTimestampInfo> handlers,
        TouchTimestampInfo info)
    {
        if (Volatile.Read(ref s_touchTimestampSubscriberCount) == 1)
        {
            try
            {
                handlers(info);
            }
            catch (Exception exception)
            {
                LogOnce($"Touch timestamp subscriber threw: {exception}");
            }
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<TouchTimestampInfo>)handler)(info);
            }
            catch (Exception exception)
            {
                LogOnce($"Touch timestamp subscriber threw: {exception}");
            }
        }
    }

    private static void DispatchTouchHandlers(
        Action<TouchEventInfo> handlers,
        TouchEventInfo info)
    {
        if (Volatile.Read(ref s_touchSubscriberCount) == 1)
        {
            try
            {
                handlers(info);
            }
            catch (Exception exception)
            {
                LogOnce($"Touch event subscriber threw: {exception}");
            }
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<TouchEventInfo>)handler)(info);
            }
            catch (Exception exception)
            {
                LogOnce($"Touch event subscriber threw: {exception}");
            }
        }
    }

    private static void LogOnce(string message)
    {
        if (Interlocked.Exchange(ref s_faultLogged, 1) != 0)
            return;
        try
        {
            Logger.Error(LogTag, $"{message} (further occurrences suppressed)");
        }
        catch
        {
            // Never allow an exception to escape back through the native input stack.
        }
    }
}
