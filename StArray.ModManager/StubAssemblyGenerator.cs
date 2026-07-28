using System.Runtime.InteropServices;
using System.Text;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Mono;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager;

public static unsafe class StubAssemblyGenerator
{
    private static readonly HashSet<string> SkipAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "mscorlib", "netstandard", "System", "System.Core", "System.Xml",
        "System.Xml.Linq", "System.Configuration", "System.Data", "System.Data.Common",
        "System.Numerics", "System.Runtime", "System.ComponentModel",
        "System.ComponentModel.Composition", "System.ComponentModel.DataAnnotations",
        "System.Threading", "System.Collections", "System.Collections.Concurrent",
        "System.Collections.Specialized", "System.Linq", "System.IO",
        "System.IO.Compression", "System.Reflection", "System.Reflection.Emit",
        "System.Diagnostics", "System.Diagnostics.Process", "System.Diagnostics.TraceSource",
        "System.Security", "System.Security.Cryptography",
        "System.Net", "System.Net.Http", "System.Net.Sockets",
        "System.Text.RegularExpressions", "System.Text.Encoding",
        "System.ObjectModel", "System.Transactions", "System.Web",
        "System.Memory", "System.Buffers", "System.Runtime.CompilerServices.VisualC",
        "System.Runtime.InteropServices", "System.Runtime.Serialization",
        "System.Threading.Tasks", "System.Threading.Thread",
        "Mono.Security",
        "UnityEngine.AIModule", "UnityEngine.ARModule",
        "UnityEngine.ClothModule", "UnityEngine.GameCenterModule",
        "UnityEngine.ImageConversionModule", "UnityEngine.InputModule",
        "UnityEngine.JSONSerializeModule", "UnityEngine.ParticleSystemModule",
        "UnityEngine.PerformanceReportingModule", "UnityEngine.SpriteMaskModule",
        "UnityEngine.SpriteShapeModule", "UnityEngine.StyleSheetsModule",
        "UnityEngine.SubstanceModule", "UnityEngine.TerrainModule",
        "UnityEngine.TerrainPhysicsModule", "UnityEngine.TilemapModule",
        "UnityEngine.TLSModule", "UnityEngine.UnityAnalyticsModule",
        "UnityEngine.UnityConnectModule", "UnityEngine.UnityTestProtocolModule",
        "UnityEngine.VehiclesModule", "UnityEngine.VideoModule",
        "UnityEngine.WindModule",
    };

    private static readonly Dictionary<string, string> TypeMap = new(StringComparer.Ordinal)
    {
        ["System.Void"] = "void",
        ["System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.Char"] = "char",
        ["System.Decimal"] = "decimal",
        ["System.Double"] = "double",
        ["System.Single"] = "float",
        ["System.Int32"] = "int",
        ["System.UInt32"] = "uint",
        ["System.Int64"] = "long",
        ["System.UInt64"] = "ulong",
        ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort",
        ["System.IntPtr"] = "nint",
        ["System.UIntPtr"] = "nuint",
        ["System.String"] = "RuntimeString",
        ["System.Object"] = "nint",
    };

    private static readonly HashSet<string> SpecialNames =
    [
        "Finalize", "MemberwiseClone",
    ];

    public static void GenerateToDir(string outputDir)
    {
        if (!RuntimeManager.IsAvailable)
            RuntimeManager.Detect();

        if (!RuntimeManager.IsAvailable)
        {
            Logger.Error("StubAssemblyGenerator", "No runtime backend detected");
            return;
        }

        var domain = RuntimeManager.GetDomain();
        if (domain == null)
        {
            Logger.Error("StubAssemblyGenerator", "Failed to get app domain");
            return;
        }

        if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
        Directory.CreateDirectory(outputDir);
        Logger.Info("StubAssemblyGenerator", $"Generating stubs to {outputDir}");

        int totalAssemblies = 0;
        int totalClasses = 0;

        // only generate for Assembly-CSharp
        var targetName = "Assembly-CSharp";
        var target = domain.GetAssemblies().FirstOrDefault(a =>
            string.Equals(a.Name, targetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Name, targetName + ".dll", StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            Logger.Error("StubAssemblyGenerator", $"Assembly '{targetName}' not found");
            return;
        }

        try
        {
            ResolvedTypes.Clear();
            CollectAssemblyTypes(target);
            var count = GenerateAssemblyStubs(outputDir, target);
            if (count > 0)
            {
                totalAssemblies++;
                totalClasses += count;
                Logger.Info("StubAssemblyGenerator", $"  {targetName}: {count} classes");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("StubAssemblyGenerator", $"Error processing {targetName}: {ex.Message}");
        }

        Logger.Info("StubAssemblyGenerator",
            $"Done: {totalClasses} classes in {totalAssemblies} assemblies -> {outputDir}");
    }

    private static readonly HashSet<string> ResolvedTypes = new();
    private static readonly Dictionary<nint, nint> _nestingMap = new();
    private static readonly Dictionary<nint, List<nint>> _childrenMap = new();

    private static void CollectAssemblyTypes(IRuntimeAssembly asm)
    {
        if (RuntimeManager.IsIl2Cpp)
            CollectIl2CppTypes((Il2CppAssembly)asm);
        else if (RuntimeManager.IsMono)
            CollectMonoTypes((MonoAssembly)asm);
    }

    private static void CollectIl2CppTypes(Il2CppAssembly asm)
    {
        var image = Il2CppFunctions.il2cpp_assembly_get_image(asm.Ptr);
        if (image == 0) return;
        var classCount = Il2CppFunctions.il2cpp_image_get_class_count(image);

        // first pass: build nesting map and children map
        _nestingMap.Clear();
        _childrenMap.Clear();

        // index all class names for fallback lookup
        var nameToPtr = new Dictionary<string, nint>();
        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;
            var cname = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            nameToPtr[cname] = klass;
        }

        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;

            // 1. use il2cpp_class_get_nested_types API
            var iter = IntPtr.Zero;
            while (true)
            {
                var nested = Il2CppFunctions.il2cpp_class_get_nested_types(klass, ref iter);
                if (nested == 0) break;
                if (!_nestingMap.ContainsKey(nested))
                {
                    _nestingMap[nested] = klass;
                    if (!_childrenMap.ContainsKey(klass))
                        _childrenMap[klass] = new List<nint>();
                    _childrenMap[klass].Add(nested);
                }
            }

            // 2. fallback: detect nesting from name with '+'
            var className = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            if (className.Contains('+') && !_nestingMap.ContainsKey(klass))
            {
                var parts = className.Split('+');
                var parentName = string.Join("+", parts, 0, parts.Length - 1);
                if (nameToPtr.TryGetValue(parentName, out var parentPtr))
                {
                    _nestingMap[klass] = parentPtr;
                    if (!_childrenMap.ContainsKey(parentPtr))
                        _childrenMap[parentPtr] = new List<nint>();
                    _childrenMap[parentPtr].Add(klass);
                }
            }
        }

        // second pass: add all type names using nesting map
        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;
            var (ns, name) = GetClassNamespaceAndName(klass);
            if (string.IsNullOrEmpty(name)) continue;
            var safeNs = SanitizeNamespace(ns);
            var safeName = SanitizeIdentifier(name);
            var fullName = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
            ResolvedTypes.Add(fullName);
        }
    }

    private static void CollectMonoTypes(MonoAssembly asm)
    {
        var image = MonoFunctions.MonoAssemblyGetImage(asm.Ptr);
        if (image == 0) return;
        var table = Methods.mono_image_get_table_info((_MonoImage*)image,
            (int)MonoMetaTableEnum.MONO_TABLE_TYPEDEF);
        if (table == null) return;
        var rows = Methods.mono_table_info_get_rows(table);

        _nestingMap.Clear();
        _childrenMap.Clear();
        for (int i = 1; i <= rows; i++)
        {
            var klass = Methods.mono_class_get((_MonoImage*)image, (uint)i);
            if (klass == null) continue;
            var parent = MonoFunctions.MonoClassGetNestingType((nint)klass);
            if (parent != 0)
            {
                _nestingMap[(nint)klass] = parent;
                if (!_childrenMap.ContainsKey(parent))
                    _childrenMap[parent] = new List<nint>();
                _childrenMap[parent].Add((nint)klass);
            }
        }

        for (int i = 1; i <= rows; i++)
        {
            var klass = Methods.mono_class_get((_MonoImage*)image, (uint)i);
            if (klass == null) continue;
            var (ns, name) = GetClassNamespaceAndName((nint)klass);
            if (string.IsNullOrEmpty(name)) continue;
            var safeNs = SanitizeNamespace(ns);
            var safeName = SanitizeIdentifier(name);
            var fullName = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
            ResolvedTypes.Add(fullName);
        }
    }

    private static int GenerateAssemblyStubs(string outputDir, IRuntimeAssembly asm)
    {
        if (RuntimeManager.IsIl2Cpp)
            return GenerateIl2CppStubs(outputDir, asm);
        if (RuntimeManager.IsMono)
            return GenerateMonoStubs(outputDir, asm);
        return 0;
    }

    private static int GenerateIl2CppStubs(string outputDir, IRuntimeAssembly asm)
    {
        var il2cppAsm = (Il2CppAssembly)asm;
        var image = Il2CppFunctions.il2cpp_assembly_get_image(il2cppAsm.Ptr);
        if (image == 0) return 0;

        var imageNamePtr = Il2CppFunctions.il2cpp_image_get_name(image);
        var imageName = Marshal.PtrToStringAnsi(imageNamePtr) ?? "Unknown";

        var classCount = Il2CppFunctions.il2cpp_image_get_class_count(image);
        int written = 0;

        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;
            if (_nestingMap.ContainsKey(klass)) continue;

            try
            {
                if (WriteClassStub(outputDir, imageName, klass))
                    written++;
            }
            catch
            {
                // skip problematic classes
            }
        }

        return written;
    }

    private static int GenerateMonoStubs(string outputDir, IRuntimeAssembly asm)
    {
        var monoAsm = (MonoAssembly)asm;
        var image = MonoFunctions.MonoAssemblyGetImage(monoAsm.Ptr);
        if (image == 0) return 0;

        var imageName = MonoFunctions.MonoImageGetName(image) ?? "Unknown";

        var table = Methods.mono_image_get_table_info((_MonoImage*)image,
            (int)MonoMetaTableEnum.MONO_TABLE_TYPEDEF);
        if (table == null) return 0;

        var rows = Methods.mono_table_info_get_rows(table);
        int written = 0;

        for (int i = 1; i <= rows; i++)
        {
            var klass = Methods.mono_class_get((_MonoImage*)image, (uint)i);
            if (klass == null) continue;
            if (_nestingMap.ContainsKey((nint)klass)) continue;

            try
            {
                if (WriteClassStub(outputDir, imageName, (nint)klass))
                    written++;
            }
            catch
            {
                // skip problematic classes
            }
        }

        return written;
    }

    private static bool WriteClassStub(string outputDir, string assemblyName, nint klassPtr)
    {
        if (_nestingMap.ContainsKey(klassPtr)) return false;

        var (ns, name) = GetClassNamespaceAndName(klassPtr);
        if (string.IsNullOrEmpty(name)) return false;

        // skip special types
        if (name.StartsWith('<') || name.StartsWith("__") ||
            name is "<Module>" or "<PrivateImplementationDetails>" or "Array" or "ValueType" or "Enum")
            return false;

        if (IsGeneric(klassPtr)) return false;

        var safeNs = SanitizeNamespace(ns);
        var safeName = SanitizeIdentifier(name);
        var safeAssembly = SanitizeIdentifier(Path.GetFileNameWithoutExtension(assemblyName));

        // group by namespace → manage file count
        var dir = Path.Combine(outputDir, safeAssembly);
        if (!string.IsNullOrEmpty(safeNs))
        {
            foreach (var part in safeNs.Split('.'))
                dir = Path.Combine(dir, part);
        }
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{safeName}.cs");
        if (File.Exists(filePath)) return false; // already generated (should not happen)

        using var w = new StreamWriter(filePath, false, Encoding.UTF8);

        w.WriteLine("using StArray.ModManager.RuntimeAbstractions;");
        w.WriteLine();

        if (!string.IsNullOrEmpty(safeNs))
            w.WriteLine($"namespace {safeNs};");
        w.WriteLine();

        WriteTypeBody(w, assemblyName, klassPtr, ns, name, "");
        return true;
    }

    private static void WriteTypeBody(StreamWriter w, string assemblyName, nint klassPtr,
        string ns, string fullName, string indent)
    {
        var innerName = GetSimpleClassName(klassPtr);
        if (string.IsNullOrEmpty(innerName)) return;
        var safeInner = SanitizeIdentifier(innerName);
        if (safeInner.StartsWith('<')) return;

        var childIndent = indent + "    ";
        var baseType = GetBaseType(klassPtr);

        w.WriteLine($"{indent}[UnmanagedType(\"{EscapeString(assemblyName)}\", \"{EscapeString(ns)}\", \"{EscapeString(fullName)}\")]");
        w.WriteLine($"{indent}public partial class {safeInner} : {baseType}");
        w.WriteLine($"{indent}{{");

        w.WriteLine($"{childIndent}public {safeInner}(nint ptr) : base(ptr) {{ }}");
        w.WriteLine();

        var methods = EnumerateMethods(klassPtr).ToList();
        var fields = EnumerateFields(klassPtr).ToList();

        // separate add_/remove_ event methods from regular methods
        var regularMethods = new List<(string name, string returnType, string[] paramTypes, bool isStatic)>();
        var eventNames = new HashSet<string>();
        var addMethods = new Dictionary<string, (string name, string returnType, string[] paramTypes, bool isStatic)>();
        var removeMethods = new Dictionary<string, (string name, string returnType, string[] paramTypes, bool isStatic)>();
        foreach (var m in methods)
        {
            if (SpecialNames.Contains(m.name)) { regularMethods.Add(m); continue; }
            if (m.name.StartsWith("add_"))
            {
                var key = m.name.Substring(4);
                eventNames.Add(key);
                addMethods[key] = m;
            }
            else if (m.name.StartsWith("remove_"))
            {
                var key = m.name.Substring(7);
                eventNames.Add(key);
                removeMethods[key] = m;
            }
            else
                regularMethods.Add(m);
        }

        // regular methods as [UnmanagedMember] partial methods
        // (get_/set_ accessors are private; other methods are public)
        foreach (var m in regularMethods)
        {
            var cleanName = SanitizeIdentifier(m.name);
            var staticStr = m.isStatic ? "static " : "";
            var access = (m.name.StartsWith("get_") || m.name.StartsWith("set_")) ? "private" : "public";
            var sig = $"[UnmanagedMember] {access} {staticStr}partial {m.returnType} {cleanName}(";

            var pars = new List<string>();
            for (int i = 0; i < m.paramTypes.Length; i++)
                pars.Add($"{m.paramTypes[i]} arg{i}");
            sig += string.Join(", ", pars) + ");";

            w.WriteLine($"{childIndent}{sig}");
        }

        // event add_/remove_ → empty stub + event wrapper
        foreach (var eventName in eventNames)
        {
            addMethods.TryGetValue(eventName, out var addM);
            removeMethods.TryGetValue(eventName, out var removeM);
            var m = addM.name != null ? addM : removeM;
            var eventType = m.paramTypes.Length > 0 ? m.paramTypes[0] : "nint";
            var staticStr = m.isStatic ? "static " : "";
            if (addM.name != null)
                w.WriteLine($"{childIndent}private {staticStr}void add_{eventName}({eventType} value) {{ }}");
            if (removeM.name != null)
                w.WriteLine($"{childIndent}private {staticStr}void remove_{eventName}({eventType} value) {{ }}");
            w.WriteLine($"{childIndent}public {staticStr}event {eventType} {SanitizeIdentifier(eventName)}E");
            w.WriteLine($"{childIndent}{{");
            w.WriteLine($"{childIndent}    add => add_{eventName}(value);");
            w.WriteLine($"{childIndent}    remove => remove_{eventName}(value);");
            w.WriteLine($"{childIndent}}}");
        }

        // fields as inline properties (skip if already covered by property accessor methods)
        var methodNames = methods.Select(m => m.name).ToHashSet();
        var hasStaticField = false;
        var hasInstanceField = false;
        foreach (var f in fields)
        {
            if (f.name.StartsWith('<')) continue;
            if (methodNames.Contains($"get_{f.name}")) continue;
            var sf = SanitizeIdentifier(f.name);
            var ft = f.typeName;
            var st = f.isStatic ? "static " : "";
            if (f.isStatic)
            {
                hasStaticField = true;
                w.WriteLine($"{childIndent}public {st}{ft} {sf}");
                w.WriteLine($"{childIndent}{{");
                w.WriteLine($"{childIndent}    get => GetClass().GetField(\"{f.name}\").GetValue<{ft}>(0);");
                w.WriteLine($"{childIndent}    set => GetClass().GetField(\"{f.name}\").SetValue(0, value);");
                w.WriteLine($"{childIndent}}}");
            }
            else
            {
                hasInstanceField = true;
                w.WriteLine($"{childIndent}public {ft} {sf}");
                w.WriteLine($"{childIndent}{{");
                w.WriteLine($"{childIndent}    get => Obj.GetField<{ft}>(\"{f.name}\");");
                w.WriteLine($"{childIndent}    set => Obj.SetField<{ft}>(\"{f.name}\", value);");
                w.WriteLine($"{childIndent}}}");
            }
        }

        if (hasInstanceField)
            w.WriteLine();
        if (hasInstanceField)
            w.WriteLine($"{childIndent}private RuntimeObject Obj => new(Ptr);");
        if (hasStaticField)
        {
            w.WriteLine();
            w.WriteLine($"{childIndent}private static IRuntimeClass? GetClass()");
            w.WriteLine($"{childIndent}{{");
            w.WriteLine($"{childIndent}    var domain = RuntimeManager.GetDomain();");
            w.WriteLine($"{childIndent}    if (domain == null) return null;");
            w.WriteLine($"{childIndent}    var asm = domain.OpenAssembly(\"{EscapeString(assemblyName)}\");");
            w.WriteLine($"{childIndent}    return asm?.GetClass(\"{EscapeString(ns)}\", \"{EscapeString(fullName)}\");");
            w.WriteLine($"{childIndent}}}");
        }

        // nested types
        if (_childrenMap.TryGetValue(klassPtr, out var children))
        {
            foreach (var child in children)
            {
                w.WriteLine();
                var (childNs, childName) = GetClassNamespaceAndName(child);
                WriteTypeBody(w, assemblyName, child, childNs, childName, childIndent);
            }
        }

        w.WriteLine($"{indent}}}");
    }

    private static (string ns, string name) GetClassNamespaceAndName(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
        {
            var ns = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_namespace(klassPtr)) ?? "";
            var name = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klassPtr)) ?? "";
            name = PrependNestingChain(name, klassPtr, il2cpp: true);
            return (ns, name);
        }

        if (RuntimeManager.IsMono)
        {
            var ns = MonoFunctions.MonoClassGetNamespace(klassPtr) ?? "";
            var name = MonoFunctions.MonoClassGetName(klassPtr) ?? "";
            name = PrependNestingChain(name, klassPtr, il2cpp: false);
            return (ns, name);
        }

        return ("", "");
    }

    private static string GetSimpleClassName(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
        {
            var raw = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klassPtr)) ?? "";
            var idx = raw.LastIndexOf('+');
            return idx >= 0 ? raw.Substring(idx + 1) : raw;
        }
        if (RuntimeManager.IsMono)
            return MonoFunctions.MonoClassGetName(klassPtr) ?? "";
        return "";
    }

    private static string PrependNestingChain(string innerName, nint klassPtr, bool il2cpp)
    {
        if (il2cpp)
        {
            var current = klassPtr;
            while (_nestingMap.TryGetValue(current, out var outer))
            {
                var outerName = GetSimpleClassName(outer);
                if (!string.IsNullOrEmpty(outerName))
                    innerName = $"{outerName}.{innerName}";
                current = outer;
            }
        }
        else
        {
            var current = klassPtr;
            while (true)
            {
                var outer = MonoFunctions.MonoClassGetNestingType(current);
                if (outer == 0) break;
                var outerName = MonoFunctions.MonoClassGetName(outer) ?? "";
                if (!string.IsNullOrEmpty(outerName))
                    innerName = $"{outerName}.{innerName}";
                current = outer;
            }
        }
        return innerName;
    }

    private static bool IsInterface(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return Il2CppFunctions.il2cpp_class_is_interface(klassPtr);
        if (RuntimeManager.IsMono)
            return (MonoFunctions.MonoClassGetFlags(klassPtr) & 0x20) != 0;
        return true;
    }

    private static bool IsGeneric(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return Il2CppFunctions.il2cpp_class_is_generic(klassPtr);
        // Mono generic detection via flags
        if (RuntimeManager.IsMono)
            return false; // skip generic detection for mono for now
        return true;
    }

    private static bool IsTypeResolved(nint typePtr, bool il2cpp)
    {
        if (typePtr == 0) return false;
        try
        {
            nint cls;
            if (il2cpp)
            {
                cls = Il2CppFunctions.il2cpp_class_from_type(typePtr);
            }
            else
            {
                cls = MonoFunctions.MonoTypeGetClass(typePtr);
            }
            if (cls == 0) return false;
            var (ns, name) = GetClassNamespaceAndName(cls);
            if (string.IsNullOrEmpty(name)) return false;
            var safeNs = SanitizeNamespace(ns);
            var safeName = SanitizeIdentifier(name);
            var fullName = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
            return ResolvedTypes.Contains(fullName);
        }
        catch
        {
            return false;
        }
    }

    private static string GetBaseType(nint klassPtr)
    {
        nint parent;
        if (RuntimeManager.IsIl2Cpp)
            parent = Il2CppFunctions.il2cpp_class_get_parent(klassPtr);
        else if (RuntimeManager.IsMono)
            parent = MonoFunctions.MonoClassGetParent(klassPtr);
        else
            return "UnmanagedObject";

        if (parent == 0) return "UnmanagedObject";
        var (ns, name) = GetClassNamespaceAndName(parent);
        if (string.IsNullOrEmpty(name)) return "UnmanagedObject";
        var safeNs = SanitizeNamespace(ns);
        var safeName = SanitizeIdentifier(name);
        var fullName = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
        return ResolvedTypes.Contains(fullName) ? fullName : "UnmanagedObject";
    }

    private static IEnumerable<(string name, string returnType, string[] paramTypes, bool isStatic)> EnumerateMethods(
        nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return EnumerateIl2CppMethods(klassPtr);
        if (RuntimeManager.IsMono)
            return EnumerateMonoMethods(klassPtr);
        return [];
    }

    private static IEnumerable<(string name, string returnType, string[] paramTypes, bool isStatic)>
        EnumerateIl2CppMethods(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var m = Il2CppFunctions.il2cpp_class_get_methods(klassPtr, ref iter);
            if (m == 0) break;

            var namePtr = Il2CppFunctions.il2cpp_method_get_name(m);
            var name = Marshal.PtrToStringAnsi(namePtr) ?? "";
            if (name == "" || name.StartsWith('.') || name.StartsWith('_')) continue;

            uint pc = Il2CppFunctions.il2cpp_method_get_param_count(m);
            uint _flagsDummy = 0;
            uint flags = Il2CppFunctions.il2cpp_method_get_flags(m, ref _flagsDummy);
            bool isStatic = (flags & 0x10) != 0;

            var retType = "void";
            var retTypePtr = Il2CppFunctions.il2cpp_method_get_return_type(m);
            if (retTypePtr != 0)
            {
                var rnPtr = Il2CppFunctions.il2cpp_type_get_name(retTypePtr);
                var raw = Marshal.PtrToStringAnsi(rnPtr) ?? "";
                retType = ResolveTypeName(retTypePtr, raw, true);
            }

            var paramTypes = new string[pc];
            for (uint i = 0; i < pc; i++)
            {
                var p = Il2CppFunctions.il2cpp_method_get_param(m, i);
                if (p != 0)
                {
                    var pnPtr = Il2CppFunctions.il2cpp_type_get_name(p);
                    paramTypes[i] = ResolveTypeName(p, Marshal.PtrToStringAnsi(pnPtr) ?? "", true);
                }
                else
                {
                    paramTypes[i] = "nint";
                }
            }

            yield return (name, retType, paramTypes, isStatic);
        }
    }

    private static IEnumerable<(string name, string returnType, string[] paramTypes, bool isStatic)>
        EnumerateMonoMethods(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var m = MonoFunctions.MonoClassGetMethods(klassPtr, ref iter);
            if (m == 0) break;

            var name = MonoFunctions.MonoMethodGetName(m);
            if (string.IsNullOrEmpty(name) || name.StartsWith('.') || name.StartsWith('_')) continue;

            var sig = MonoFunctions.MonoMethodSignature(m);
            if (sig == 0) continue;

            uint pc = MonoFunctions.MonoSignatureGetParamCount(sig);
            uint flags = MonoFunctions.MonoMethodGetFlags(m);
            bool isStatic = (flags & 0x10) != 0;

            var retType = "void";
            var retTypePtr = MonoFunctions.MonoSignatureGetReturnType(sig);
            if (retTypePtr != 0)
            {
                var raw = MonoFunctions.MonoTypeGetName(retTypePtr) ?? "";
                retType = ResolveTypeName(retTypePtr, raw, false);
            }

            var paramTypes = GetMonoMethodParamTypes(sig, pc);

            yield return (name, retType, paramTypes, isStatic);
        }
    }

    private static unsafe string[] GetMonoMethodParamTypes(nint sig, uint count)
    {
        var types = new string[count];
        void* iter = null;
        for (uint i = 0; i < count; i++)
        {
            var pt = MonoFunctions.MonoSignatureGetParams(sig, ref iter);
            if (pt != 0)
                types[i] = ResolveTypeName(pt, MonoFunctions.MonoTypeGetName(pt) ?? "", false);
            else
                types[i] = "nint";
        }
        return types;
    }

    private static string ResolveTypeName(nint typePtr, string rawTypeName, bool il2cpp)
    {
        if (typePtr == 0 || string.IsNullOrEmpty(rawTypeName)) return "nint";

        nint cls = il2cpp
            ? Il2CppFunctions.il2cpp_class_from_type(typePtr)
            : MonoFunctions.MonoTypeGetClass(typePtr);
        if (cls == 0) return MapType(rawTypeName);

        // check array via raw type name (e.g. "SomeType[]", "bool[,]")
        if (rawTypeName.EndsWith("]"))
        {
            nint elemCls = il2cpp
                ? Il2CppFunctions.il2cpp_class_get_element_class(cls)
                : MonoFunctions.MonoClassGetElementClass(cls);
            if (elemCls != 0)
            {
                var elemTypeName = ResolveArrayElementType(elemCls);
                if (elemTypeName != null)
                    return $"RuntimeArray<{elemTypeName}>";
            }
            return "RuntimeArray<nint>";
        }

        // non-array: check resolved types
        var (ns, name) = GetClassNamespaceAndName(cls);
        var safeNs = SanitizeNamespace(ns);
        var safeName = SanitizeIdentifier(name);
        var fullName = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
        if (ResolvedTypes.Contains(fullName))
            return fullName;

        return MapType(rawTypeName);
    }

    private static string? ResolveArrayElementType(nint elemCls)
    {
        var (ens, ename) = GetClassNamespaceAndName(elemCls);
        var safeEns = SanitizeNamespace(ens);
        var safeEname = SanitizeIdentifier(ename);
        var elemFullName = string.IsNullOrEmpty(safeEns) ? safeEname : $"{safeEns}.{safeEname}";
        if (ResolvedTypes.Contains(elemFullName))
            return elemFullName;

        // fallback: element type might use a different name format
        return null;
    }

    private static IEnumerable<(string name, bool isStatic, string typeName)> EnumerateFields(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return EnumerateIl2CppFields(klassPtr);
        if (RuntimeManager.IsMono)
            return EnumerateMonoFields(klassPtr);
        return [];
    }

    private static IEnumerable<(string name, bool isStatic, string typeName)> EnumerateIl2CppFields(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var f = Il2CppFunctions.il2cpp_class_get_fields(klassPtr, ref iter);
            if (f == 0) break;

            var namePtr = Il2CppFunctions.il2cpp_field_get_name(f);
            var name = Marshal.PtrToStringAnsi(namePtr) ?? "";
            if (name == "") continue;

            var flags = Il2CppFunctions.il2cpp_field_get_flags(f);
            bool isStatic = (flags & 0x10) != 0;

            var typeName = "nint";
            var typePtr = Il2CppFunctions.il2cpp_field_get_type(f);
            if (typePtr != 0)
            {
                var tnPtr = Il2CppFunctions.il2cpp_type_get_name(typePtr);
                var raw = Marshal.PtrToStringAnsi(tnPtr) ?? "nint";
                typeName = ResolveTypeName(typePtr, raw, true);
            }

            yield return (name, isStatic, typeName);
        }
    }

    private static IEnumerable<(string name, bool isStatic, string typeName)> EnumerateMonoFields(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var f = MonoFunctions.MonoClassGetFields(klassPtr, ref iter);
            if (f == 0) break;

            var name = MonoFunctions.MonoFieldGetName(f);
            if (string.IsNullOrEmpty(name)) continue;

            var flags = MonoFunctions.MonoFieldGetFlags(f);
            bool isStatic = (flags & 0x10) != 0;

            var typeName = "nint";
            var typePtr = MonoFunctions.MonoFieldGetType(f);
            if (typePtr != 0)
            {
                var raw = MonoFunctions.MonoTypeGetName(typePtr) ?? "nint";
                typeName = ResolveTypeName(typePtr, raw, false);
            }

            yield return (name, isStatic, typeName);
        }
    }

    internal static string MapType(string runtimeTypeName)
    {
        if (string.IsNullOrEmpty(runtimeTypeName)) return "nint";
        if (TypeMap.TryGetValue(runtimeTypeName, out var mapped)) return mapped;
        return "nint";
    }

    private static string MapType(string runtimeTypeName, bool isEnum)
    {
        if (!isEnum) return MapType(runtimeTypeName);
        return SimplifyType(runtimeTypeName);
    }

    private static string SimplifyType(string type)
    {
        if (type.StartsWith("global::")) return type.Substring(8);
        return type;
    }

    internal static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";

        var sb = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                sb.Append(c);
            else
                sb.Append('_');
        }

        var result = sb.ToString();
        if (result.Length == 0) return "_";
        if (char.IsDigit(result[0])) result = "_" + result;

        return result;
    }

    internal static string SanitizeNamespace(string ns)
    {
        if (string.IsNullOrEmpty(ns)) return "";
        var parts = ns.Split('.');
        for (int i = 0; i < parts.Length; i++)
            parts[i] = SanitizeIdentifier(parts[i]);
        return string.Join(".", parts.Where(p => p.Length > 0));
    }

    internal static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
