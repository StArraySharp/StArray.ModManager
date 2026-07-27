namespace StArray.ModManager.RuntimeAbstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface)]
public sealed class UnmanagedTypeAttribute : Attribute
{
    public string Assembly { get; }
    public string Namespace { get; }
    public string ClassName { get; }
    public UnmanagedTypeAttribute(string assembly, string ns, string className)
    {
        Assembly = assembly;
        Namespace = ns;
        ClassName = className;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UnmanagedMemberAttribute : Attribute
{
    public string? Name { get; set; }
    public int ParamCount { get; set; } = -1;
}

/// <summary>
/// 记录成员在运行时的真实类型名（含泛型参数），用于在存根里退化成 <c>nint</c> 的类型 ——
/// 例如 <c>System.Collections.Generic.HashSet`1&lt;DLCManager&gt;</c>。
/// 纯说明性质，不参与调用；拿到指针后可自行用
/// <see cref="UnmanagedEnumerable"/> 之类的包装器访问。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class UnmanagedTypeNameAttribute(string typeName) : Attribute
{
    public string TypeName { get; } = typeName;
}
