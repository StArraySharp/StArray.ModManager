namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时类抽象</summary>
public interface IRuntimeClass
{
    nint Ptr { get; }
    bool IsValid { get; }
    string? Name { get; }
    string? Namespace { get; }
    IRuntimeMethod? GetMethod(string name, int paramCount);
    IRuntimeMethod? GetMethod(string name, params string[] paramTypes);
    IRuntimeField? GetField(string name);
}
