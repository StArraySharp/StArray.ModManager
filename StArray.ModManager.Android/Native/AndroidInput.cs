namespace StArray.ModManager.Android.Native;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Android NDK 输入系统 API 的 C# P/Invoke 封装。
/// 对应 AOSP 的 input.h 和 libui.so / libandroid.so。
/// </summary>
public static class AndroidInput
{
    // ======================== 常量与枚举 ========================

    /// <summary>ALooper_prepare 选项</summary>
    [Flags]
    public enum PrepareFlags
    {
        None = 0,
        AllowNonCallbacks = 1 << 0,
    }

    /// <summary>输入事件类型</summary>
    public enum EventType
    {
        Key = 1,
        Motion = 2,
    }

    /// <summary>按键动作</summary>
    public enum KeyAction
    {
        Down = 0,
        Up = 1,
        Multiple = 2,
    }

    /// <summary>触摸动作（主动作需先去除指针索引位）</summary>
    public enum MotionAction
    {
        Down = 0,
        Up = 1,
        Move = 2,
        Cancel = 3,
        Outside = 4,
        PointerDown = 5,
        PointerUp = 6,
        HoverMove = 7,
        Scroll = 8,
        HoverEnter = 9,
        HoverExit = 10,
        ButtonPress = 11,
        ButtonRelease = 12,
    }

    /// <summary>Meta 键状态</summary>
    [Flags]
    public enum MetaState
    {
        None = 0,
        ShiftOn = 0x01,
        AltOn = 0x02,
        ShiftLeftOn = 0x40,
        ShiftRightOn = 0x80,
        AltLeftOn = 0x10,
        AltRightOn = 0x20,
    }

    /// <summary>触摸事件动作掩码工具</summary>
    public static class MotionMask
    {
        public const int Action = 0xFF;
        public const int PointerIndex = 0xFF00;
        public const int PointerIndexShift = 8;
    }

    // Keep integer helpers available to the timestamp broadcast path. The raw
    // action is read only once per native event, avoiding repeated P/Invoke
    // calls on the Android input thread.
    public const int ActionMask = MotionMask.Action;
    public const int ActionPointerIndexMask = MotionMask.PointerIndex;
    public const int ActionPointerIndexShift = MotionMask.PointerIndexShift;

    // ======================== 不透明句柄结构体 ========================

    /// <summary>不透明输入事件句柄</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AInputEvent { }

    /// <summary>不透明输入队列句柄</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AInputQueue { }

    /// <summary>不透明 Looper 句柄</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ALooper { }

    // ======================== 委托类型 ========================

    /// <summary>
    /// AInputQueue_attachLooper 回调签名。
    /// 必须保持委托实例的引用，防止被 GC 回收。
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InputQueueCallback(int fd, int events, IntPtr data);

    // ======================== P/Invoke 导入 ========================

    private const string Lib = "libandroid.so";

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ALooper_prepare(PrepareFlags opts);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void AInputQueue_attachLooper(
        IntPtr queue,
        IntPtr looper,
        int ident,
        InputQueueCallback callback,
        IntPtr data
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void AInputQueue_detachLooper(IntPtr queue);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AInputQueue_getEvent(
        IntPtr queue,
        out IntPtr outEvent
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void AInputQueue_finishEvent(
        IntPtr queue,
        IntPtr ev,
        int handled
    );

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern EventType AInputEvent_getType(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AKeyEvent_getKeyCode(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern KeyAction AKeyEvent_getAction(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern MetaState AKeyEvent_getMetaState(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AKeyEvent_getRepeatCount(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AMotionEvent_getAction(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern float AMotionEvent_getX(IntPtr ev, int pointerIndex);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern float AMotionEvent_getY(IntPtr ev, int pointerIndex);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AMotionEvent_getPointerCount(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int AMotionEvent_getPointerId(IntPtr ev, int pointerIndex);

    /// <summary>
    /// 事件发生时刻，单位纳秒，时钟源为 <c>CLOCK_MONOTONIC</c>。
    /// 该时间戳独立于渲染帧，可用于还原触摸真实发生时刻。
    /// </summary>
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern long AMotionEvent_getEventTime(IntPtr ev);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern long AMotionEvent_getDownTime(IntPtr ev);

    // ======================== 辅助方法（扩展风格） ========================

    /// <summary>从原始动作值取出主动作。</summary>
    public static MotionAction GetMainAction(int rawAction)
        => (MotionAction)(rawAction & ActionMask);

    /// <summary>从原始动作值取出指针索引。</summary>
    public static int GetPointerIndex(int rawAction)
        => (rawAction & ActionPointerIndexMask) >> ActionPointerIndexShift;

    /// <summary>获取触摸事件的主动作（去除指针索引）</summary>
    public static MotionAction GetMainAction(this IntPtr ev)
    {
        if (AInputEvent_getType(ev) != EventType.Motion)
            throw new InvalidOperationException("事件不是 Motion 类型");
        int raw = AMotionEvent_getAction(ev);
        return GetMainAction(raw);
    }

    /// <summary>获取触摸事件的指针索引（用于多点触控）</summary>
    public static int GetPointerIndex(this IntPtr ev)
    {
        int raw = AMotionEvent_getAction(ev);
        return GetPointerIndex(raw);
    }
}
