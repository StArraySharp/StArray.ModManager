using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.UI;

public static partial class ImGuiInputHandler
{
    private const float TouchScrollStartDistance = 12.0f;
    private const string TouchScrollImGuiVersion = "1.91.6";

    private enum TouchScrollPhase { None, AwaitingPress, Candidate, Panning }

    // Read-only prefix of ImGuiWindow from the bundled 64-bit Dear ImGui 1.91.6 docking build.
    // Keep the version check and update these offsets against imgui_internal.h when upgrading.
    [StructLayout(LayoutKind.Explicit)]
    private struct ScrollWindow
    {
        [FieldOffset(0)] public nint Context;
        [FieldOffset(16)] public uint Id;
        [FieldOffset(20)] public ImGuiWindowFlags Flags;
        [FieldOffset(88)] public Vector2 Position;
        [FieldOffset(96)] public Vector2 Size;
        [FieldOffset(152)] public float TitleBarHeight;
        [FieldOffset(156)] public float MenuBarHeight;
        [FieldOffset(204)] public Vector2 Scroll;
        [FieldOffset(212)] public Vector2 ScrollMax;
        [FieldOffset(244)] public Vector2 ScrollbarSizes;
        [FieldOffset(255)] public byte Active;

        public readonly bool CanScroll() =>
            Active != 0 && (Flags & (ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoScrollWithMouse)) == 0
            && float.IsFinite(ScrollMax.X) && float.IsFinite(ScrollMax.Y)
            && ScrollMax.X >= 0 && ScrollMax.Y >= 0 && ScrollMax != Vector2.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public Vector2 Min;
        public Vector2 Max;

        public readonly bool Contains(Vector2 point) =>
            point.X >= Min.X && point.X < Max.X && point.Y >= Min.Y && point.Y < Max.Y;
    }

