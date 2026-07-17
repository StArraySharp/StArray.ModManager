namespace StArray.ModManager.RuntimeAbstractions;

[AttributeUsage(AttributeTargets.Class)]
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

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UnmanagedMemberAttribute : Attribute
{
    public string? Name { get; set; }
    public int ParamCount { get; set; } = -1;
}
