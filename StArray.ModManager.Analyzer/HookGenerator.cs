using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StArray.ModManager.Analyzer;

/// <summary>
/// 增量 Source Generator —— 为 <see cref="NativeHookAttribute"/> 和
/// <see cref="UnmanagedHookAttribute"/> 自动生成 Hook 安装/卸载/原函数调用基础设施。
/// </summary>
[Generator]
public class HookGenerator : IIncrementalGenerator
{
    private const string NativeHookAttrName = "StArray.ModManager.Hooks.NativeHookAttribute";
    private const string UnmanagedHookAttrName = "StArray.ModManager.Hooks.UnmanagedHookAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is MethodDeclarationSyntax mds &&
                    mds.AttributeLists.Count > 0 &&
                    mds.Modifiers.Any(SyntaxKind.StaticKeyword),
                transform: static (ctx, _) => GetHookInfo(ctx))
            .Where(static info => info != null)!;

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(methods.Collect()),
            static (spc, t) => GenerateCode(spc, t.Left, t.Right));
    }

    private sealed class HookInfo
    {
        public string AttrType { get; set; }
        public string ContainingType { get; set; }
        public string MethodName { get; set; }
        public string ReturnType { get; set; }
        public ImmutableArray<(string Type, string Name)> Parameters { get; set; }
        public ImmutableArray<string> CtorArgs { get; set; }
        public ImmutableArray<(string Key, string Value)> NamedArgs { get; set; }
        public bool HasUnmanagedCallersOnly { get; set; }
    }

    private static HookInfo? GetHookInfo(GeneratorSyntaxContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        var sem = ctx.SemanticModel;
        var sym = sem.GetDeclaredSymbol(method);
        if (sym is null) return null;

        foreach (var attr in sym.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            string? attrType = attrName switch
            {
                NativeHookAttrName => "NativeHook",
                UnmanagedHookAttrName => "UnmanagedHook",
                _ => null,
            };
            if (attrType == null) continue;

            var ct = sym.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var rt = sym.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var pl = ImmutableArray.CreateBuilder<(string, string)>();
            foreach (var p in sym.Parameters)
                pl.Add((p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), p.Name));

            var ca = ImmutableArray.CreateBuilder<string>();
            foreach (var a in attr.ConstructorArguments) ca.Add(FormatConstant(a));
            var na = ImmutableArray.CreateBuilder<(string, string)>();
            foreach (var n in attr.NamedArguments) na.Add((n.Key, FormatConstant(n.Value)));

            var uco = sym.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute");

            return new HookInfo
            {
                AttrType = attrType,
                ContainingType = ct,
                MethodName = sym.Name,
                ReturnType = rt,
                Parameters = pl.ToImmutable(),
                CtorArgs = ca.ToImmutable(),
                NamedArgs = na.ToImmutable(),
                HasUnmanagedCallersOnly = uco,
            };
        }
        return null;
    }

    private static string FormatConstant(TypedConstant tc)
    {
        if (tc.Kind == TypedConstantKind.Enum)
        {
            var intVal = tc.Value;
            foreach (var m in tc.Type!.GetMembers())
                if (m is IFieldSymbol fs && fs.ConstantValue != null && fs.HasConstantValue && fs.ConstantValue.Equals(intVal))
                    return $"{tc.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{fs.Name}";
            return $"{tc.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{tc.Value}";
        }
        if (tc.Kind == TypedConstantKind.Array)
        {
            var items = tc.Values.Select(FormatConstant);
            return "new string[] { " + string.Join(", ", items) + " }";
        }
        return tc.Value switch
        {
            string s => $"\"{s}\"", bool b => b ? "true" : "false",
            char c => $"'{c}'", null => "null",
            _ => tc.Value.ToString() ?? "null",
        };
    }

    private static void GenerateCode(SourceProductionContext ctx, Compilation compilation,
        ImmutableArray<HookInfo> hooks)
    {
        if (hooks.Length == 0) return;

        foreach (var group in hooks.GroupBy(h => h.ContainingType))
        {
            var ns = ExtractNamespace(group.Key);
            var cn = ExtractClassName(group.Key);
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8618, CS8625");
            sb.AppendLine("#nullable enable");
            sb.AppendLine($"namespace {ns ?? "__Hooks"}");
            sb.AppendLine("{");
            sb.AppendLine($"    public unsafe partial class {cn}");
            sb.AppendLine("    {");

            foreach (var h in group)
            {
                var sf = Sanitize(h.MethodName);
                var pd = string.Join(", ", h.Parameters.Select(p => $"{SimplifyType(p.Type)} @{p.Name}"));
                var pn = string.Join(", ", h.Parameters.Select(p => $"@{p.Name}"));
                var cv = GetConv(h);
                var ucoCallConv = GetUCOCallConv(h);

                var rt = SimplifyType(h.ReturnType);

                // ── 委托类型（用于保存原函数）──
                sb.AppendLine();
                sb.AppendLine($"        [{cv}]");
                sb.AppendLine($"        private delegate {rt} _{sf}Delegate({pd});");
                sb.AppendLine($"        private static nint _{sf}_origPtr;");
                sb.AppendLine($"        private static _{sf}Delegate? _{sf}_orig;");

                // ── 持久引用（防止 delegate 被 GC；[UnmanagedCallersOnly] 不需要）──
                if (!h.HasUnmanagedCallersOnly)
                    sb.AppendLine($"        private static _{sf}Delegate? _{sf}_wrap;");

                // ── Install ──
                sb.AppendLine();
                sb.AppendLine($"        private static bool Install_{sf}()");
                sb.AppendLine("        {");
                if (h.AttrType == "NativeHook")
                {
                    bool isAddr = h.CtorArgs.Length == 1 && !h.CtorArgs[0].StartsWith("\"");
                    if (isAddr)
                        sb.AppendLine($"            var t = (nint){h.CtorArgs[0]};");
                    else
                    {
                        var l = h.CtorArgs.Length > 0 ? h.CtorArgs[0] : "\"\"";
                        var s = h.CtorArgs.Length > 1 ? h.CtorArgs[1] : "\"\"";
                        sb.AppendLine($"            var t = global::StArray.ModManager.Runtime.HookHelper.GetFunction({l}, {s});");
                    }
                    sb.AppendLine($"            if (t == nint.Zero) return false;");
                    sb.AppendLine($"            _{sf}_origPtr = t;");
                    if (h.HasUnmanagedCallersOnly)
                        sb.AppendLine($"            var d = typeof({h.ContainingType}).GetMethod(\"{h.MethodName}\", global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();");
                    else
                    {
                        sb.AppendLine($"            _{sf}_wrap = {h.MethodName};");
                        sb.AppendLine($"            var d = global::System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_{sf}_wrap);");
                    }
                    sb.AppendLine($"            var o = global::StArray.ModManager.Runtime.HookHelper.Hook(t, d);");
                    sb.AppendLine($"            if (o == nint.Zero) return false;");
                    sb.AppendLine($"            _{sf}_orig = global::System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<_{sf}Delegate>(o);");
                    sb.AppendLine("            return true;");
                }
                else // UnmanagedHook
                {
                    var a = h.CtorArgs.Length > 0 ? h.CtorArgs[0] : "\"\"";
                    string clsNs;
                    string c;
                    string m;
                    if (h.CtorArgs.Length == 4)
                    {
                        clsNs = h.CtorArgs[1];
                        c = h.CtorArgs[2];
                        m = h.CtorArgs[3];
                    }
                    else
                    {
                        clsNs = "\"\"";
                        c = h.CtorArgs.Length > 1 ? h.CtorArgs[1] : "\"\"";
                        m = h.CtorArgs.Length > 2 ? h.CtorArgs[2] : "\"\"";
                    }
                    foreach (var n in h.NamedArgs)
                        if (n.Key is "Namespace" or "namespace") clsNs = n.Value;
                    string? ptn = null;
                    foreach (var n in h.NamedArgs)
                        if (n.Key is "ParameterTypeNames" or "parameterTypeNames") ptn = n.Value;
                    string pc = "-1";
                    foreach (var n in h.NamedArgs)
                        if (n.Key is "ParameterCount" or "parameterCount") pc = n.Value;

                    sb.AppendLine($"            var domain = global::StArray.ModManager.RuntimeAbstractions.RuntimeManager.GetDomain();");
                    sb.AppendLine($"            if (domain == null) return false;");
                    sb.AppendLine($"            var asm = domain.OpenAssembly({a});");
                    sb.AppendLine($"            if (asm == null) return false;");
                    sb.AppendLine($"            var cls = asm.GetClass({clsNs}, {c});");
                    sb.AppendLine($"            if (cls == null) return false;");
                    if (ptn != null)
                        sb.AppendLine($"            var method = cls.GetMethod({m}, {ptn});");
                    else
                        sb.AppendLine($"            var method = cls.GetMethod({m}, {pc});");
                    sb.AppendLine($"            if (method == null) return false;");
                    sb.AppendLine($"            var t = method.FunctionPtr;");
                    sb.AppendLine($"            _{sf}_origPtr = t;");
                    if (h.HasUnmanagedCallersOnly)
                        sb.AppendLine($"            var d = typeof({h.ContainingType}).GetMethod(\"{h.MethodName}\", global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();");
                    else
                    {
                        sb.AppendLine($"            _{sf}_wrap = {h.MethodName};");
                        sb.AppendLine($"            var d = global::System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_{sf}_wrap);");
                    }
                    sb.AppendLine($"            var o = global::StArray.ModManager.Runtime.HookHelper.Hook(t, d);");
                    sb.AppendLine($"            if (o == nint.Zero) return false;");
                    sb.AppendLine($"            _{sf}_orig = global::System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<_{sf}Delegate>(o);");
                    sb.AppendLine("            return true;");
                }
                sb.AppendLine("        }");

                // ── Original ──
                sb.AppendLine();
                sb.AppendLine($"        public static {rt} {h.MethodName}Original({pd})");
                sb.AppendLine("        {");
                if (rt == "void")
                    sb.AppendLine($"            _{sf}_orig?.Invoke({pn});");
                else
                    sb.AppendLine($"            if (_{sf}_orig != null) return _{sf}_orig({pn}); return default;");
                sb.AppendLine("        }");
            }

            sb.AppendLine();
            sb.AppendLine("        public static bool InstallHooks()");
            sb.AppendLine("        {");
            sb.AppendLine("            bool ok = true;");
            foreach (var h in group)
                sb.AppendLine($"            ok &= Install_{Sanitize(h.MethodName)}();");
            sb.AppendLine("            return ok;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static void UninstallHooks()");
            sb.AppendLine("        {");
            foreach (var h in group)
            {
                var sf = Sanitize(h.MethodName);
                sb.AppendLine($"            if (_{sf}_origPtr != nint.Zero)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                global::StArray.ModManager.Runtime.HookHelper.Unhook(_{sf}_origPtr);");
                sb.AppendLine($"                _{sf}_origPtr = nint.Zero;");
                sb.AppendLine($"                _{sf}_orig = null;");
                if (!h.HasUnmanagedCallersOnly)
                    sb.AppendLine($"                _{sf}_wrap = null;");
                sb.AppendLine($"            }}");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            ctx.AddSource($"Hooks_{cn}.g.cs", sb.ToString());
        }
    }

    private static string GetConv(HookInfo h)
    {
        foreach (var n in h.NamedArgs)
            if (n.Key is "Convention" or "convention")
            {
                if (n.Value.Contains("StdCall"))
                    return "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.StdCall)";
                if (n.Value.Contains("FastCall"))
                    return "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.FastCall)";
                if (n.Value.Contains("ThisCall"))
                    return "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.ThisCall)";
                return "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)";
            }
        return h.AttrType == "NativeHook"
            ? "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.StdCall)"
            : "global::System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(global::System.Runtime.InteropServices.CallingConvention.Cdecl)";
    }

    private static string GetUCOCallConv(HookInfo h)
    {
        foreach (var n in h.NamedArgs)
            if (n.Key is "Convention" or "convention")
            {
                if (n.Value.Contains("StdCall"))
                    return "typeof(global::System.Runtime.CompilerServices.CallConvStdcall)";
                // FastCall / ThisCall —— x64 上与 Cdecl 二进制兼容
                return "typeof(global::System.Runtime.CompilerServices.CallConvCdecl)";
            }
        return h.AttrType == "NativeHook"
            ? "typeof(global::System.Runtime.CompilerServices.CallConvStdcall)"
            : "typeof(global::System.Runtime.CompilerServices.CallConvCdecl)";
    }

    private static bool IsVoid(HookInfo h) =>
        h.ReturnType is "void" or "global::System.Void";

    private static string SimplifyType(string type)
    {
        if (type == "global::System.IntPtr") return "nint";
        if (type == "global::System.UIntPtr") return "nuint";
        if (type == "global::System.Void") return "void";
        if (type.StartsWith("global::")) return type.Substring(8);
        return type;
    }

    private static string Sanitize(string n) => n.Replace(".", "_").Replace(" ", "_");

    private static string ExtractNamespace(string f)
    {
        string t = f.StartsWith("global::") ? f.Substring(8) : f;
        int d = t.LastIndexOf('.');
        return d > 0 ? t.Substring(0, d) : "";
    }

    private static string ExtractClassName(string f)
    {
        string t = f.StartsWith("global::") ? f.Substring(8) : f;
        int d = t.LastIndexOf('.');
        return d > 0 ? t.Substring(d + 1) : t;
    }
}
