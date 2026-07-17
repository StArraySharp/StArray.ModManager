namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时 AppDomain 抽象</summary>
public interface IAppDomain
{
    nint Ptr { get; }
    bool IsValid { get; }
    IRuntimeAssembly? OpenAssembly(string name);
    IReadOnlyList<IRuntimeAssembly> GetAssemblies();

    nint NewString(string str);
    nint NewArray(nint elementClass, int length);
}
