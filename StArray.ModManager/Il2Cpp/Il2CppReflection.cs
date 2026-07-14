using System.Runtime.InteropServices;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Il2Cpp;

/// <summary>反射 API：Assembly → Class → Method/Field</summary>
public unsafe class Il2CppAssembly : IRuntimeAssembly
{
    public nint Ptr { get; }
    public Il2CppAssembly(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string Name => Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_image_get_name(
        Il2CppFunctions.il2cpp_assembly_get_image(Ptr))) ?? "";

    public Il2CppClass? GetClass(string namespaze, string name)
    {
        var img = Il2CppFunctions.il2cpp_assembly_get_image(Ptr);
        var k = Il2CppFunctions.il2cpp_class_from_name(img, namespaze, name);
        return k != 0 ? new Il2CppClass(k) : null;
    }

    IRuntimeClass? IRuntimeAssembly.GetClass(string namespaze, string name)
        => GetClass(namespaze, name);

    public static Il2CppAssembly? Get(string name)
    {
        var domain = Il2CppFunctions.il2cpp_domain_get();
        if (domain == 0) return null;
        var nameObj = Il2CppFunctions.il2cpp_string_new(name);
        var asm = Il2CppFunctions.il2cpp_domain_assembly_open(domain, nameObj);
        return asm != 0 ? new Il2CppAssembly(asm) : null;
    }
}

public unsafe class Il2CppClass : IRuntimeClass
{
    public nint Ptr { get; }
    public Il2CppClass(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string Name => Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(Ptr)) ?? "";
    public string Namespace => Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_namespace(Ptr)) ?? "";

    // ──── Methods ────

    public Il2CppMethod? GetMethod(string name, int paramCount)
    {
        var m = Il2CppFunctions.il2cpp_class_get_method_from_name(Ptr, name, paramCount);
        return m != 0 ? new Il2CppMethod(m) : null;
    }

    IRuntimeMethod? IRuntimeClass.GetMethod(string name, int paramCount)
        => GetMethod(name, paramCount);

    public Il2CppMethod? GetMethod(string name, params string[] paramTypes)
    {
        nint iter = 0;
        nint m;
        while ((m = Il2CppFunctions.il2cpp_class_get_methods(Ptr, ref iter)) != 0)
        {
            var namePtr = Il2CppFunctions.il2cpp_method_get_name(m);
            var mName = Marshal.PtrToStringAnsi(namePtr);
            if (mName != name) continue;

            uint pc = Il2CppFunctions.il2cpp_method_get_param_count(m);
            if (pc != paramTypes.Length) continue;

            bool match = true;
            for (uint i = 0; i < pc; i++)
            {
                var pt = Il2CppFunctions.il2cpp_method_get_param(m, i);
                var ptName = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(pt));
                if (ptName != paramTypes[i]) { match = false; break; }
            }
            if (match) return new Il2CppMethod(m);
        }
        return null;
    }

    IRuntimeMethod? IRuntimeClass.GetMethod(string name, params string[] paramTypes)
        => GetMethod(name, paramTypes);

    // ──── Fields ────

    public Il2CppField? GetField(string name)
    {
        var f = Il2CppFunctions.il2cpp_class_get_field_from_name(Ptr, name);
        return f != 0 ? new Il2CppField(f) : null;
    }

    IRuntimeField? IRuntimeClass.GetField(string name)
        => GetField(name);

    public IEnumerable<Il2CppField> GetFields()
    {
        nint iter = 0;
        nint f;
        while ((f = Il2CppFunctions.il2cpp_class_get_fields(Ptr, ref iter)) != 0)
            yield return new Il2CppField(f);
    }

    // ──── Type / Instance ────

    public nint GetTypeObject()
    {
        var t = Il2CppFunctions.il2cpp_class_get_type(Ptr);
        return Il2CppFunctions.il2cpp_type_get_object(t);
    }

    public Il2CppObject New() =>
        new(Il2CppFunctions.il2cpp_object_new(Ptr));

    public bool IsGeneric => Il2CppFunctions.il2cpp_class_is_generic(Ptr);
    public bool IsInflated => Il2CppFunctions.il2cpp_class_is_inflated(Ptr);

    /// <summary>实例化泛型类。例如 GetGeneric("System.Collections.Generic.List`1", "System.Int32")</summary>
    public static Il2CppClass? GetGeneric(string genericTypeDef, params string[] typeArgs)
    {
        var corlib = Il2CppFunctions.il2cpp_get_corlib();
        var image = Il2CppFunctions.il2cpp_assembly_get_image(
            Il2CppFunctions.il2cpp_image_get_assembly(corlib));
        // 构造泛型实例全名：List`1[System.Int32]
        var args = string.Join(",", typeArgs);
        var fullName = $"{genericTypeDef}[{args}]";
        // il2cpp 内部会将此解析为泛型实例
        var k = Il2CppFunctions.il2cpp_class_from_name(image, "", fullName);
        return k != 0 ? new Il2CppClass(k) : null;
    }
}

