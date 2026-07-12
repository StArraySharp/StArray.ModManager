using System.Runtime.InteropServices;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Mono;

/// <summary>反射 API：Assembly → Class → Method/Field</summary>
public unsafe class MonoAssembly : IRuntimeAssembly
{
    public nint Ptr { get; }
    public MonoAssembly(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string? Name => MonoFunctions.MonoImageGetName(MonoFunctions.MonoAssemblyGetImage(Ptr));

    public MonoImage? GetImage()
    {
        var img = MonoFunctions.MonoAssemblyGetImage(Ptr);
        return img != 0 ? new MonoImage(img) : null;
    }

    public MonoClass? GetClass(string namespaze, string name)
    {
        var img = MonoFunctions.MonoAssemblyGetImage(Ptr);
        if (img == 0) return null;
        return MonoClass.FromName(img, namespaze, name);
    }

    IRuntimeClass? IRuntimeAssembly.GetClass(string namespaze, string name)
        => GetClass(namespaze, name);

    public static MonoAssembly? Get(string name)
    {
        var domain = MonoFunctions.MonoGetRootDomain();
        if (domain == 0) return null;
        var asm = MonoFunctions.MonoDomainAssemblyOpen(domain, name);
        return asm != 0 ? new MonoAssembly(asm) : null;
    }

    public static MonoAssembly? LoadFrom(string path)
    {
        var img = MonoFunctions.MonoImageOpen(path, out _);
        if (img == 0) return null;
        var asm = MonoFunctions.MonoAssemblyLoadFrom(img, path, out _);
        return asm != 0 ? new MonoAssembly(asm) : null;
    }
}

public unsafe class MonoImage
{
    public nint Ptr { get; }
    public MonoImage(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string? Name => MonoFunctions.MonoImageGetName(Ptr);
    public string? Filename => MonoFunctions.MonoImageGetFilename(Ptr);
    public nint Assembly => MonoFunctions.MonoImageGetAssembly(Ptr);
}

public unsafe class MonoClass : IRuntimeClass
{
    public nint Ptr { get; }
    public MonoClass(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string? Name => MonoFunctions.MonoClassGetName(Ptr);
    public string? Namespace => MonoFunctions.MonoClassGetNamespace(Ptr);
    public nint Image => MonoFunctions.MonoClassGetImage(Ptr);

    public MonoMethod? GetMethod(string name, int paramCount)
        => FromMethodPtr(MonoFunctions.MonoClassGetMethodFromName(Ptr, name, paramCount));

    IRuntimeMethod? IRuntimeClass.GetMethod(string name, int paramCount)
        => GetMethod(name, paramCount);

    /// <summary>通过参数类型名称查找方法（慢路径：枚举所有方法匹配）</summary>
    public MonoMethod? GetMethod(string name, params string[] paramTypes)
    {
        nint iter = 0;
        while (true)
        {
            var m = MonoFunctions.MonoClassGetMethods(Ptr, ref iter);
            if (m == 0) break;

            var mName = MonoFunctions.MonoMethodGetName(m);
            if (mName != name) continue;

            var sig = MonoFunctions.MonoMethodSignature(m);
            if (sig == 0) continue;
            uint pc = MonoFunctions.MonoSignatureGetParamCount(sig);
            if (pc != paramTypes.Length) continue;

            bool match = true;
            void* iter2 = null;
            for (uint i = 0; i < pc; i++)
            {
                var pt = MonoFunctions.MonoSignatureGetParams(sig, ref iter2);
                if (pt == 0) { match = false; break; }
                var ptName = MonoFunctions.MonoTypeGetName(pt);
                if (ptName != paramTypes[i]) { match = false; break; }
            }
            if (match) return new MonoMethod(m);
        }
        return null;
    }

    IRuntimeMethod? IRuntimeClass.GetMethod(string name, params string[] paramTypes)
        => GetMethod(name, paramTypes);

    // ──── Fields ────

    public MonoField? GetField(string name)
    {
        var f = MonoFunctions.MonoClassGetFieldFromName(Ptr, name);
        return f != 0 ? new MonoField(f, Ptr) : null;
    }

    IRuntimeField? IRuntimeClass.GetField(string name)
        => GetField(name);

    public IEnumerable<MonoField> GetFields()
    {
        nint iter = 0;
        nint f;
        while ((f = MonoFunctions.MonoClassGetFields(Ptr, ref iter)) != 0)
            yield return new MonoField(f, Ptr);
    }

    public static MonoClass? FromName(nint image, string namespaze, string name)
    {
        var k = MonoFunctions.MonoClassFromName(image, namespaze, name);
        return k != 0 ? new MonoClass(k) : null;
    }

    internal static MonoMethod? FromMethodPtr(nint m)
        => m != 0 ? new MonoMethod(m) : null;
}

public unsafe class MonoMethod : IRuntimeMethod
{
    public nint Ptr { get; }
    public MonoMethod(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    public string? Name => MonoFunctions.MonoMethodGetName(Ptr);

    /// <summary>编译方法并返回指向原生代码的函数指针</summary>
    public nint FunctionPtr => MonoFunctions.MonoCompileMethod(Ptr);

    /// <summary>获取方法签名</summary>
    public nint Signature => MonoFunctions.MonoMethodSignature(Ptr);

    /// <summary>参数个数</summary>
    public uint ParamCount => MonoFunctions.MonoSignatureGetParamCount(Signature);

    /// <summary>返回类型</summary>
    public nint ReturnType => MonoFunctions.MonoSignatureGetReturnType(Signature);

    /// <summary>返回类型名称（如 "System.Int32"）</summary>
    public string? ReturnTypeName
    {
        get
        {
            var rt = ReturnType;
            return rt != 0 ? MonoFunctions.MonoTypeGetName(rt) : null;
        }
    }

    /// <summary>判断是否为静态方法</summary>
    public bool IsStatic
    {
        get
        {
            uint flags = MonoFunctions.MonoMethodGetFlags(Ptr);
            return (flags & 0x10) != 0;
        }
    }

    // ──── Invoke ────

    /// <summary>调用方法。obj=0 表示静态调用。args 传递 nint 指针数组。</summary>
    public unsafe nint Invoke(nint obj, nint[]? args = null)
    {
        nint exc = 0;
        return MonoFunctions.MonoRuntimeInvoke(Ptr, obj, args, out exc);
    }

    /// <summary>静态调用</summary>
    public nint InvokeStatic(nint[]? args = null)
        => Invoke(0, args);

    /// <summary>调用并拆箱返回值（适用于返回值为值类型的方法）</summary>
    public unsafe T InvokeUnbox<T>(nint obj, nint[]? args = null) where T : unmanaged
    {
        nint ret = Invoke(obj, args);
        if (ret == 0) return default;
        nint unboxed = MonoFunctions.MonoObjectUnbox(ret);
        if (unboxed == 0) return default;
        return *(T*)unboxed;
    }

    /// <summary>静态调用并拆箱</summary>
    public T InvokeStaticUnbox<T>(nint[]? args = null) where T : unmanaged
        => InvokeUnbox<T>(0, args);

    /// <summary>调用构造函数（调用前必须已分配对象）</summary>
    public void InvokeConstructor(nint obj)
        => MonoFunctions.MonoRuntimeObjectInit(obj);
}

public unsafe class MonoField : IRuntimeField
{
    public nint Ptr { get; }
    public nint Klass { get; }
    public MonoField(nint ptr, nint klass = 0) { Ptr = ptr; Klass = klass; }
    public bool IsValid => Ptr != 0;

