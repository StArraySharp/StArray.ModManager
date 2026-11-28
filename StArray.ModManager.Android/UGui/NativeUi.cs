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

    public static Il2CppClass Class(Type type) => Classes.GetValue(type, static t =>
    {
        var id = t.GetCustomAttribute<UiTypeAttribute>()
            ?? throw new ArgumentException($"{t} has no native UGUI type identity.");
        return FindClass(id.Image, id.Namespace, id.Name);
    });

    internal static Il2CppClass FindClass(string image, string namespaze, string name)
    {
        var klass = Il2CppAssembly.Get(image)?.GetClass(namespaze, name);
        return klass ?? throw new TypeLoadException($"Missing native type {image}:{namespaze}.{name}.");
    }

    public static nint TypeObject(Type type) => Class(type).GetTypeObject();

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
        private static readonly ConstructorInfo Constructor = typeof(T).GetConstructor([typeof(nint)])
            ?? throw new MissingMethodException(typeof(T).FullName, ".ctor(nint)");

        internal static T Create(nint pointer) => (T)Constructor.Invoke([pointer]);
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
        // Enumerate declarations in order so a derived Unity property wins over a hidden base property.
        string owner = $"{klass.Namespace}.{klass.Name}";
        for (nint type = klass.Ptr; type != 0; type = il2cpp_class_get_parent(type))
        {
            Il2CppMethod? match = null;
            nint iterator = 0;
            while (il2cpp_class_get_methods(type, ref iterator) is var ptr && ptr != 0)
            {
                var candidate = new Il2CppMethod(ptr);
                if (candidate.ParamCount != signature.Count || candidate.Name != name
                    || il2cpp_method_is_generic(ptr)) continue;
                bool matches = true;
                for (uint i = 0; i < signature.Count && matches; i++)
                    matches = ReadTypeName(il2cpp_method_get_param(ptr, i)) == signature[(int)i];
                if (!matches) continue;
                if (match != null) throw new AmbiguousMatchException($"{owner}::{name}");
                match = candidate;
            }
            if (match != null) return match;
        }
        throw new MissingMethodException(owner, $"{name}({string.Join(", ", signature)})");
    }

    internal static string ReadTypeName(nint type)
    {
        nint text = il2cpp_type_get_name(type);
        try { return (Marshal.PtrToStringUTF8(text) ?? "").Replace('/', '.').Replace('+', '.'); }
        finally { if (text != 0) il2cpp_free(text); }
    }

    internal static Il2CppField Field(Il2CppClass klass, string name) => klass.GetField(name)
        ?? throw new MissingFieldException(klass.Name, name);
}

/// <summary>One argument in the IL2CPP runtime-invoke ABI.</summary>
public unsafe struct UiArg
{
    // Largest Unity value used by the panel is ColorBlock (88 bytes).
    [InlineArray(96)]
    private struct ValueBuffer { private byte _element; }
    private ValueBuffer _value;
    private bool _inline;
    private nint _pointer;
    private NativeUiObject? _object;
    private UiRoot? _temporary;

    public static UiArg Value<T>(T value) where T : unmanaged
    {
        if (sizeof(T) > 96) throw new ArgumentOutOfRangeException(nameof(value));
        UiArg arg = new() { _inline = true };
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
    internal nint GetPointer(byte* value)
    {
        if (!_inline) return _pointer;
        _value[..].CopyTo(new Span<byte>(value, 96));
        return (nint)value;
    }

    internal void Release()
    {
        _temporary?.Dispose();
        GC.KeepAlive(_object);
    }
}

/// <summary>A lazily resolved method with an exact Android Unity parameter signature.</summary>
public sealed unsafe class UiMethod(Type owner, string name, bool virtualDispatch, params string[] signature)
{
    private Il2CppMethod? _method;

    public nint Invoke(NativeUiObject? instance, params UiArg[] args)
    {
        try { return InvokeRaw(instance?.Pointer ?? 0, args); }
        finally { GC.KeepAlive(instance); }
    }

    public nint InvokeValue<T>(ref T instance, params UiArg[] args) where T : unmanaged
    {
        fixed (T* ptr = &instance) return InvokeRaw((nint)ptr, args);
    }

    public nint Create(params UiArg[] args)
    {
        var klass = NativeUi.Class(owner);
        nint instance = klass.New();
        using var root = new UiRoot(instance);
        InvokeRaw(instance, args);
        return instance;
    }

    private nint InvokeRaw(nint instance, UiArg[] args)
    {
        try
        {
            NativeUiSession.RequireCurrent();
            var method = _method ??= NativeUi.Resolve(NativeUi.Class(owner), name, signature);
            if (virtualDispatch && instance != 0)
            {
                nint slot = il2cpp_object_get_virtual_method(instance, method.Ptr);
                if (slot == 0) throw new MissingMethodException(owner.FullName, name);
                method = new Il2CppMethod(slot);
            }
            byte* buffer = stackalloc byte[96 * args.Length];
            var native = new nint[args.Length];
            for (int i = 0; i < args.Length; i++) native[i] = args[i].GetPointer(buffer + 96 * i);
            return method.Invoke(instance, native);
        }
        finally
        {
            foreach (ref var arg in args.AsSpan()) arg.Release();
        }
    }
}
