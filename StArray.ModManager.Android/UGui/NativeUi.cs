using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using static StArray.ModManager.Il2Cpp.Il2CppFunctions;

namespace StArray.ModManager.Android.UGui;

/// <summary>A class identity from the target game's Unity assemblies.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
public sealed class UiTypeAttribute(string image, string namespaze, string name) : Attribute
{
    public string Image { get; } = image;
    public string Namespace { get; } = namespaze;
    public string Name { get; } = name;
}

/// <summary>A CoreCLR reference rooted in the game's IL2CPP heap.</summary>
public abstract class NativeUiObject
{
    private readonly UiRoot _root;
    protected NativeUiObject(nint pointer) => _root = new UiRoot(pointer);
    public nint Pointer => _root.Target;
}

internal sealed class UiRoot : SafeHandle
{
    internal UiRoot(nint pointer, bool weak = false) : base(0, ownsHandle: true)
    {
        if (pointer == 0) throw new ArgumentNullException(nameof(pointer));
        SetHandle(weak ? il2cpp_gchandle_new_weakref(pointer, false) : il2cpp_gchandle_new(pointer, false));
        if (IsInvalid) throw new InvalidOperationException("IL2CPP did not create a UGUI object handle.");
    }

    public override bool IsInvalid => handle == 0;
    internal nint Target => IsClosed ? 0 : il2cpp_gchandle_get_target(handle);

    protected override bool ReleaseHandle()
    {
        il2cpp_gchandle_free(handle);
        return true;
    }
}

/// <summary>Resolves native types and translates Unity values without loading game assemblies into CoreCLR.</summary>
public static unsafe class NativeUi
{
    private static readonly ConditionalWeakTable<Type, Il2CppClass> Classes = new();
    private static readonly ConditionalWeakTable<Type, UiRoot> TypeObjects = new();
    private static readonly ConcurrentDictionary<(string Image, string Namespace, string Name), Il2CppClass> NativeClasses = new();
    private static readonly ConcurrentDictionary<nint, MethodIndex> Methods = new();
    private static readonly ConcurrentDictionary<nint, string> TypeNames = new();
    private static readonly ConcurrentDictionary<(nint Class, string Name), Il2CppField> Fields = new();

    public static Il2CppClass Class(Type type) => Classes.GetValue(type, static t =>
    {
        var id = t.GetCustomAttribute<UiTypeAttribute>()
            ?? throw new ArgumentException($"{t} has no native UGUI type identity.");
        return FindClass(id.Image, id.Namespace, id.Name);
    });

    internal static Il2CppClass FindClass(string image, string namespaze, string name)
    {
        return NativeClasses.GetOrAdd((image, namespaze, name), static key =>
            Il2CppAssembly.Get(key.Image)?.GetClass(key.Namespace, key.Name)
            ?? throw new TypeLoadException($"Missing native type {key.Image}:{key.Namespace}.{key.Name}."));
    }

    public static nint TypeObject(Type type) =>
        TypeObjects.GetValue(type, static key => new UiRoot(Class(key).GetTypeObject())).Target;

    public static T? Wrap<T>(nint pointer) where T : NativeUiObject =>
        pointer == 0 ? null : Factory<T>.Create(pointer);

    public static T? Cast<T>(NativeUiObject? value) where T : NativeUiObject
    {
        if (value is T typed) return typed;
        if (value == null) return null;
        nint ptr = value.Pointer;
        var result = il2cpp_class_is_assignable_from(Class(typeof(T)).Ptr, il2cpp_object_get_class(ptr))
            ? Wrap<T>(ptr) : null;
        GC.KeepAlive(value);
        return result;
    }

    private static class Factory<T> where T : NativeUiObject
    {
        private static readonly ConstructorInvoker Constructor = ConstructorInvoker.Create(
            typeof(T).GetConstructor([typeof(nint)])
            ?? throw new MissingMethodException(typeof(T).FullName, ".ctor(nint)"));

        internal static T Create(nint pointer)
        {
            object argument = pointer;
            try { return (T)Constructor.Invoke(argument); }
            catch (Exception exception) when (!typeof(T).IsAbstract)
            {
                throw new TargetInvocationException(exception);
            }
        }
    }

    public static T Unbox<T>(nint boxed) where T : unmanaged
    {
        if (boxed == 0) throw new InvalidOperationException($"Unity returned null for {typeof(T)}.");
        return *(T*)il2cpp_object_unbox(boxed);
    }

    public static string? ReadString(nint pointer) => pointer == 0 ? null :
        new string(il2cpp_string_chars(pointer), 0, il2cpp_string_length(pointer));

    internal static nint WriteString(string value)
    {
        fixed (char* chars = value)
            return il2cpp_string_new_utf16(chars, value.Length);
    }

