using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StArray.ModManager.SourceGenerator;

[Generator]
public class NativeImportGenerator : IIncrementalGenerator
{
    private const string AttrName = "StArray.ModManager.NativeImportAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidate(node),
                transform: static (ctx, _) => GetMethodInfo(ctx))
            .Where(static info => info != null)
            .Select(static (info, _) => info!);

        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetClassLevelLibrary(ctx))
            .Where(static lib => lib != null)
            .Select(static (lib, _) => lib!);

        var combined = methods.Collect().Combine(classes.Collect());

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(combined),
            static (spc, t) => Generate(spc, t.Left, t.Right.Left, t.Right.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        if (node is not MethodDeclarationSyntax mds) return false;
        if (!mds.Modifiers.Any(SyntaxKind.PartialKeyword)) return false;
        if (mds.Body != null || mds.ExpressionBody != null) return false;
        if (mds.AttributeLists.Count == 0) return false;
        return true;
    }

    private sealed class MethodInfo
    {
        public string ContainingType { get; set; } = "";
        public string MethodName { get; set; } = "";
        public string ReturnType { get; set; } = "";
        public bool IsStatic { get; set; }
        public ImmutableArray<(string Type, string Name)> Parameters { get; set; }
        public string? Library { get; set; }
        public string? EntryPoint { get; set; }
        public int Convention { get; set; }
        public int CharSet { get; set; }
    }

    private static MethodInfo? GetMethodInfo(GeneratorSyntaxContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        var sem = ctx.SemanticModel;
        var sym = sem.GetDeclaredSymbol(method);
        if (sym is null) return null;

        foreach (var attr in sym.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != AttrName) continue;

            var lib = attr.ConstructorArguments.Length > 0
                ? attr.ConstructorArguments[0].Value?.ToString() : null;

            string? entryPoint = null;
            int convention = 2; // Cdecl
            int charSet = 0;

            foreach (var n in attr.NamedArguments)
            {
                switch (n.Key)
                {
                    case "EntryPoint": entryPoint = n.Value.Value?.ToString(); break;
                    case "Convention": convention = (int)(n.Value.Value ?? 2); break;
                    case "CharSet": charSet = (int)(n.Value.Value ?? 0); break;
                }
            }

            if (entryPoint == null)
                entryPoint = sym.Name;

            var ct = sym.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var rt = sym.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var pl = ImmutableArray.CreateBuilder<(string, string)>();
            foreach (var p in sym.Parameters)
                pl.Add((p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), p.Name));

            return new MethodInfo
            {
                ContainingType = ct,
                MethodName = sym.Name,
                ReturnType = rt,
                IsStatic = sym.IsStatic,
                Parameters = pl.ToImmutable(),
                Library = lib,
                EntryPoint = entryPoint,
                Convention = convention,
                CharSet = charSet,
            };
        }

        return null;
    }

    private sealed class ClassLibrary
    {
        public string ContainingType { get; set; } = "";
        public string Library { get; set; } = "";
        public int Convention { get; set; }
        public int CharSet { get; set; }
    }

    private static ClassLibrary? GetClassLevelLibrary(GeneratorSyntaxContext ctx)
    {
        var cls = (ClassDeclarationSyntax)ctx.Node;
        var sem = ctx.SemanticModel;
        var sym = sem.GetDeclaredSymbol(cls);
        if (sym is null) return null;

        foreach (var attr in sym.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != AttrName) continue;
            if (attr.ConstructorArguments.Length == 0) return null;

            var lib = attr.ConstructorArguments[0].Value?.ToString();
            if (lib == null) return null;

            int convention = 2;
            int charSet = 0;
            foreach (var n in attr.NamedArguments)
            {
                switch (n.Key)
                {
                    case "Convention": convention = (int)(n.Value.Value ?? 2); break;
                    case "CharSet": charSet = (int)(n.Value.Value ?? 0); break;
                }
            }

            var ct = sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new ClassLibrary { ContainingType = ct, Library = lib, Convention = convention, CharSet = charSet };
        }

        return null;
    }

    private static void Generate(SourceProductionContext spc, Compilation compilation,
        ImmutableArray<MethodInfo> methods, ImmutableArray<ClassLibrary> classLibs)
    {
        if (methods.Length == 0) return;

        var libMap = classLibs.ToDictionary(l => l.ContainingType, l => l);

        foreach (var group in methods.GroupBy(m => m.ContainingType))
        {
            var ns = ExtractNamespace(group.Key);
            var cn = ExtractClassName(group.Key);
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8618, CS8625, IDE1006");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            if (ns != null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine("    using global::System.Runtime.InteropServices;");
            sb.AppendLine();
            sb.AppendLine($"    public unsafe partial class {cn}");
            sb.AppendLine("    {");

            foreach (var m in group)
            {
                var lib = m.Library ?? (libMap.TryGetValue(m.ContainingType, out var cl) ? cl.Library : null);
                if (lib == null) continue;

                var nativeName = m.EntryPoint ?? m.MethodName;
                var conv = m.Convention > 0 ? m.Convention : (libMap.TryGetValue(m.ContainingType, out var cl2) ? cl2.Convention : 2);
                var cs = m.CharSet > 0 ? m.CharSet : (libMap.TryGetValue(m.ContainingType, out var cl3) ? cl3.CharSet : 0);

                var pd = string.Join(", ", m.Parameters.Select(p => $"{SimplifyType(p.Type)} @{p.Name}"));
                var pn = string.Join(", ", m.Parameters.Select(p => $"@{p.Name}"));
                var rt = SimplifyType(m.ReturnType);
                var mod = m.IsStatic ? "static " : "";

                var convStr = ConventionString(conv);
                var charSetStr = cs == 0 ? "" : cs switch
                {
                    1 => ", CharSet = CharSet.Ansi",
                    2 => ", CharSet = CharSet.Unicode",
                    3 => ", CharSet = CharSet.Auto",
                    _ => "",
                };

                var dgName = $"_{m.MethodName}_dgt";
                var fieldName = $"s_{m.MethodName}";

                sb.AppendLine();
                sb.AppendLine($"        [{convStr}{charSetStr}]");
                sb.AppendLine($"        private delegate {rt} {dgName}({pd});");
                sb.AppendLine($"        private static {dgName}? {fieldName};");
                sb.AppendLine();
                sb.AppendLine($"        public {mod}partial {rt} {m.MethodName}({pd})");
                sb.AppendLine("        {");
                sb.AppendLine($"            var d = {fieldName};");
                sb.AppendLine("            if (d == null)");
                sb.AppendLine("            {");
                sb.AppendLine($"                var ptr = global::StArray.ModManager.Runtime.HookHelper.GetFunction(\"{EscapeString(lib)}\", \"{EscapeString(nativeName)}\");");
                sb.AppendLine($"                d = Marshal.GetDelegateForFunctionPointer<{dgName}>(ptr);");
                sb.AppendLine($"                {fieldName} = d;");
                sb.AppendLine("            }");
                sb.AppendLine(rt == "void"
                    ? $"            d({pn});"
                    : $"            return d({pn});");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            if (ns != null) sb.AppendLine("}");
            sb.AppendLine();

            spc.AddSource($"NativeImport_{cn}.g.cs", sb.ToString());
        }
    }

    private static string? ExtractNamespace(string f)
    {
        var t = f.StartsWith("global::") ? f.Substring(8) : f;
        var d = t.LastIndexOf('.');
        return d > 0 ? t.Substring(0, d) : null;
    }

    private static string ExtractClassName(string f)
    {
        var t = f.StartsWith("global::") ? f.Substring(8) : f;
        var d = t.LastIndexOf('.');
        return d > 0 ? t.Substring(d + 1) : t;
    }

    private static string ConventionString(int conv) => conv switch
    {
        1 => "UnmanagedFunctionPointer(CallingConvention.StdCall)",
        2 => "UnmanagedFunctionPointer(CallingConvention.Cdecl)",
        3 => "UnmanagedFunctionPointer(CallingConvention.ThisCall)",
        4 => "UnmanagedFunctionPointer(CallingConvention.FastCall)",
        _ => "UnmanagedFunctionPointer(CallingConvention.Cdecl)",
    };

    private static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string SimplifyType(string type) => type switch
    {
        "global::System.Void" => "void",
        "global::System.Int32" => "int",
        "global::System.UInt32" => "uint",
        "global::System.Int64" => "long",
        "global::System.UInt64" => "ulong",
        "global::System.Boolean" => "bool",
        "global::System.Byte" => "byte",
        "global::System.SByte" => "sbyte",
        "global::System.Int16" => "short",
        "global::System.UInt16" => "ushort",
        "global::System.Single" => "float",
        "global::System.Double" => "double",
        "global::System.Char" => "char",
        "global::System.String" => "string",
        "global::System.IntPtr" => "nint",
        "global::System.UIntPtr" => "nuint",
        _ => type.StartsWith("global::") ? type.Substring(8) : type,
    };
}
