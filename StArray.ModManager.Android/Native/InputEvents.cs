using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

/// <summary>一次完整触摸事件的快照。所有字段均从 Java MotionEvent 复制而来。</summary>
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
/// 输入事件广播点。每个订阅者收到的都是从原始 Android 输入事件复制出的托管快照。
/// </summary>
/// <remarks>
/// 所有回调都在 Android 输入分发线程同步执行；OnInput 每个 Java KeyEvent/MotionEvent
/// 恰好回调一次，MotionEvent 的当前样本和历史样本都包含在同一份快照中。回调应快速返回，
/// 耗时工作应由订阅者自行转交到后台队列。OnInput、OnTouch 和 OnTouchTimestamp
/// 都由宿主 Activity 的 Java 输入桥接提供，因此宿主 APK 必须包含对应的 Activity 补丁。
/// </remarks>
public static class InputEvents
{
    private const string LogTag = nameof(InputEvents);
    private static Action<TouchEventInfo>? s_onTouch;
    private static Action<TouchTimestampInfo>? s_onTouchTimestamp;
    private static Action<AndroidInputEventInfo>? s_onInput;
    private static int s_touchSubscriberCount;
    private static int s_touchTimestampSubscriberCount;
    private static int s_inputSubscriberCount;
    private static int s_faultLogged;

    private static readonly object DedupLock = new();
    /// <summary>是否已有任一类订阅者。</summary>
    public static bool HasSubscribers =>
        Volatile.Read(ref s_touchSubscriberCount) > 0
        || Volatile.Read(ref s_touchTimestampSubscriberCount) > 0
        || Volatile.Read(ref s_inputSubscriberCount) > 0;

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
    /// Full Java KeyEvent/MotionEvent snapshots, including external keyboard metadata, multi-touch
    /// pointers, historical samples, screen coordinates, timestamps, and Android axis values.
    /// Each callback runs synchronously on the Android input-dispatch thread. This event and the
    /// touch-only compatibility events require the patched host Activity bridge.
    /// </summary>
    public static event Action<AndroidInputEventInfo>? OnInput
    {
        add
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onInput += value;
                Interlocked.Increment(ref s_inputSubscriberCount);
                AndroidJavaInputBridge.SetFullDataEnabled(true);
            }
        }
        remove
        {
            if (value == null) return;
            lock (DedupLock)
            {
                s_onInput -= value;
                int count = Interlocked.Decrement(ref s_inputSubscriberCount);
                if (count <= 0)
                {
                    Interlocked.Exchange(ref s_inputSubscriberCount, 0);
                    AndroidJavaInputBridge.SetFullDataEnabled(false);
                }
            }
        }
    }

    /// <summary>Synchronously broadcast one copied Java event on its input-dispatch thread.</summary>
    internal static void RaiseFromJava(AndroidInputEventInfo input)
    {
        Action<AndroidInputEventInfo>? inputHandlers;
        Action<TouchEventInfo>? touchHandlers;
        Action<TouchTimestampInfo>? timestampHandlers;
        lock (DedupLock)
        {
            inputHandlers = s_onInput;
            touchHandlers = s_onTouch;
            timestampHandlers = s_onTouchTimestamp;
        }

        if (inputHandlers != null)
            DispatchInputHandlers(inputHandlers, input);

        if (input.Kind != AndroidInputEventKind.Motion
            || input.IsGenericMotion
            || (touchHandlers == null && timestampHandlers == null))
        {
            return;
        }

        int pointerCount = input.StoredPointerCount;
        if (pointerCount == 0)
            return;

        AndroidInput.MotionAction action = (AndroidInput.MotionAction)input.Action;
        int pointerIndex = Math.Clamp(input.ActionIndex, 0, pointerCount - 1);
        AndroidInputPointerInfo pointer = input.Pointers.Span[pointerIndex];
        long eventTimeNanos = input.EventTimeNanos;
        int pointerId = action == AndroidInput.MotionAction.Cancel ? -1 : pointer.Id;
        bool timestampAction = action is AndroidInput.MotionAction.Down
            or AndroidInput.MotionAction.PointerDown
            or AndroidInput.MotionAction.Up
            or AndroidInput.MotionAction.PointerUp
            or AndroidInput.MotionAction.Cancel;

        if (timestampHandlers != null && timestampAction)
        {
            DispatchTimestampHandlers(
                timestampHandlers,
                new TouchTimestampInfo(action, pointerId, eventTimeNanos));
        }

        if (touchHandlers != null)
        {
            DispatchTouchHandlers(
                touchHandlers,
                new TouchEventInfo(
                    action,
                    pointerIndex,
                    pointerId,
                    eventTimeNanos,
                    pointer.X,
                    pointer.Y));
        }
    }

    private static void DispatchInputHandlers(
        Action<AndroidInputEventInfo> handlers,
        AndroidInputEventInfo input)
    {
        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<AndroidInputEventInfo>)handler)(input);
            }
            catch (Exception exception)
            {
                LogOnce($"Input subscriber threw: {exception}");
            }
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
