using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using ImGuiNET;
using StArray.ModManager.Android.UI;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

public enum AndroidInputEventKind
{
    Motion = 1,
    Key = 2,
}

/// <summary>A pointer sample included in a forwarded Android MotionEvent.</summary>
public readonly record struct AndroidInputPointerInfo(
    int Id,
    int ToolType,
    float X,
    float Y,
    float RawX,
    float RawY,
    float Pressure,
    float Size,
    float TouchMajor,
    float TouchMinor,
    float ToolMajor,
    float ToolMinor,
    float Orientation,
    float Tilt,
    float Distance,
    ReadOnlyMemory<float> AxisValues)
{
    /// <summary>Returns an Android MotionEvent axis value, or zero outside [0, 63].</summary>
    public float GetAxisValue(int axis)
        => (uint)axis < (uint)AxisValues.Length ? AxisValues.Span[axis] : 0.0f;
}

/// <summary>
/// One historical sample carried by a Java MotionEvent. Its timestamp uses the platform's
/// nanosecond API on Android 14 (API 34) and later, with a millisecond-resolution fallback on
/// earlier Android versions.
/// </summary>
public readonly record struct AndroidInputHistorySampleInfo(
    long EventTimeNanos,
    int PointerCount,
    ReadOnlyMemory<AndroidInputPointerInfo> Pointers)
{
    public int StoredPointerCount => Pointers.Length;
    public bool HasTruncatedPointers => StoredPointerCount < PointerCount;
}

/// <summary>
/// Immutable snapshot of one Java KeyEvent or MotionEvent. EventTimeNanos and historical sample
/// times use the MotionEvent nanosecond APIs on API 34+; older Android versions and KeyEvent use
/// millisecond values promoted to nanoseconds. DownTimeNanos is currently sourced from the public
/// millisecond API. All timestamps use Android's uptime clock. A MotionEvent's current sample and
/// history are kept together so each Activity input dispatch produces exactly one OnInput callback.
/// <param name="FullDataIncluded">Whether expanded MotionEvent axes/history sampling was enabled.</param>
/// </summary>
public readonly record struct AndroidInputEventInfo(
    AndroidInputEventKind Kind,
    bool IsGenericMotion,
    int Action,
    int RawAction,
    int ActionIndex,
    int PointerCount,
    ReadOnlyMemory<AndroidInputPointerInfo> Pointers,
    int Source,
    int DeviceId,
    int Flags,
    int MetaState,
    int ButtonState,
    int ActionButton,
    int KeyCode,
    int ScanCode,
    int RepeatCount,
    int UnicodeCodePoint,
    int ViewportWidth,
    int ViewportHeight,
    long EventTimeNanos,
    long DownTimeNanos,
    float HorizontalScroll,
    float VerticalScroll,
    int HistorySampleCount,
    ReadOnlyMemory<AndroidInputHistorySampleInfo> HistoricalSamples,
    bool FullDataIncluded)
{
    /// <summary>Number of pointers copied, limited to the bridge's 32-pointer bound.</summary>
    public int StoredPointerCount => Pointers.Length;

    /// <summary>True when the source event had more pointers than the bridge can store.</summary>
    public bool HasTruncatedPointers => StoredPointerCount < PointerCount;

    /// <summary>Number of historical samples copied, limited to the bridge's 256-sample bound.</summary>
    public int StoredHistorySampleCount => HistoricalSamples.Length;

    /// <summary>True when one or more source history samples were not included.</summary>
    public bool HasTruncatedHistory => StoredHistorySampleCount < HistorySampleCount;
}

/// <summary>JNI interop for input events forwarded by the host Activity.</summary>
internal static unsafe class AndroidJavaInputBridge
{
    private const int AbiVersion = 2;
    private const int MaxPointers = 32;
    private const int MaxHistorySamples = 256;
    private const int AxisCount = 64;
    private const int MotionEventType = 1;
    private const int KeyEventType = 2;
    private const int MaxPendingImGuiEvents = 512;