    public string? Name => MonoFunctions.MonoFieldGetName(Ptr);
    public uint Offset => MonoFunctions.MonoFieldGetOffset(Ptr);
    public bool IsStatic => (MonoFunctions.MonoFieldGetFlags(Ptr) & 0x10) != 0;
    public string? TypeName
    {
        get
        {
            var t = MonoFunctions.MonoFieldGetType(Ptr);
            return t != 0 ? MonoFunctions.MonoTypeGetName(t) : null;
        }
    }

    public unsafe T GetValue<T>(nint obj) where T : unmanaged
    {
        T val = default;
        if (IsStatic)
        {
            var domain = MonoFunctions.MonoGetRootDomain();
            var vtable = MonoFunctions.MonoClassVtable(domain, Klass);
            MonoFunctions.MonoFieldStaticGetValue(vtable, Ptr, (nint)(&val));
        }
        else
        {
            MonoFunctions.MonoFieldGetValue(obj, Ptr, (nint)(&val));
        }
        return val;
    }

    public unsafe void SetValue<T>(nint obj, T value) where T : unmanaged
    {
        if (IsStatic)
        {
            var domain = MonoFunctions.MonoGetRootDomain();
            var vtable = MonoFunctions.MonoClassVtable(domain, Klass);
            MonoFunctions.MonoFieldStaticSetValue(vtable, Ptr, (nint)(&value));
        }
        else
        {
            MonoFunctions.MonoFieldSetValue(obj, Ptr, (nint)(&value));
        }
    }
}
