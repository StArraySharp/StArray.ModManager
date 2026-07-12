namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时程序集抽象</summary>
public interface IRuntimeAssembly
{
    nint Ptr { get; }
    bool IsValid { get; }
    string? Name { get; }
    IRuntimeClass? GetClass(string namespaze, string name);
}
