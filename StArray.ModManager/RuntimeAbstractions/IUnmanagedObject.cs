namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>
/// 原生托管对象基类 —— 强制子类实现 <c>nint Ptr</c> 属性并通过构造函数传入指针。
/// <see cref="RuntimeObject{T}.GetInstance"/> 通过本基类构造函数创建类型化实例。
/// </summary>
public abstract class UnmanagedObject
{
    public nint Ptr { get; }

    protected UnmanagedObject(nint ptr) => Ptr = ptr;

    public T Field<T>(string name) where T : unmanaged
        => new RuntimeObject(Ptr).GetField<T>(name);

    protected static T? Wrap<T>(nint ptr) where T : UnmanagedObject
        => ptr != 0 ? (T?)Activator.CreateInstance(typeof(T), ptr) : null;
}