    [DllImport("cimgui", EntryPoint = "igFindHoveredWindowEx", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FindHoveredWindowEx(Vector2 position,
        [MarshalAs(UnmanagedType.I1)] bool anyViewport, out nint window, out nint windowBelowMoving);

    [DllImport("cimgui", EntryPoint = "igFindWindowByID", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint FindScrollWindowById(uint id);

    [DllImport("cimgui", EntryPoint = "ImGuiWindow_Rect", CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetWindowRect(out NativeRect rect, nint window);

    [DllImport("cimgui", EntryPoint = "igGetActiveID", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint GetActiveId();

    [DllImport("cimgui", EntryPoint = "igGetWindowScrollbarID", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint GetScrollbarId(nint window, int axis);

    [DllImport("cimgui", EntryPoint = "igGetWindowScrollbarRect", CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetScrollbarRect(out NativeRect rect, nint window, int axis);

    [DllImport("cimgui", EntryPoint = "igIsWindowContentHoverable", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool IsWindowContentHoverable(nint window, ImGuiHoveredFlags flags);

    [DllImport("cimgui", EntryPoint = "igIsActiveIdUsingNavDir", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool ActiveItemUsesDirection(ImGuiDir direction);

    [DllImport("cimgui", EntryPoint = "igSetScrollX_WindowPtr", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetWindowScrollX(nint window, float value);

    [DllImport("cimgui", EntryPoint = "igSetScrollY_WindowPtr", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetWindowScrollY(nint window, float value);

    private static TouchScrollPhase s_touchPhase;
    private static bool? s_touchScrollSupported;
    private static bool s_touchPressedWidget;
    private static Vector2 s_touchStart;
    private static Vector2 s_touchLast;
    private static Vector2 s_touchScrollMax;
    private static uint s_touchWindowId;
    private static bool s_touchScrollHorizontal;
    private static float s_pendingScrollDelta;
    private static float s_scrollRemainder;

    private static void BeginTouchGesture(float x, float y)
    {
        s_touchScrollSupported ??= RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            && ImGui.GetVersion() == TouchScrollImGuiVersion;
        if (!s_touchScrollSupported.Value)
            return;

        s_touchStart = s_touchLast = new Vector2(x, y);
        s_touchPhase = TouchScrollPhase.AwaitingPress;
    }

    private static void EndTouchGesture() => s_touchPhase = TouchScrollPhase.None;

    private static bool HandleTouchScrollMove(ImGuiIOPtr io, float x, float y)
    {
        var position = new Vector2(x, y);
        switch (s_touchPhase)
        {
            case TouchScrollPhase.AwaitingPress:
                s_touchLast = position;
                return true;
            case TouchScrollPhase.Candidate:
                s_touchLast = position;
                return TryStartTouchScroll(io);
            case TouchScrollPhase.Panning:
                s_pendingScrollDelta += s_touchScrollHorizontal
                    ? s_touchLast.X - x : s_touchLast.Y - y;
                s_touchLast = position;
                return true;
            default:
                return false;
        }
    }

    // Classify after the widgets have processed the press and before EndFrame can start moving
    // a window from empty content. Hold subsequent pointer positions until the gesture is decided.
    internal static void UpdateTouchScrollState()
    {
        if (s_touchPhase != TouchScrollPhase.AwaitingPress)
            return;

        var io = ImGui.GetIO();
        if (!io.MouseClicked[0])
            return;

        try
        {
            Vector2 click = ImGui.GetMousePos();
            if (Vector2.DistanceSquared(click, s_touchStart) <= 2.0f && io.WantCaptureMouse
                && TryGetTouchScrollTarget(out var window))
            {
                s_touchWindowId = window.Id;
                s_touchScrollMax = window.ScrollMax;
                s_touchPressedWidget = ImGui.IsAnyItemActive();
                s_touchPhase = TouchScrollPhase.Candidate;
                if (TryStartTouchScroll(io))
                    return;
            }
        }
        catch (EntryPointNotFoundException ex)
        {
            DisableTouchScroll(ex);
        }

        EndTouchGesture();
        io.AddMousePosEvent(s_touchLast.X, s_touchLast.Y);
    }

    private static unsafe bool TryGetTouchScrollTarget(out ScrollWindow window)
    {
        FindHoveredWindowEx(s_touchStart, false, out nint pointer, out _);
        window = default;
        if (pointer == 0 || !IsWindowContentHoverable(pointer, ImGuiHoveredFlags.None))
            return false;

        // Sliders, DragScalar and text editors claim navigation directions when activated.
        // They keep the original press and all pointer movement regardless of drag direction.
        int cursor = (int)ImGui.GetMouseCursor();
        if (ActiveItemUsesDirection(ImGuiDir.Left) || ActiveItemUsesDirection(ImGuiDir.Right)
            || ActiveItemUsesDirection(ImGuiDir.Up) || ActiveItemUsesDirection(ImGuiDir.Down)
            || cursor is >= (int)ImGuiMouseCursor.TextInput and <= (int)ImGuiMouseCursor.ResizeNWSE)
            return false;

        window = *(ScrollWindow*)pointer;
        GetWindowRect(out var bounds, pointer);
        if (window.Context != ImGui.GetCurrentContext() || window.Position != bounds.Min
            || window.Position + window.Size != bounds.Max)
        {
            DisableTouchScroll();
            return false;
        }

        if (!window.CanScroll() || !bounds.Contains(s_touchStart)
            || s_touchStart.Y < bounds.Min.Y + window.TitleBarHeight + window.MenuBarHeight)
            return false;

        // A scrollbar can be visible even when that axis has zero scroll range.
        uint activeId = GetActiveId();
        return !IsScrollbarPress(pointer, 0, window.ScrollbarSizes.Y, activeId)
            && !IsScrollbarPress(pointer, 1, window.ScrollbarSizes.X, activeId);
    }

    private static bool IsScrollbarPress(nint window, int axis, float size, uint activeId)
    {
        if (size <= 0)
            return false;

        GetScrollbarRect(out var bounds, window, axis);
        return bounds.Contains(s_touchStart) || (activeId != 0 && activeId == GetScrollbarId(window, axis));
    }

    private static bool TryStartTouchScroll(ImGuiIOPtr io)
    {
        Vector2 delta = s_touchLast - s_touchStart;
        float threshold = MathF.Max(TouchScrollStartDistance, io.MouseDragThreshold * 2.0f);
        float distance = delta.Length();
        if (distance < threshold)
            return true;

        bool horizontal = MathF.Abs(delta.X) > MathF.Abs(delta.Y);
        if ((horizontal ? s_touchScrollMax.X : s_touchScrollMax.Y) <= 0)
        {
            EndTouchGesture();
            return false;
        }

        s_touchPhase = TouchScrollPhase.Panning;
        s_touchScrollHorizontal = horizontal;
        s_pendingScrollDelta += -(horizontal ? delta.X : delta.Y) * (1.0f - threshold / distance);
        s_scrollRemainder = 0.0f;

        // A release away from a pressed button or selectable prevents activation on touch-up.
        if (s_touchPressedWidget)
            io.AddMousePosEvent(-float.MaxValue, -float.MaxValue);
        io.AddMouseButtonEvent(0, false);
        if (s_touchPressedWidget)
            io.AddMousePosEvent(s_touchStart.X, s_touchStart.Y);
        return true;
    }

    // Apply the accumulated finger displacement before Begin() lays out the target window.
    internal static unsafe void ApplyTouchScroll()
    {
        float delta = s_pendingScrollDelta;
        s_pendingScrollDelta = 0.0f;
        if (delta == 0.0f)
            return;

        try
        {
            nint pointer = FindScrollWindowById(s_touchWindowId);
            if (pointer == 0 || !((ScrollWindow*)pointer)->CanScroll())
            {
                EndTouchGesture();
                return;
            }

            ScrollWindow window = *(ScrollWindow*)pointer;
            float current = s_touchScrollHorizontal ? window.Scroll.X : window.Scroll.Y;
            float max = s_touchScrollHorizontal ? window.ScrollMax.X : window.ScrollMax.Y;
            float target = Math.Clamp(current + delta + s_scrollRemainder, 0.0f, max);
            float pixels = MathF.Truncate(target);
            s_scrollRemainder = target - pixels;
            if (s_touchScrollHorizontal)
                SetWindowScrollX(pointer, pixels);
            else
                SetWindowScrollY(pointer, pixels);
        }
        catch (EntryPointNotFoundException ex)
        {
            DisableTouchScroll(ex);
        }
    }

    private static void DisableTouchScroll(EntryPointNotFoundException? exception = null)
    {
        s_touchScrollSupported = false;
        EndTouchGesture();
        Logger.Warn(nameof(ImGuiInputHandler), exception is null
            ? "Touch scrolling is unavailable because the loaded ImGui window layout does not match."
            : $"Touch scrolling is unavailable with the loaded cimgui library: {exception.Message}");
    }
}
