using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

public readonly unsafe struct RuntimeObject
{
    public nint Ptr { get; }
    public bool IsValid => Ptr != 0;

    public RuntimeObject(nint ptr) => Ptr = ptr;

    private nint GetClassPtr()
    {
        return RuntimeManager.Backend switch
        {
            RuntimeBackend.Il2Cpp => Il2CppFunctions.il2cpp_object_get_class(Ptr),
            RuntimeBackend.Mono => MonoFunctions.MonoObjectGetClass(Ptr),
            _ => 0,
        };
    }

    // ── Method invocation ──

    public nint Invoke(string methodName, int paramCount, nint[]? args = null)
    {
        var klass = GetClassPtr();
        if (klass == 0) return 0;

        if (RuntimeManager.IsIl2Cpp)
        {
            var method = Il2CppFunctions.il2cpp_class_get_method_from_name(klass, methodName, paramCount);
            if (method == 0) return 0;
            nint exc = 0;
            fixed (nint* p = args)
                return Il2CppFunctions.il2cpp_runtime_invoke(method, Ptr, (void**)p, ref exc);
        }

        if (RuntimeManager.IsMono)
        {
            var method = MonoFunctions.MonoClassGetMethodFromName(klass, methodName, paramCount);
            if (method == 0) return 0;
            nint exc = 0;
            return MonoFunctions.MonoRuntimeInvoke(method, Ptr, args, out exc);
        }

        return 0;
    }

    public nint Invoke(string methodName, nint[]? args = null) => Invoke(methodName, args?.Length ?? 0, args);

    public void InvokeVoid(string methodName, int paramCount = 0, nint[]? args = null) => Invoke(methodName, paramCount, args);

    public T InvokeUnbox<T>(string methodName, int paramCount = 0, nint[]? args = null) where T : unmanaged
    {
        var ret = Invoke(methodName, paramCount, args);
        if (ret == 0) return default;
        if (RuntimeManager.IsIl2Cpp) return *(T*)Il2CppFunctions.il2cpp_object_unbox(ret);
        if (RuntimeManager.IsMono) return *(T*)MonoFunctions.MonoObjectUnbox(ret);
        return default;
    }

    public RuntimeObject? InvokeObject(string methodName, int paramCount = 0, nint[]? args = null)
    {
        var ret = Invoke(methodName, paramCount, args);
        return ret != 0 ? new RuntimeObject(ret) : null;
    }

    // ── Field access (by name) ──

    public T GetField<T>(string fieldName) where T : unmanaged
    {
        var klass = GetClassPtr();
        if (klass == 0) return default;

        if (RuntimeManager.IsIl2Cpp)
        {
            var field = Il2CppFunctions.il2cpp_class_get_field_from_name(klass, fieldName);
            if (field == 0) return default;
            var offset = Il2CppFunctions.il2cpp_field_get_offset(field);
            return *(T*)(Ptr + offset);
        }

        if (RuntimeManager.IsMono)
        {
            var field = MonoFunctions.MonoClassGetFieldFromName(klass, fieldName);
            if (field == 0) return default;
            var offset = MonoFunctions.MonoFieldGetOffset(field);
            return *(T*)(Ptr + offset);
        }

        return default;
    }

    public void SetField<T>(string fieldName, T value) where T : unmanaged
    {
        var klass = GetClassPtr();
        if (klass == 0) return;

        if (RuntimeManager.IsIl2Cpp)
        {
            var field = Il2CppFunctions.il2cpp_class_get_field_from_name(klass, fieldName);
            if (field == 0) return;
            var offset = Il2CppFunctions.il2cpp_field_get_offset(field);
            *(T*)(Ptr + offset) = value;
        }

        if (RuntimeManager.IsMono)
        {
            var field = MonoFunctions.MonoClassGetFieldFromName(klass, fieldName);
            if (field == 0) return;
            var offset = MonoFunctions.MonoFieldGetOffset(field);
            *(T*)(Ptr + offset) = value;
        }
    }

    // ── Field access (via IRuntimeField) ──

    public T GetField<T>(IRuntimeField field) where T : unmanaged => field.GetValue<T>(Ptr);
    public void SetField<T>(IRuntimeField field, T value) where T : unmanaged => field.SetValue(Ptr, value);

    // ── Factory ──

    public static RuntimeObject? New(string assembly, string ns, string className)
    {
        var domain = RuntimeManager.GetDomain();
        return domain != null ? New(domain, assembly, ns, className) : null;
    }

    public static RuntimeObject? New(IAppDomain domain, string assembly, string ns, string className)
    {
        var asm = domain.OpenAssembly(assembly);
        if (asm == null) return null;
        var cls = asm.GetClass(ns, className);
        if (cls == null) return null;
        var ptr = cls.New();
        return ptr != 0 ? new RuntimeObject(ptr) : null;
    }

    // ── Utility ──

    public override string ToString()
    {
        var obj = InvokeObject("ToString", 0);
        if (obj == null || obj.Value.Ptr == 0) return $"RuntimeObject(0x{Ptr:X})";
        return new RuntimeString(obj.Value.Ptr).ToString();
    }

    // ── Indexer: field access by name ──

    public nint this[string fieldName]
    {
        readonly get => GetField<nint>(fieldName);
        set => SetField(fieldName, value);
    }

    // ── Marshalling helpers ──

    public static implicit operator nint(RuntimeObject obj) => obj.Ptr;
    public static implicit operator RuntimeObject(nint ptr) => new(ptr);
}

