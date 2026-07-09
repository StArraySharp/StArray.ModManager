using System.Runtime.InteropServices;

namespace StArray.ModManager.Mono;

/// <summary>Mono 托管对象基类 — 对应 System.Object</summary>
public unsafe class MonoObject
{
    public nint Ptr { get; }
    public MonoObject(nint ptr) => Ptr = ptr;
    public bool IsValid => Ptr != 0;

    /// <summary>拆箱值类型，返回指向值类型数据的指针</summary>
    public nint Unbox() => MonoFunctions.MonoObjectUnbox(Ptr);

    /// <summary>获取对象的 MonoClass</summary>
    public nint GetClass() => MonoFunctions.MonoObjectGetClass(Ptr);

    public override string? ToString()
    {
        var k = MonoFunctions.MonoClassFromName(
            MonoFunctions.MonoGetCorlib(), "System", "Object");
        var m = MonoFunctions.MonoClassGetMethodFromName(k, "ToString", 0);
        if (m == 0) return null;
        nint exc = 0;
        var r = MonoFunctions.MonoRuntimeInvoke(m, Ptr, null, out exc);
        if (r == 0) return null;
        return new MonoString(r).ToString();
    }

    public int GetHashCodeManaged()
    {
        var k = MonoFunctions.MonoClassFromName(
            MonoFunctions.MonoGetCorlib(), "System", "Object");
        var m = MonoFunctions.MonoClassGetMethodFromName(k, "GetHashCode", 0);
        if (m == 0) return 0;
        nint exc = 0;
        return (int)MonoFunctions.MonoRuntimeInvoke(m, Ptr, null, out exc);
    }

    protected T? GetInvoked<T>(nint method) where T : MonoObject
    {
        nint exc = 0;
        var r = MonoFunctions.MonoRuntimeInvoke(method, Ptr, null, out exc);
        return r != 0 ? (T)Activator.CreateInstance(typeof(T), r)! : null;
    }
}

/// <summary>Mono 托管字符串 — 对应 System.String</summary>
public unsafe class MonoString : MonoObject
{
    public MonoString(nint ptr) : base(ptr) { }

    /// <summary>字符串长度（字符数）</summary>
    public int Length => MonoFunctions.MonoStringLength(Ptr);

    /// <summary>指向内部 UTF-16 字符缓冲区的指针</summary>
    public char* Chars => MonoFunctions.MonoStringChars(Ptr);

    /// <summary>转换为托管 System.String</summary>
    public override string ToString() => Marshal.PtrToStringUni((nint)Chars, Length) ?? "";

    /// <summary>创建新字符串（UTF-8 输入）</summary>
    public static MonoString New(nint domain, string str)
        => new(MonoFunctions.MonoStringNew(domain, str));

    /// <summary>导出为 UTF-8 并释放内部缓冲区</summary>
    public string? ToUTF8() => MonoFunctions.MonoStringToUTF8(Ptr);

    public static implicit operator string(MonoString s) => s.ToString();
}

/// <summary>Mono 托管数组 — 对应 System.Array</summary>
public unsafe class MonoArray : MonoObject
{
    public MonoArray(nint ptr) : base(ptr) { }

    /// <summary>数组元素个数</summary>
    public nuint Length => MonoFunctions.MonoArrayLength(Ptr);

    /// <summary>获取元素类</summary>
    public static nint GetArrayClass(nint elementClass, uint rank)
        => MonoFunctions.MonoArrayClassGet(elementClass, rank);

    /// <summary>创建新数组</summary>
    public static MonoArray New(nint domain, nint eclass, nuint n)
        => new(MonoFunctions.MonoArrayNew(domain, eclass, n));
}

/// <summary>Mono 托管泛型数组 — 强类型元素访问</summary>
public unsafe class MonoArray<T> : MonoArray where T : MonoObject
{
    public MonoArray(nint ptr) : base(ptr) { }

    public T? this[nuint index]
    {
        get
        {
            if (index >= Length) return null;
            // Mono 数组元素按指针大小对齐存储对象引用
            var elemPtr = Unbox() + (nint)(index * (nuint)nint.Size);
            var objPtr = *(nint*)elemPtr;
            return objPtr != 0 ? (T)Activator.CreateInstance(typeof(T), objPtr)! : null;
        }
    }

    public T[] ToArray()
    {
        var arr = new T[Length];
        for (nuint i = 0; i < Length; i++)
            arr[i] = this[i]!;
        return arr;
    }

    public List<T> ToList()
    {
        var list = new List<T>((int)Length);
        for (nuint i = 0; i < Length; i++)
            list.Add(this[i]!);
        return list;
    }
}

/// <summary>Mono 异常 — 对应 System.Exception</summary>
public unsafe class MonoException : MonoObject
{
    public MonoException(nint ptr) : base(ptr) { }

    /// <summary>获取异常消息</summary>
    public string? Message
    {
        get
        {
            var k = MonoFunctions.MonoClassFromName(
                MonoFunctions.MonoGetCorlib(), "System", "Exception");
            var m = MonoFunctions.MonoClassGetMethodFromName(k, "get_Message", 0);
            if (m == 0) return null;
            nint exc = 0;
            var r = MonoFunctions.MonoRuntimeInvoke(m, Ptr, null, out exc);
            return r != 0 ? new MonoString(r).ToString() : null;
        }
    }
}
