namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时字段抽象</summary>
public interface IRuntimeField
{
    nint Ptr { get; }
    bool IsValid { get; }
    string? Name { get; }
    uint Offset { get; }
    bool IsStatic { get; }
    string? TypeName { get; }

    unsafe T GetValue<T>(nint obj) where T : unmanaged;
    unsafe void SetValue<T>(nint obj, T value) where T : unmanaged;
}