public unsafe class Il2CppMethod : IRuntimeMethod
{
    public nint Ptr { get; }
    public Il2CppMethod(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string Name => Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_method_get_name(Ptr)) ?? "";
    public bool IsStatic { get { uint f = 0; return (Il2CppFunctions.il2cpp_method_get_flags(Ptr, ref f) & 0x10) != 0; } }
    public uint ParamCount => Il2CppFunctions.il2cpp_method_get_param_count(Ptr);

    public string GetParamName(uint index)
    {
        var p = Il2CppFunctions.il2cpp_method_get_param_name(Ptr, index);
        return Marshal.PtrToStringAnsi(p) ?? "";
    }

    public string GetParamTypeName(uint index)
    {
        var t = Il2CppFunctions.il2cpp_method_get_param(Ptr, index);
        return t != 0 ? Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(t)) ?? "" : "";
    }

    public string ReturnTypeName
    {
        get
        {
            var t = Il2CppFunctions.il2cpp_method_get_return_type(Ptr);
            return t != 0 ? Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(t)) ?? "" : "";
        }
    }

    public nint FunctionPtr => Ptr;

    public unsafe nint Invoke(nint obj, nint[]? args = null)
    {
        nint exc = 0;
        fixed (nint* p = args)
            return Il2CppFunctions.il2cpp_runtime_invoke(Ptr, obj, (void**)p, ref exc);
    }

    public nint InvokeStatic(nint[]? args = null)
        => Invoke(0, args);

    public unsafe T InvokeUnbox<T>(nint obj, nint[]? args = null) where T : unmanaged
    {
        nint ret = Invoke(obj, args);
        if (ret == 0) return default;
        nint unboxed = Il2CppFunctions.il2cpp_object_unbox(ret);
        if (unboxed == 0) return default;
        return *(T*)unboxed;
    }

    public unsafe T InvokeStaticUnbox<T>(nint[]? args = null) where T : unmanaged
        => InvokeUnbox<T>(0, args);

    private static unsafe nint* GetArgPtr(nint[]? args)
    {
        if (args == null || args.Length == 0) return null;
        var ptr = (nint*)Marshal.AllocHGlobal(args.Length * nint.Size);
        for (int i = 0; i < args.Length; i++) ptr[i] = args[i];
        return ptr;
    }
}

public unsafe class Il2CppField : IRuntimeField
{
    public nint Ptr { get; }
    public Il2CppField(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string Name => Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_field_get_name(Ptr)) ?? "";
    public uint Offset => Il2CppFunctions.il2cpp_field_get_offset(Ptr);
    public bool IsStatic => (Il2CppFunctions.il2cpp_field_get_flags(Ptr) & 0x10) != 0;

    public string TypeName
    {
        get
        {
            var t = Il2CppFunctions.il2cpp_field_get_type(Ptr);
            return t != 0 ? Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(t)) ?? "" : "";
        }
    }

    public unsafe T GetValue<T>(nint obj) where T : unmanaged
    {
        if (IsStatic)
        {
            T val = default;
            Il2CppFunctions.il2cpp_field_static_get_value(Ptr, &val);
            return val;
        }
        return *(T*)(obj + Offset);
    }

    public unsafe void SetValue<T>(nint obj, T value) where T : unmanaged
    {
        if (IsStatic)
            Il2CppFunctions.il2cpp_field_static_set_value(Ptr, &value);
        else
            *(T*)(obj + Offset) = value;
    }

    public nint GetObjectValue(nint obj) => Il2CppFunctions.il2cpp_field_get_value_object(Ptr, obj);
}

/// 自定义类工厂 Native il2cpp_class_new  
public static class ClassFactory
{
    [DllImport("modmanager", EntryPoint = "modmanager_class_create")]
    public static extern nint Create(
        [MarshalAs(UnmanagedType.LPStr)] string assemblyName,
        [MarshalAs(UnmanagedType.LPStr)] string namespaze,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        [MarshalAs(UnmanagedType.LPStr)] string parentName);

    /// 创建自定义 MonoBehaviour 子类
    public static Il2CppClass? CreateMonoBehaviour(string assemblyName, string namespaze, string name)
    {
        var ptr = Create(assemblyName, namespaze, name, "UnityEngine.MonoBehaviour");
        return ptr != 0 ? new Il2CppClass(ptr) : null;
    }
}
