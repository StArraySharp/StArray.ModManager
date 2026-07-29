using Microsoft.CodeAnalysis;

namespace StArray.ModManager.Analyzer;

/// <summary>
/// 生成器诊断。此前所有失败路径都是静默 return/continue —— 用户漏写 partial、漏写 static、
/// 或程序集名少了 .dll，都不会有任何提示，只能在运行时表现为「功能不存在」。
/// </summary>
internal static class Diagnostics
{
    private const string Category = "StArray.ModManager";

    /// <summary>类标了 [UnmanagedType] 却不是 partial</summary>
    public static readonly DiagnosticDescriptor TypeNotPartial = new(
        "SAMM001",
        "标记 [UnmanagedType] 的类必须是 partial",
        "类型 '{0}' 标记了 [UnmanagedType]，但不是 partial —— 不会为它生成任何存根实现",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>成员标了 [UnmanagedMember] 却不是 partial 定义</summary>
    public static readonly DiagnosticDescriptor MemberNotPartial = new(
        "SAMM002",
        "标记 [UnmanagedMember] 的方法必须是 partial 定义",
        "方法 '{0}' 标记了 [UnmanagedMember]，但不是无实现的 partial 定义 —— 会被跳过",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>类里没有任何可生成的成员</summary>
    public static readonly DiagnosticDescriptor NoMembers = new(
        "SAMM003",
        "[UnmanagedType] 类型没有可生成的成员",
        "类型 '{0}' 标记了 [UnmanagedType]，但没有任何标记 [UnmanagedMember] 的 partial 方法",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    /// <summary>程序集名缺少 .dll 后缀</summary>
    public static readonly DiagnosticDescriptor AssemblyNameMissingDll = new(
        "SAMM004",
        "程序集名应带 .dll 后缀",
        "程序集名 '{0}' 缺少 .dll 后缀；运行时通过 OpenAssembly 解析，需要文件名形式（如 \"{0}.dll\"），否则会在运行时静默解析失败",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    /// <summary>Hook 方法不是 static</summary>
    public static readonly DiagnosticDescriptor HookNotStatic = new(
        "SAMM005",
        "Hook 方法必须是 static",
        "方法 '{0}' 标记了 Hook 特性，但不是 static —— 不会为它生成任何 hook 代码",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>Hook 的目标类型/方法名为空</summary>
    public static readonly DiagnosticDescriptor HookMissingTarget = new(
        "SAMM006",
        "Hook 缺少目标信息",
        "方法 '{0}' 的 Hook 特性缺少必要的目标参数（{1}）",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>[NativeImport] 方法不是 partial / 有实现体</summary>
    public static readonly DiagnosticDescriptor ImportNotPartial = new(
        "SAMM007",
        "标记 [NativeImport] 的方法必须是无实现的 partial 定义",
        "方法 '{0}' 标记了 [NativeImport]，但不是无实现的 partial 定义 —— 会被跳过",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>[NativeImport] 未指定库名，且所在类也没有类级默认库</summary>
    public static readonly DiagnosticDescriptor ImportMissingLibrary = new(
        "SAMM008",
        "[NativeImport] 缺少库名",
        "方法 '{0}' 未指定库名，且其所在类型没有类级默认库",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    /// <summary>实例 Hook 的首参不是 nint</summary>
    public static readonly DiagnosticDescriptor HookFirstParamNotThis = new(
        "SAMM009",
        "实例方法 Hook 的首个参数应为 nint（this 指针）",
        "方法 '{0}' 的首个参数是 '{1}'；被 hook 的实例方法在 native 侧首参为 this 指针，通常应声明为 nint",
        Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

    /// <summary>Resolver 方法不存在</summary>
    public static readonly DiagnosticDescriptor ResolverMethodNotFound = new(
        "SAMM010",
        "Resolver 方法不存在",
        "方法 '{0}' 中指定的 resolver 方法 '{1}' 不存在或签名不匹配（需要 static nint MethodName()）",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
