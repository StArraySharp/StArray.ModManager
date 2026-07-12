namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时方法抽象</summary>
public interface IRuntimeMethod
{
    nint Ptr { get; }
    bool IsValid { get; }
    string? Name { get; }
    nint FunctionPtr { get; }
    uint ParamCount { get; }
    bool IsStatic { get; }
    string? ReturnTypeName { get; }

    nint Invoke(nint obj, nint[]? args = null);
    nint InvokeStatic(nint[]? args = null);
    T InvokeUnbox<T>(nint obj, nint[]? args = null) where T : unmanaged;
    T InvokeStaticUnbox<T>(nint[]? args = null) where T : unmanaged;
}
