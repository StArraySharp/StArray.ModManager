namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>统一运行时程序集抽象</summary>
public interface IRuntimeAssembly
{
    nint Ptr { get; }
    bool IsValid { get; }
    string? Name { get; }
    /// <summary>程序集文件的完整路径（可能为 null，例如从内存加载的程序集）。</summary>
    string? Filename { get; }
    IRuntimeClass? GetClass(string namespaze, string name);
}
