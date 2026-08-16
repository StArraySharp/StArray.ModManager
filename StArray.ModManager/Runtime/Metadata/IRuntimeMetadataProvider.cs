namespace StArray.ModManager.Runtime.Metadata;

/// <summary>
/// 类型与其命名空间。<see cref="Name"/> 为点号连接的嵌套链（如 "Outer.Inner"）。
/// </summary>
public readonly record struct TypeIdentity(string Namespace, string Name);

/// <summary>字段元数据（后端无关形）。</summary>
public readonly record struct FieldSnapshot(
    string Name,
    bool IsStatic,
    bool IsLiteral,
    string TypeName,        // 已解析的 stub 类型名（"int"、"ns.Type" 等）
    string RawTypeName,     // 运行时自己的类型全名（含泛型参数），用于注释
    nint TypePtr,
    nint FieldPtr,
    uint Flags);

/// <summary>方法/构造器元数据（后端无关形）。</summary>
public readonly record struct MethodSnapshot(
    string Name,
    string ReturnTypeName,
    string[] ParamTypeNames,
    bool IsStatic,
    nint RetTypePtr,
    nint[] ParamTypePtrs,
    string RawReturnTypeName,
    string[] RawParamTypeNames,
    bool IsCtor);

/// <summary>
/// 运行时元数据提供者的抽象约束：把 Mono / Il2Cpp 的原生差异全部封装在实现里，
/// <c>AssemblyEmitter</c> 只依赖本接口（不再出现 RuntimeManager.Is* 分叉）。
/// </summary>
/// <remarks>
/// 实现方必须保证：所有成员读取对"已收集类型"安全；对未收集/外来指针应返回空值
/// 而非触发原生崩溃（Mono 侧历史上有 0xC0000005 教训，见各实现注释）。
/// </remarks>
public interface IRuntimeMetadataProvider
{
    // ── 程序集 ──

    /// <summary>枚举当前域内可参与 stub 生成的程序集（已按 SkipAssemblies 等规则过滤前的全集）。</summary>
    IEnumerable<(string Name, string? Filename, nint Ptr)> EnumerateAssemblies();

    /// <summary>收集一个程序集的全部类型，返回 (类型指针, 命名空间, 显示名, 嵌套父指针)。</summary>
    IEnumerable<(nint TypePtr, TypeIdentity Identity, nint NestingParent)> CollectTypes(nint assemblyPtr);

    // ── 类型 ──

    /// <summary>类型是否为枚举。</summary>
    bool IsEnum(nint typePtr);

    /// <summary>类型是否为接口。</summary>
    bool IsInterface(nint typePtr);

    /// <summary>类型是否为开放泛型定义。</summary>
    bool IsGenericTypeDefinition(nint typePtr);

    /// <summary>类型的直接父类指针（无则 0）。</summary>
    nint GetParentClass(nint typePtr);

    /// <summary>类型实现的全部接口指针。</summary>
    IEnumerable<nint> EnumerateInterfaces(nint typePtr);

    /// <summary>该指针是否属于本次收集的类型（防护未知指针）。</summary>
    bool IsCollectedType(nint typePtr);

    // ── 成员 ──

    /// <summary>类型的全部实例+静态字段。</summary>
    IEnumerable<FieldSnapshot> EnumerateFields(nint typePtr);

    /// <summary>类型的全部方法（含构造器，<see cref="MethodSnapshot.IsCtor"/> 区分）。</summary>
    IEnumerable<MethodSnapshot> EnumerateMethods(nint typePtr);

    /// <summary>枚举类型的字面值成员（enum 成员）。返回 null 表示无法安全读取（调用方降级为普通类）。</summary>
    List<(string Name, object Value)>? ReadEnumMembers(nint typePtr, Type underlying);

    // ── 线程 ──

    /// <summary>进入元数据读取前附加线程（如需要）。返回是否由本调用附加（退出时需配对 detach）。</summary>
    bool AttachThread();

    /// <summary>配对 <see cref="AttachThread"/> 的退出。</summary>
    void DetachThread();
}
