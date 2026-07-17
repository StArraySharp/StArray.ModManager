using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.TestStubs;

[UnmanagedType("UnityEngine.CoreModule.dll", "UnityEngine", "Transform")]
public partial class Transform : UnmanagedObject
{
    public Transform(nint ptr) : base(ptr) { }

    // 读写属性 → Position { get; set; }
    [UnmanagedMember] public partial nint get_position();
    [UnmanagedMember] public partial void set_position(nint value);

    // 只读属性 → LocalPosition { get; }
    [UnmanagedMember] public partial nint get_localPosition();

    // 只写属性 → LocalScale { set; }
    [UnmanagedMember] public partial void set_localScale(nint value);

    // 普通方法
    [UnmanagedMember] public partial nint Find(string name);
    [UnmanagedMember] public partial void DoSomething(int value, bool flag);

    // 静态方法
    [UnmanagedMember] public static partial nint GetSomeStaticValue();
    [UnmanagedMember] public static partial void SetSomeStaticValue(nint value);

    // 事件
    [UnmanagedMember] public partial void add_TransformChanged(nint handler);
    [UnmanagedMember] public partial void remove_TransformChanged(nint handler);
}
