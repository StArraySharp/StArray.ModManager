using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一托管数组 — 元素按 nint 大小对齐（对象引用数组）</summary>
public readonly unsafe struct RuntimeArray
{
    public nint Ptr { get; }

    public RuntimeArray(nint ptr) => Ptr = ptr;
    public RuntimeArray(RuntimeObject obj) => Ptr = obj.Ptr;
    public bool IsValid => Ptr != 0;

    public int Length
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return (int)Il2CppFunctions.il2cpp_array_length(Ptr);
            if (RuntimeManager.IsMono)
            {
                MonoDomain.Current.ThreadAttach();
                var res = (int)MonoFunctions.MonoArrayLength(Ptr);
                MonoDomain.Current.ThreadDetach();
                return res;
            }
            return 0;
        }
    }

    /// <summary>获取元素原始指针（指向数组数据起始位置）</summary>
    public nint DataPtr
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_object_unbox(Ptr);
            if (RuntimeManager.IsMono) return MonoFunctions.MonoObjectUnbox(Ptr);
            return 0;
        }
    }

    /// <summary>按索引读取元素（nint = 对象引用或值类型数据指针）</summary>
    public nint this[int index]
    {
        get
        {
            if (index < 0 || index >= Length) return 0;
            return *(nint*)(DataPtr + index * nint.Size);
        }
    }

    /// <summary>按索引获取元素为 RuntimeObject</summary>
    public RuntimeObject? GetObject(int index)
    {
        var ptr = this[index];
        return ptr != 0 ? new RuntimeObject(ptr) : null;
    }

    public RuntimeObject?[] ToObjectArray()
    {
        var arr = new RuntimeObject?[Length];
        for (int i = 0; i < Length; i++)
            arr[i] = GetObject(i);
        return arr;
    }

    public static RuntimeArray New(nint elementClass, int length)
    {
        var domain = RuntimeManager.GetDomain();
        return domain != null ? New(domain, elementClass, length) : default;
    }

    public static RuntimeArray New(IAppDomain domain, nint elementClass, int length)
    {
        var ptr = domain.NewArray(elementClass, length);
        return ptr != 0 ? new RuntimeArray(ptr) : default;
    }

    public static implicit operator RuntimeArray(RuntimeObject obj) => new(obj.Ptr);
    public static implicit operator RuntimeObject(RuntimeArray arr) => new(arr.Ptr);
}

/// <summary>统一托管泛型数组 — 强类型元素访问</summary>
public readonly unsafe struct RuntimeArray<T> where T : unmanaged
{
    public nint Ptr { get; }

    public RuntimeArray(nint ptr) => Ptr = ptr;
    public RuntimeArray(RuntimeObject obj) => Ptr = obj.Ptr;
    public RuntimeArray(RuntimeArray arr) => Ptr = arr.Ptr;
    public bool IsValid => Ptr != 0;

    public int Length
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return (int)Il2CppFunctions.il2cpp_array_length(Ptr);
            if (RuntimeManager.IsMono) return (int)MonoFunctions.MonoArrayLength(Ptr);
            return 0;
        }
    }

    private nint DataPtr
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_object_unbox(Ptr) + nint.Size * 2;
            if (RuntimeManager.IsMono) return Ptr + nint.Size;
            return 0;
        }
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Length) return default;
            if (RuntimeManager.IsMono)
                return *(T*)MonoFunctions.MonoArrayAddrWithSize(Ptr, sizeof(T), (nuint)index);
            return *(T*)(DataPtr + index * sizeof(T));
        }
    }

    public T[] ToArray()
    {
        var arr = new T[Length];
        for (int i = 0; i < Length; i++)
            arr[i] = this[i];
        return arr;
    }

    public List<T> ToList()
    {
        var list = new List<T>(Length);
        for (int i = 0; i < Length; i++)
            list.Add(this[i]);
        return list;
    }

    public static implicit operator RuntimeArray<T>(RuntimeObject obj) => new(obj.Ptr);
    public static implicit operator RuntimeObject(RuntimeArray<T> arr) => new(arr.Ptr);
}