    internal static nint ArrayData(nint pointer) => pointer + 4 * nint.Size;

    public static T[] ReadValues<T>(nint pointer) where T : unmanaged => pointer == 0 ? [] :
        new ReadOnlySpan<T>((void*)ArrayData(pointer), checked((int)il2cpp_array_length(pointer))).ToArray();

    public static T[] ReadObjects<T>(nint pointer) where T : NativeUiObject
    {
        if (pointer == 0) return [];
        using var root = new UiRoot(pointer);
        var items = new ReadOnlySpan<nint>((void*)ArrayData(pointer), checked((int)il2cpp_array_length(pointer)));
        var result = new T[items.Length];
        for (int i = 0; i < items.Length; i++) result[i] = Wrap<T>(items[i])!;
        return result;
    }

    internal static Il2CppMethod Resolve(Il2CppClass klass, string name, IReadOnlyList<string> signature)
    {
        for (nint type = klass.Ptr; type != 0;)
        {
            var index = Methods.GetOrAdd(type, static pointer => new MethodIndex(pointer));
            type = index.Parent;
            if (!index.Declarations.TryGetValue(name, out var declarations)) continue;

            nint match = 0;
            foreach (nint method in declarations)
            {
                if (il2cpp_method_get_param_count(method) != signature.Count || il2cpp_method_is_generic(method))
                    continue;
                bool matches = true;
                for (uint i = 0; i < signature.Count && matches; i++)
                    matches = ReadTypeName(il2cpp_method_get_param(method, i)) == signature[(int)i];
                if (!matches) continue;
                if (match != 0) throw new AmbiguousMatchException($"{klass.Namespace}.{klass.Name}::{name}");
                match = method;
            }
            if (match != 0) return new Il2CppMethod(match);
        }
        throw new MissingMethodException($"{klass.Namespace}.{klass.Name}", $"{name}({string.Join(", ", signature)})");
    }

    private sealed class MethodIndex
    {
        internal nint Parent { get; }
        internal Dictionary<string, List<nint>> Declarations { get; } = new(StringComparer.Ordinal);

        internal MethodIndex(nint type)
        {
            Parent = il2cpp_class_get_parent(type);
            nint iterator = 0;
            while (il2cpp_class_get_methods(type, ref iterator) is var method && method != 0)
            {
                string name = Marshal.PtrToStringAnsi(il2cpp_method_get_name(method)) ?? "";
                if (!Declarations.TryGetValue(name, out var overloads))
                    Declarations.Add(name, overloads = []);
                overloads.Add(method);
            }
        }
    }

    internal static string ReadTypeName(nint type) => TypeNames.GetOrAdd(type, static pointer =>
    {
        nint text = il2cpp_type_get_name(pointer);
        try { return (Marshal.PtrToStringUTF8(text) ?? "").Replace('/', '.').Replace('+', '.'); }
        finally { if (text != 0) il2cpp_free(text); }
    });

    internal static Il2CppField Field(Il2CppClass klass, string name) =>
        Fields.GetOrAdd((klass.Ptr, name), static (key, owner) =>
            owner.GetField(key.Name) ?? throw new MissingFieldException(owner.Name, key.Name), klass);
}

/// <summary>One argument in the IL2CPP runtime-invoke ABI.</summary>
public unsafe struct UiArg
{
    // Largest Unity value used by the panel is ColorBlock (88 bytes).
    internal const int ValueCapacity = 96;
    [InlineArray(ValueCapacity)]
    private struct ValueBuffer { private byte _element; }
    private ValueBuffer _value;
    private int _valueSize;
    private nint _pointer;
    private NativeUiObject? _object;
    private UiRoot? _temporary;

    public static UiArg Value<T>(T value) where T : unmanaged
    {
        if (sizeof(T) > ValueCapacity) throw new ArgumentOutOfRangeException(nameof(value));
        UiArg arg = new() { _valueSize = sizeof(T) };
        Unsafe.WriteUnaligned(ref arg._value[0], value);
        return arg;
    }

    public static UiArg Reference(NativeUiObject? value) => new() { _object = value, _pointer = value?.Pointer ?? 0 };
    public static UiArg Pointer(nint pointer) => new() { _pointer = pointer };
    public static UiArg String(string? value) => value == null ? default : Temporary(NativeUi.WriteString(value));
    public static UiArg Type(Type? type) => type == null ? default : Pointer(NativeUi.TypeObject(type));

