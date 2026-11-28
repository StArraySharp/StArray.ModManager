using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using static StArray.ModManager.Il2Cpp.Il2CppFunctions;

namespace StArray.ModManager.Android.UGui;

/// <summary>Owns one native mobile window's listeners. Open, enter and dispose from UnityMain.</summary>
public sealed unsafe class NativeUiSession : IDisposable
{
    public const int ApiVersion = 1;
    [ThreadStatic] private static NativeUiSession? _current;
    private static readonly ConcurrentDictionary<nint, Listener> Callbacks = new();
    private static readonly Dictionary<nint, EventBinding> Bindings = [];
    private static readonly Dictionary<CallbackKind, nint> MethodRecords = [];
    private static Il2CppClass? _object;
    private static Il2CppMethod? _isAlive;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly List<Listener> _listeners = [];
    private Exception? _callbackError;
    private int _framesUntilPrune;
    private bool _disposed;

    private NativeUiSession() { }

    /// <summary>Opens a session from a game update callback on the Unity thread.</summary>
    public static NativeUiSession Open()
    {
        if (!OperatingSystem.IsAndroid() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            throw new PlatformNotSupportedException("Native UGUI requires Android ARM64 IL2CPP.");
        if (il2cpp_thread_current() == 0)
            throw new InvalidOperationException("Open native UGUI on UnityMain.");
        var app = NativeUi.FindClass("UnityEngine.CoreModule", "UnityEngine", "Application");
        var version = NativeUi.Resolve(app, "get_unityVersion", []).InvokeStatic();
        if (NativeUi.ReadString(version) != "6000.3.10f1")
            throw new PlatformNotSupportedException("Native UGUI currently supports Unity 6000.3.10f1.");
        VerifyMethodLayout();
        return new NativeUiSession();
    }

    private void CheckThread()
    {
        if (Environment.CurrentManagedThreadId != _threadId)
            throw new InvalidOperationException("Native UGUI session accessed outside its Unity thread.");
    }

    internal static NativeUiSession RequireCurrent() => _current ??
        throw new InvalidOperationException("Native Unity calls require an active UGUI session.");

    public Scope Enter()
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
        var previous = _current;
        _current = this;
        return new Scope(previous);
    }

    public readonly ref struct Scope
    {
        private readonly NativeUiSession? _previous;
        internal Scope(NativeUiSession? previous) => _previous = previous;
        public void Dispose() => _current = _previous;
    }