    private static readonly ConcurrentQueue<AndroidInputEventInfo> ImGuiInputQueue = new();
    private static int s_queuedImGuiEventCount;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeInputEventHeader
    {
        public int StructSize;
        public int AbiVersion;
        public int Type;
        public int IsGenericMotion;
        public int FullDataIncluded;
        public int Action;
        public int ActionIndex;
        public int PointerCount;
        public int StoredPointerCount;
        public int Source;
        public int DeviceId;
        public int Flags;
        public int MetaState;
        public int ButtonState;
        public int ActionButton;
        public int KeyCode;
        public int ScanCode;
        public int RepeatCount;
        public int UnicodeCodePoint;
        public int ViewportWidth;
        public int ViewportHeight;
        public int RawAction;
        public long EventTimeNanos;
        public long DownTimeNanos;
        public float HorizontalScroll;
        public float VerticalScroll;
        public int HistorySampleCount;
        public int StoredHistorySampleCount;
        public nint HistorySamples;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeInputHistorySample
    {
        public int PointerCount;
        public int StoredPointerCount;
        public long EventTimeNanos;
        public nint Pointers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct NativeInputPointer
    {
        public int Id;
        public int ToolType;
        public float X;
        public float Y;
        public float RawX;
        public float RawY;
        public float Pressure;
        public float Size;
        public float TouchMajor;
        public float TouchMinor;
        public float ToolMajor;
        public float ToolMinor;
        public float Orientation;
        public float Tilt;
        public float Distance;
        public fixed float AxisValues[AxisCount];
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeInputCallback(nint inputEvent);

    [DllImport("modmanager", EntryPoint = "modmanager_set_java_input_callback",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetInputCallback(nint callback);

    [DllImport("modmanager", EntryPoint = "modmanager_set_java_input_capture_state",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetCaptureStateNative(int overlayVisible, int captureMouse,
        int captureKeyboard);

    [DllImport("modmanager", EntryPoint = "modmanager_set_java_input_full_data_enabled",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetFullDataEnabledNative(int enabled);

    private static readonly NativeInputCallback s_callback = OnNativeInput;

    internal static bool RegisterCallback()
    {
        try
        {
            SetInputCallback(Marshal.GetFunctionPointerForDelegate(s_callback));
            return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException)
        {
            return false;
        }
    }

    internal static void SetCaptureState(bool captureMouse, bool captureKeyboard)
    {
        try
        {
            SetCaptureStateNative(0, captureMouse ? 1 : 0, captureKeyboard ? 1 : 0);
        }
        catch (Exception exception) when (exception is DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException)
        {
            // Input capture is available only through the host Activity bridge.
        }
    }

    /// <summary>Called on the render thread after the current ImGui frame is built.</summary>
    internal static void UpdateCaptureState()
    {
        if (!ImGuiInputHandler.IsInitialized)
            return;

        try
        {
            var io = ImGui.GetIO();
            SetCaptureState(io.WantCaptureMouse, io.WantCaptureKeyboard);
        }
        catch (Exception exception)
        {
            Logger.Warn(nameof(AndroidJavaInputBridge),
                $"Could not update Java input capture state: {exception.Message}");
        }
    }

    internal static void SetFullDataEnabled(bool enabled)
    {
        try
        {
            SetFullDataEnabledNative(enabled ? 1 : 0);
        }
        catch (Exception exception) when (exception is DllNotFoundException
            or EntryPointNotFoundException or BadImageFormatException)
        {
            // This option only controls optional MotionEvent axis sampling.
        }
    }

    private static void OnNativeInput(nint eventPointer)
    {
        if (eventPointer == nint.Zero)
            return;

        try
        {
            NativeInputEventHeader* input = (NativeInputEventHeader*)eventPointer;
            int requiredSize = sizeof(NativeInputEventHeader)
                + MaxPointers * sizeof(NativeInputPointer);
            if (input->AbiVersion != AbiVersion
                || input->StructSize < requiredSize
                || input->Type is not (MotionEventType or KeyEventType)
                || input->PointerCount < 0
                || input->StoredPointerCount < 0
                || input->StoredPointerCount > MaxPointers
                || input->StoredPointerCount > input->PointerCount
                || input->StoredPointerCount != Math.Min(input->PointerCount, MaxPointers)
                || input->FullDataIncluded is not (0 or 1)
                || input->HistorySampleCount < 0
                || input->StoredHistorySampleCount < 0
                || input->StoredHistorySampleCount > MaxHistorySamples
                || input->StoredHistorySampleCount > input->HistorySampleCount
                || (input->StoredHistorySampleCount > 0 && input->FullDataIncluded == 0)
                || (input->Type == KeyEventType
                    && (input->PointerCount != 0
                        || input->StoredPointerCount != 0
                        || input->HistorySampleCount != 0
                        || input->StoredHistorySampleCount != 0))
                || (input->StoredHistorySampleCount > 0 && input->HistorySamples == nint.Zero))
                return;

            AndroidInputEventInfo snapshot = CopyEvent(input);
            if (ImGuiInputHandler.IsInitialized)
                EnqueueForImGui(CreateImGuiSnapshot(snapshot));

            // Mod subscribers run synchronously on the Activity input-dispatch thread.
            InputEvents.RaiseFromJava(snapshot);
        }
        catch (Exception exception)
        {
            // Never unwind a managed exception through Activity's Java input dispatch.
            Logger.Error(nameof(AndroidJavaInputBridge),
                $"Java input event dispatch failed: {exception}");
        }
    }

    internal static AndroidInputEventInfo CopyEvent(NativeInputEventHeader* input)
    {
        int storedCount = input->StoredPointerCount;
        var nativePointers = (NativeInputPointer*)((byte*)input + sizeof(NativeInputEventHeader));
        AndroidInputPointerInfo[] pointers = CopyPointers(
            nativePointers, storedCount, input->FullDataIncluded != 0);

        int storedHistoryCount = input->StoredHistorySampleCount;
        var history = new AndroidInputHistorySampleInfo[storedHistoryCount];
        NativeInputHistorySample* nativeHistory = (NativeInputHistorySample*)input->HistorySamples;
        for (int index = 0; index < storedHistoryCount; index++)
        {
            NativeInputHistorySample* sample = &nativeHistory[index];
            if (sample->PointerCount < 0
                || sample->StoredPointerCount < 0
                || sample->StoredPointerCount > MaxPointers
                || sample->StoredPointerCount > sample->PointerCount
                || sample->StoredPointerCount != Math.Min(sample->PointerCount, MaxPointers)
                || sample->PointerCount != input->PointerCount
                || (sample->StoredPointerCount > 0 && sample->Pointers == nint.Zero))
            {
                throw new InvalidOperationException("Invalid native MotionEvent history sample.");
            }

            history[index] = new AndroidInputHistorySampleInfo(
                sample->EventTimeNanos,
                sample->PointerCount,
                CopyPointers(
                    (NativeInputPointer*)sample->Pointers,
                    sample->StoredPointerCount,
                    input->FullDataIncluded != 0));
        }

        return new AndroidInputEventInfo(
            input->Type == MotionEventType
                ? AndroidInputEventKind.Motion
                : AndroidInputEventKind.Key,
            input->IsGenericMotion != 0,
            input->Action,
            input->RawAction,
            input->ActionIndex,
            input->PointerCount,
            pointers,
            input->Source,
            input->DeviceId,
            input->Flags,
            input->MetaState,
            input->ButtonState,
            input->ActionButton,
            input->KeyCode,
            input->ScanCode,
            input->RepeatCount,
            input->UnicodeCodePoint,
            input->ViewportWidth,
            input->ViewportHeight,
            input->EventTimeNanos,
            input->DownTimeNanos,
            input->HorizontalScroll,
            input->VerticalScroll,
            input->HistorySampleCount,
            history,
            input->FullDataIncluded != 0);
    }

    private static AndroidInputPointerInfo[] CopyPointers(
        NativeInputPointer* nativePointers,
        int storedCount,
        bool includeAxes)
    {
        if (storedCount == 0)
            return Array.Empty<AndroidInputPointerInfo>();

        var pointers = new AndroidInputPointerInfo[storedCount];
        float[]? axisStorage = includeAxes ? new float[storedCount * AxisCount] : null;
        for (int index = 0; index < storedCount; index++)
        {
            NativeInputPointer* pointer = &nativePointers[index];
            ReadOnlyMemory<float> axes = ReadOnlyMemory<float>.Empty;
            if (axisStorage != null)
            {
                for (int axis = 0; axis < AxisCount; axis++)
                    axisStorage[index * AxisCount + axis] = pointer->AxisValues[axis];
                axes = new ReadOnlyMemory<float>(axisStorage, index * AxisCount, AxisCount);
            }

            pointers[index] = new AndroidInputPointerInfo(
                pointer->Id,
                pointer->ToolType,
                pointer->X,
                pointer->Y,
                pointer->RawX,
                pointer->RawY,
                pointer->Pressure,
                pointer->Size,
                pointer->TouchMajor,
                pointer->TouchMinor,
                pointer->ToolMajor,
                pointer->ToolMinor,
                pointer->Orientation,
                pointer->Tilt,
                pointer->Distance,
                axes);
        }

        return pointers;
    }

    private static void EnqueueForImGui(AndroidInputEventInfo input)
    {
        ImGuiInputQueue.Enqueue(input);
        int queued = Interlocked.Increment(ref s_queuedImGuiEventCount);
        if (queued > MaxPendingImGuiEvents && ImGuiInputQueue.TryDequeue(out _))
        {
            Interlocked.Decrement(ref s_queuedImGuiEventCount);
        }
    }

    private static AndroidInputEventInfo CreateImGuiSnapshot(AndroidInputEventInfo input)
    {
        ReadOnlySpan<AndroidInputPointerInfo> source = input.Pointers.Span;
        var pointers = new AndroidInputPointerInfo[source.Length];
        for (int index = 0; index < source.Length; index++)
            pointers[index] = source[index] with { AxisValues = ReadOnlyMemory<float>.Empty };

        return input with
        {
            Pointers = pointers,
            HistorySampleCount = 0,
            HistoricalSamples = ReadOnlyMemory<AndroidInputHistorySampleInfo>.Empty,
            FullDataIncluded = false,
        };
    }

    /// <summary>Drain Java input on the ImGui render thread, never on Activity dispatch.</summary>
    internal static void DispatchPendingImGuiEvents()
    {
        int pendingAtFrameStart = Math.Max(0, Volatile.Read(ref s_queuedImGuiEventCount));
        while (pendingAtFrameStart-- > 0
            && ImGuiInputQueue.TryDequeue(out AndroidInputEventInfo input))
        {
            Interlocked.Decrement(ref s_queuedImGuiEventCount);
            try
            {
                ImGuiInputHandler.DispatchJavaInput(input);
            }
            catch (Exception exception)
            {
                Logger.Error(nameof(AndroidJavaInputBridge),
                    $"Failed to apply queued ImGui input: {exception}");
            }
        }
    }
}