    public static UiArg Values<T>(T[]? values) where T : unmanaged
    {
        if (values == null) return default;
        var element = typeof(T).IsPrimitive ? NativeUi.FindClass("mscorlib", "System", typeof(T).Name) : NativeUi.Class(typeof(T));
        uint alignment = 0;
        if (il2cpp_class_value_size(element.Ptr, ref alignment) != sizeof(T))
            throw new InvalidOperationException($"UGUI array element layout mismatch for {typeof(T)}.");
        var arg = Temporary(il2cpp_array_new(element.Ptr, (ulong)values.Length));
        values.AsSpan().CopyTo(new Span<T>((void*)NativeUi.ArrayData(arg._pointer), values.Length));
        return arg;
    }

    public static UiArg Types(Type[]? types)
    {
        if (types == null) return default;
        var arg = Temporary(il2cpp_array_new(NativeUi.FindClass("mscorlib", "System", "Type").Ptr, (ulong)types.Length));
        for (int i = 0; i < types.Length; i++)
            il2cpp_gc_wbarrier_set_field(arg._pointer, NativeUi.ArrayData(arg._pointer) + i * nint.Size, NativeUi.TypeObject(types[i]));
        return arg;
    }

    private static UiArg Temporary(nint ptr) => new() { _pointer = ptr, _temporary = new UiRoot(ptr) };
    internal readonly nint GetPointer(byte* value)
    {
        if (_valueSize == 0) return _pointer;
        _value[.._valueSize].CopyTo(new Span<byte>(value, _valueSize));
        return (nint)value;
    }

    internal readonly void Release()
    {
        _temporary?.Dispose();
        GC.KeepAlive(_object);
    }
}

/// <summary>A lazily resolved method with an exact Android Unity parameter signature.</summary>
public sealed unsafe class UiMethod(Type owner, string name, bool virtualDispatch, params string[] signature)
{
    private const int StackArgumentLimit = 8;
    private static readonly ConcurrentDictionary<(nint Class, nint Method), Il2CppMethod> VirtualMethods = new();
    private MethodBinding? _binding;

    public nint Invoke(NativeUiObject? instance, params UiArg[] args)
        => Invoke(instance, args.AsSpan());

    internal nint Invoke(NativeUiObject? instance, params ReadOnlySpan<UiArg> args)
    {
        try { return InvokeRaw(instance?.Pointer ?? 0, args); }
        finally { GC.KeepAlive(instance); }
    }

    public nint InvokeValue<T>(ref T instance, params UiArg[] args) where T : unmanaged
    {
        fixed (T* ptr = &instance) return InvokeRaw((nint)ptr, args);
    }

    public nint Create(params UiArg[] args)
        => Create(args.AsSpan());

    internal nint Create(params ReadOnlySpan<UiArg> args)
    {
        var klass = NativeUi.Class(owner);
        nint instance = klass.New();
        using var root = new UiRoot(instance);
        InvokeRaw(instance, args);
        return instance;
    }

    private MethodBinding Bind()
    {
        var method = NativeUi.Resolve(NativeUi.Class(owner), name, signature);
        uint implementationFlags = 0;
        bool useVirtual = virtualDispatch &&
            (il2cpp_method_get_flags(method.Ptr, ref implementationFlags) & (uint)MethodAttributes.Virtual) != 0;
        return new MethodBinding(method, useVirtual);
    }

    private sealed class MethodBinding(Il2CppMethod method, bool useVirtual)
    {
        internal Il2CppMethod Method { get; } = method;
        internal bool UseVirtual { get; } = useVirtual;
    }

    private nint InvokeRaw(nint instance, ReadOnlySpan<UiArg> args)
    {
        try
        {
            NativeUiSession.RequireCurrent();
            var binding = _binding ??= Bind();
            var method = binding.Method;
            if (binding.UseVirtual && instance != 0)
            {
                nint runtimeClass = il2cpp_object_get_class(instance);
                method = VirtualMethods.GetOrAdd((runtimeClass, method.Ptr), static (key, target) =>
                {
                    nint slot = il2cpp_object_get_virtual_method(target, key.Method);
                    if (slot == 0) throw new MissingMethodException($"No virtual implementation for IL2CPP method 0x{key.Method:X}.");
                    return new Il2CppMethod(slot);
                }, instance);
            }
            if (args.Length == 0) return method.Invoke(instance);

            int bufferSize = checked(UiArg.ValueCapacity * args.Length);
            Span<byte> buffer = args.Length <= StackArgumentLimit ? stackalloc byte[bufferSize] : new byte[bufferSize];
            Span<nint> native = args.Length <= StackArgumentLimit ? stackalloc nint[args.Length] : new nint[args.Length];
            fixed (byte* values = buffer)
            {
                for (int i = 0; i < args.Length; i++)
                    native[i] = args[i].GetPointer(values + UiArg.ValueCapacity * i);
                return method.Invoke(instance, native);
            }
        }
        finally
        {
            foreach (ref readonly var arg in args) arg.Release();
        }
    }
}