    /// <summary>Releases listeners belonging to destroyed controls and surfaces callback failures.</summary>
    public void Tick()
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_callbackError is { } error)
        {
            _callbackError = null;
            throw new InvalidOperationException("A native UGUI listener failed.", error);
        }
        if (_framesUntilPrune > 0)
        {
            _framesUntilPrune--;
            return;
        }
        _framesUntilPrune = 7; // Unity defers Destroy until the end of the frame.
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            var listener = _listeners[i];
            if (IsAlive(listener.Owner.Target)) continue;
            _listeners.RemoveAt(i);
            listener.Dispose();
        }
    }

    public void Dispose()
    {
        CheckThread();
        if (_disposed) return;
        _disposed = true;
        Exception? failure = null;
        for (int i = _listeners.Count - 1; i >= 0; i--)
        {
            try { _listeners[i].Dispose(); }
            catch (Exception ex) { failure ??= ex; }
        }
        _listeners.Clear();
        _callbackError = null;
        if (failure != null) throw new InvalidOperationException("Could not remove a Unity listener.", failure);
    }

    public static void Listen(NativeUiObject source, NativeUiObject unityEvent, Action callback)
        => RequireCurrent().Add(source.Pointer, unityEvent.Pointer, CallbackKind.Empty, _ => callback());
    public static void Listen(NativeUiObject source, NativeUiObject unityEvent, Action<bool> callback)
        => RequireCurrent().Add(source.Pointer, unityEvent.Pointer, CallbackKind.Boolean, arg => callback(arg != 0));
    public static void Listen(NativeUiObject source, NativeUiObject unityEvent, Action<float> callback)
        => RequireCurrent().Add(source.Pointer, unityEvent.Pointer, CallbackKind.Single, arg => callback(BitConverter.Int32BitsToSingle((int)arg)));
    public static void Listen(NativeUiObject source, NativeUiObject unityEvent, Action<string> callback)
        => RequireCurrent().Add(source.Pointer, unityEvent.Pointer, CallbackKind.String, arg => callback(NativeUi.ReadString(arg) ?? ""));

    public enum PointerEvent { Down = 2, Up = 3, Click = 4, Drag = 5 }

    /// <summary>Adds a native EventTrigger to an owned raycast target. Event data is valid while the callback runs.</summary>
    public static void ListenPointer(NativeUiObject gameObject, params (PointerEvent Event, Action<nint> Callback)[] handlers)
    {
        var session = RequireCurrent();
        var api = PointerBindings.Instance;
        nint owner = gameObject.Pointer;
        nint trigger = api.AddComponent.Invoke(owner, [api.Trigger.GetTypeObject()]);
        using var triggerRoot = new UiRoot(trigger);
        nint entries = api.GetTriggers.Invoke(trigger);
        using var listRoot = new UiRoot(entries);
        var add = NativeUi.Resolve(new Il2CppClass(il2cpp_object_get_class(entries)), "Add", ["UnityEngine.EventSystems.EventTrigger.Entry"]);
        foreach (var (eventType, callback) in handlers)
        {
            nint entry = api.Entry.New();
            using var entryRoot = new UiRoot(entry);
            api.InitializeEntry.Invoke(entry);
            api.EventId.SetValue(entry, (int)eventType);
            nint unityEvent = api.EntryCallback.GetObjectValue(entry);
            session.Add(owner, unityEvent, CallbackKind.EventData, callback);
            add.Invoke(entries, [entry]);
        }
        GC.KeepAlive(gameObject);
    }

    /// <summary>Reads a Unity reference field, such as Toggle.onValueChanged.</summary>
    public static nint ReadReferenceField(NativeUiObject instance, Type owner, string name)
    {
        RequireCurrent();
        nint value = NativeUi.Field(NativeUi.Class(owner), name).GetObjectValue(instance.Pointer);
        GC.KeepAlive(instance);
        return value;
    }

    private static bool IsAlive(nint pointer)
    {
        if (pointer == 0) return false;
        _isAlive ??= NativeUi.Resolve(NativeUi.FindClass("UnityEngine.CoreModule", "UnityEngine", "Object"),
            "op_Implicit", ["UnityEngine.Object"]);
        return _isAlive.InvokeStaticUnbox<bool>([pointer]);
    }

    private void Add(nint owner, nint unityEvent, CallbackKind kind, Action<nint> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (owner == 0 || unityEvent == 0) throw new InvalidOperationException("Missing UnityEvent or its owner.");
        nint eventClass = il2cpp_object_get_class(unityEvent);
        if (!Bindings.TryGetValue(eventClass, out var binding))
        {
            binding = new EventBinding(new Il2CppClass(eventClass));
            Bindings.Add(eventClass, binding);
        }
        binding.ValidateSignature(kind);
        nint target = (_object ??= NativeUi.FindClass("mscorlib", "System", "Object")).New();
        using var targetRoot = new UiRoot(target);
        nint nativeDelegate = binding.ActionClass.New();
        using var delegateRoot = new UiRoot(nativeDelegate);
        nint method = GetMethodRecord(kind);
        binding.Initialize(nativeDelegate, target, method);

        var listener = new Listener(this, callback, owner, unityEvent, target, nativeDelegate, binding);
        try
        {
            if (!Callbacks.TryAdd(target, listener))
                throw new InvalidOperationException("A native UGUI callback target was reused while active.");
            binding.Add.Invoke(unityEvent, [nativeDelegate]);
            _listeners.Add(listener);
        }
        catch
        {
            listener.Dispose();
            throw;
        }
    }

    private sealed class Listener : IDisposable
    {
        internal NativeUiSession Session { get; }
        internal Action<nint> Callback { get; }
        internal UiRoot Owner { get; }
        private readonly UiRoot _event;
        private readonly UiRoot _delegate;
        private readonly nint _target;
        private readonly EventBinding _binding;

        internal Listener(NativeUiSession session, Action<nint> callback, nint owner, nint unityEvent,
            nint target, nint nativeDelegate, EventBinding binding)
        {
            Session = session;
            Callback = callback;
            _target = target;
            _binding = binding;
            Owner = new UiRoot(owner, weak: true);
            _event = new UiRoot(unityEvent, weak: true);
            _delegate = new UiRoot(nativeDelegate);
        }

        public void Dispose()
        {
            Callbacks.TryRemove(new KeyValuePair<nint, Listener>(_target, this));
            try
            {
                nint evt = _event.Target;
                nint callback = _delegate.Target;
                if (evt != 0 && callback != 0) _binding.Remove.Invoke(evt, [callback]);
            }
            finally
            {
                _event.Dispose();
                _delegate.Dispose();
                Owner.Dispose();
            }
        }
    }

    private enum CallbackKind { Empty, Boolean, Single, String, EventData }

    private sealed class EventBinding
    {
        internal Il2CppMethod Add { get; }
        internal Il2CppMethod Remove { get; }
        internal Il2CppClass ActionClass { get; }
        private readonly Il2CppMethod _ctor;
        private readonly Il2CppMethod _invoke;
        private readonly DelegateFields _fields = DelegateFields.Instance;

        internal EventBinding(Il2CppClass eventClass)
        {
            Add = FindListenerMethod(eventClass, "AddListener");
            Remove = FindListenerMethod(eventClass, "RemoveListener");
            nint type = il2cpp_method_get_param(Add.Ptr, 0);
            nint klass = il2cpp_class_from_type(type);
            if (klass == 0 || klass != il2cpp_class_from_type(il2cpp_method_get_param(Remove.Ptr, 0)))
                throw new InvalidOperationException($"Inconsistent UnityEvent listener type for {eventClass.Name}.");
            ActionClass = new Il2CppClass(klass);
            _ctor = NativeUi.Resolve(ActionClass, ".ctor", ["System.Object", "System.IntPtr"]);
            _invoke = ActionClass.GetMethod("Invoke", ActionClass.Name == "UnityAction" ? 0 : 1)
                ?? throw new MissingMethodException(ActionClass.Name, "Invoke");
        }

        private static Il2CppMethod FindListenerMethod(Il2CppClass klass, string name)
        {
            var method = klass.GetMethod(name, 1) ?? throw new MissingMethodException(klass.Name, name);
            nint parameter = il2cpp_class_from_type(il2cpp_method_get_param(method.Ptr, 0));
            var action = new Il2CppClass(parameter);
            if (parameter == 0 || action.Namespace != "UnityEngine.Events" || action.Name is not ("UnityAction" or "UnityAction`1"))
                throw new InvalidOperationException($"Unexpected UnityEvent listener type for {klass.Name}.");
            return method;
        }

        internal void ValidateSignature(CallbackKind kind)
        {
            if (kind == CallbackKind.Empty && _invoke.ParamCount == 0) return;
            string expected = kind switch
            {
                CallbackKind.Boolean => "System.Boolean",
                CallbackKind.Single => "System.Single",
                CallbackKind.String => "System.String",
                CallbackKind.EventData => "UnityEngine.EventSystems.BaseEventData",
                _ => "",
            };
            if (_invoke.ParamCount != 1 || NativeUi.ReadTypeName(il2cpp_method_get_param(_invoke.Ptr, 0)) != expected)
                throw new InvalidOperationException($"Unsupported UnityAction signature ({kind}).");
        }

        internal void Initialize(nint instance, nint target, nint method)
        {
            _ctor.Invoke(instance, [target, (nint)(&method)]);
            nint callback = ((MethodRecord*)method)->MethodPointer;
            _fields.MethodPointer.SetValue(instance, callback);
            _fields.InvokeImpl.SetValue(instance, callback);
            _fields.MethodCode.SetValue(instance, target);
            _fields.Method.SetValue(instance, method);
            _fields.Target.SetValue(instance, target);
            _fields.MethodInfo.SetValue(instance, il2cpp_method_get_object(_invoke.Ptr, ActionClass.Ptr));
        }
    }

    private sealed class DelegateFields
    {
        internal static readonly DelegateFields Instance = new(NativeUi.FindClass("mscorlib", "System", "Delegate"));
        internal readonly Il2CppField MethodPointer, InvokeImpl, MethodCode, Method, Target, MethodInfo;

        private DelegateFields(Il2CppClass type)
        {
            MethodPointer = NativeUi.Field(type, "method_ptr");
            InvokeImpl = NativeUi.Field(type, "invoke_impl");
            MethodCode = NativeUi.Field(type, "method_code");
            Method = NativeUi.Field(type, "method");
            Target = NativeUi.Field(type, "m_target");
            MethodInfo = NativeUi.Field(type, "method_info");
        }
    }

    private sealed class PointerBindings
    {
        private const string Ui = "UnityEngine.UI";
        private const string Events = "UnityEngine.EventSystems";
        internal static readonly PointerBindings Instance = new();
        internal readonly Il2CppClass Trigger = NativeUi.FindClass(Ui, Events, "EventTrigger");
        internal readonly Il2CppClass Entry = NativeUi.FindClass(Ui, Events, "EventTrigger/Entry");
        internal readonly Il2CppMethod AddComponent, GetTriggers, InitializeEntry;
        internal readonly Il2CppField EventId, EntryCallback;

        private PointerBindings()
        {
            var gameObject = NativeUi.FindClass("UnityEngine.CoreModule", "UnityEngine", "GameObject");
            AddComponent = NativeUi.Resolve(gameObject, "AddComponent", ["System.Type"]);
            GetTriggers = NativeUi.Resolve(Trigger, "get_triggers", []);
            InitializeEntry = NativeUi.Resolve(Entry, ".ctor", []);
            EventId = NativeUi.Field(Entry, "eventID");
            EntryCallback = NativeUi.Field(Entry, "callback");
        }
    }

    // Unity 6000.3 ARM64 MethodInfo. One immutable record per signature stays valid
    // while Unity can still have a queued delegate after a window closes.
    [StructLayout(LayoutKind.Explicit, Size = 88)]
    private struct MethodRecord
    {
        [FieldOffset(0)] internal nint MethodPointer;
        [FieldOffset(72)] internal uint Token;
        [FieldOffset(80)] internal ushort Slot;
        [FieldOffset(82)] internal byte ParameterCount;
    }

    private static void VerifyMethodLayout()
    {
        MethodRecord test = new() { Token = 0x06001234, ParameterCount = 3 };
        if (il2cpp_method_get_param_count((nint)(&test)) != 3 || il2cpp_method_get_token((nint)(&test)) != test.Token)
            throw new PlatformNotSupportedException("Native UGUI MethodInfo layout mismatch.");
    }

    private static nint GetMethodRecord(CallbackKind kind)
    {
        if (MethodRecords.TryGetValue(kind, out var record)) return record;
        MethodRecord* data = (MethodRecord*)NativeMemory.AllocZeroed((nuint)sizeof(MethodRecord));
        data->Slot = ushort.MaxValue;
        data->ParameterCount = kind == CallbackKind.Empty ? (byte)0 : (byte)1;
        data->MethodPointer = kind switch
        {
            CallbackKind.Empty => (nint)(delegate* unmanaged<nint, nint, void>)&DispatchEmpty,
            CallbackKind.Boolean => (nint)(delegate* unmanaged<nint, byte, nint, void>)&DispatchBoolean,
            CallbackKind.Single => (nint)(delegate* unmanaged<nint, float, nint, void>)&DispatchSingle,
            _ => (nint)(delegate* unmanaged<nint, nint, nint, void>)&DispatchReference,
        };
        MethodRecords.Add(kind, (nint)data);
        return (nint)data;
    }

    [UnmanagedCallersOnly]
    private static void DispatchEmpty(nint target, nint method) => Dispatch(target, 0);
    [UnmanagedCallersOnly]
    private static void DispatchBoolean(nint target, byte value, nint method) => Dispatch(target, value);
    [UnmanagedCallersOnly]
    private static void DispatchSingle(nint target, float value, nint method) => Dispatch(target, BitConverter.SingleToInt32Bits(value));
    [UnmanagedCallersOnly]
    private static void DispatchReference(nint target, nint value, nint method) => Dispatch(target, value);

    private static void Dispatch(nint target, nint value)
    {
        if (!Callbacks.TryGetValue(target, out var listener)) return;
        var session = listener.Session;
        if (session._disposed || Environment.CurrentManagedThreadId != session._threadId) return;
        try
        {
            using var scope = session.Enter();
            listener.Callback(value);
        }
        catch (Exception ex) { session._callbackError ??= ex; }
    }
}