/// <summary>
/// 类型�?RuntimeObject —�?通过 <see cref="GetInstance"/> 创建 <typeparamref name="T"/> 实例�?/// T 需继承 <see cref="UnmanagedObject"/> 并实�?<c>T(nint ptr)</c> 构造函数�?/// </summary>
public readonly unsafe struct RuntimeObject<T> where T : UnmanagedObject
{
    public nint Ptr { get; }

    public RuntimeObject(nint ptr) => Ptr = ptr;
    public RuntimeObject(RuntimeObject obj) => Ptr = obj.Ptr;
    public bool IsValid => Ptr != 0;

    /// <summary>通过构造函数创�?T 实例。若指针指向静态类则抛异常�?/summary>
    public T GetInstance()
    {
        if (Ptr == 0)
            throw new InvalidOperationException($"Cannot create instance of {typeof(T).Name}: pointer is null.");
        if (IsStaticClass())
            throw new InvalidOperationException($"Cannot create instance of {typeof(T).Name}: the underlying class is static.");
        return (T)Activator.CreateInstance(typeof(T), Ptr)!;
    }

    /// <summary>检查当前指针对应的运行时类型是否为静态类（abstract + sealed）�?/summary>
    private bool IsStaticClass()
    {
        var klass = GetClassPtr();
        if (klass == 0) return false;

        const int ABSTRACT = 0x80;
        const int SEALED = 0x100;

        if (RuntimeManager.IsIl2Cpp)
        {
            var flags = Il2CppFunctions.il2cpp_class_get_flags(klass);
            return (flags & (ABSTRACT | SEALED)) == (ABSTRACT | SEALED);
        }

        if (RuntimeManager.IsMono)
        {
            var flags = MonoFunctions.MonoClassGetFlags(klass);
            return (flags & (ABSTRACT | SEALED)) == (ABSTRACT | SEALED);
        }

        return false;
    }

    private nint GetClassPtr()
    {
        if (Ptr == 0) return 0;
        return RuntimeManager.Backend switch
        {
            RuntimeBackend.Il2Cpp => Il2CppFunctions.il2cpp_object_get_class(Ptr),
            RuntimeBackend.Mono => MonoFunctions.MonoObjectGetClass(Ptr),
            _ => 0,
        };
    }

    public RuntimeObject AsRuntimeObject() => new(Ptr);

    // ── Method invocation ──

    public nint Invoke(string methodName, int paramCount, nint[]? args = null)
        => AsRuntimeObject().Invoke(methodName, paramCount, args);
    public nint Invoke(string methodName, nint[]? args = null)
        => AsRuntimeObject().Invoke(methodName, args);
    public void InvokeVoid(string methodName, int paramCount = 0, nint[]? args = null)
        => AsRuntimeObject().InvokeVoid(methodName, paramCount, args);
    public TRet InvokeUnbox<TRet>(string methodName, int paramCount = 0, nint[]? args = null) where TRet : unmanaged
        => AsRuntimeObject().InvokeUnbox<TRet>(methodName, paramCount, args);
    public RuntimeObject? InvokeObject(string methodName, int paramCount = 0, nint[]? args = null)
        => AsRuntimeObject().InvokeObject(methodName, paramCount, args);

    // ── Field access ──

    public TField GetField<TField>(string fieldName) where TField : unmanaged
        => AsRuntimeObject().GetField<TField>(fieldName);
    public void SetField<TField>(string fieldName, TField value) where TField : unmanaged
        => AsRuntimeObject().SetField(fieldName, value);
    public TField GetField<TField>(IRuntimeField field) where TField : unmanaged
        => AsRuntimeObject().GetField<TField>(field);
    public void SetField<TField>(IRuntimeField field, TField value) where TField : unmanaged
        => AsRuntimeObject().SetField(field, value);

    // ── Indexer ──

    public nint this[string fieldName]
    {
        readonly get => AsRuntimeObject()[fieldName];
        set
        {
            var asRuntimeObject = AsRuntimeObject();
            asRuntimeObject[fieldName] = value;
        }
    }

    // ── Conversions ──

    public static implicit operator RuntimeObject(RuntimeObject<T> obj) => new(obj.Ptr);
    public static implicit operator RuntimeObject<T>(RuntimeObject obj) => new(obj);
    public static implicit operator RuntimeObject<T>(nint ptr) => new(ptr);
    public static implicit operator nint(RuntimeObject<T> obj) => obj.Ptr;
}
