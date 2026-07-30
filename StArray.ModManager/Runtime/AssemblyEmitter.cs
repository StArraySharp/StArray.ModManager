using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;
using StArray.ModManager.Mono;
using StArray.ModManager.Runtime;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager;

public static unsafe class AssemblyEmitter
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

    private const string ArrayPrefix = "RuntimeArray<";

    /// <summary>Assembly, mod id and folder name of the generated output.</summary>
    public const string OutputName = "UnmanagedTypeAssembly";

    private const string OutputFileName = OutputName + ".dll";
    private const string ModDisplayName = "Unmanaged Type Assembly";

    private static readonly HashSet<string> SpecialNames =
    [
        "Finalize", "MemberwiseClone",
    ];

    // ── Shared metadata state (cleared per GenerateToDir call) ──
    private static readonly HashSet<string> ResolvedTypes = new(StringComparer.Ordinal);
    private static readonly Dictionary<nint, nint> _nestingMap = new();
    private static readonly Dictionary<nint, List<nint>> _childrenMap = new();
    private static readonly Dictionary<nint, TypeBuilder> _typeBuilders = new();
    private static readonly Dictionary<string, TypeBuilder> _nameToTypeBuilder = new(StringComparer.Ordinal);
    private static readonly Dictionary<nint, ConstructorBuilder> _typeCtors = new();
    private static readonly Dictionary<TypeBuilder, ConstructorBuilder> _ctorByBuilder = new();
    private static readonly Dictionary<nint, string> _asmOfType = new();
    private static readonly Dictionary<nint, nint> _emittedBase = new();
    private static readonly HashSet<nint> _pendingTypes = new();
    private static readonly HashSet<nint> _defining = new();
    private static readonly HashSet<string> _usedTypeNames = new(StringComparer.Ordinal);
    private static readonly Dictionary<nint, string[]> _nameChainCache = new();
    private static readonly Dictionary<Type, Type> _enumUnderlying = new();

    // Reusable reflection handles
    private static Type _unmanagedObjectType = null!;
    private static ConstructorInfo _unmanagedObjectCtor = null!;
    private static ConstructorInfo _unmanagedTypeAttrCtor = null!;
    private static ConstructorInfo _typeNameAttrCtor = null!;
    private static MethodInfo _get_Ptr = null!;
    private static MethodInfo _instanceVoid = null!;
    private static MethodInfo _instanceRet = null!;
    private static MethodInfo _staticVoid = null!;
    private static MethodInfo _staticRet = null!;
    private static MethodInfo _instanceRetUnboxDef = null!;
    private static MethodInfo _staticRetUnboxDef = null!;
    private static readonly Dictionary<Type, MethodInfo> _instanceRetUnboxCache = new();
    private static readonly Dictionary<Type, MethodInfo> _staticRetUnboxCache = new();
    private static MethodInfo _getFieldDef = null!;
    private static MethodInfo _setFieldDef = null!;
    private static MethodInfo _getStaticFieldDef = null!;
    private static MethodInfo _setStaticFieldDef = null!;
    private static readonly Dictionary<Type, MethodInfo> _getFieldCache = new();
    private static readonly Dictionary<Type, MethodInfo> _setFieldCache = new();
    private static readonly Dictionary<Type, MethodInfo> _getStaticFieldCache = new();
    private static readonly Dictionary<Type, MethodInfo> _setStaticFieldCache = new();

    private static ModuleBuilder _module = null!;

    /// <summary>
    /// Generates the assembly as a self-contained mod inside <paramref name="modsDirectory"/>,
    /// laid out as <c>&lt;mods&gt;/UnmanagedTypeAssembly/UnmanagedTypeAssembly.dll</c> — the
    /// folder-name-matches-dll-name shape ModLoader looks for.
    /// </summary>
    /// <returns>The mod folder, or null if generation failed.</returns>
    public static string? GenerateToMods(string modsDirectory)
    {
        var modDir = Path.Combine(modsDirectory, OutputName);
        return GenerateToDir(modDir, asModDll: true) != null ? modDir : null;
    }

    /// <param name="outputDir">Directory the assembly is written to; created if missing.</param>
    /// <param name="asModDll">
    /// When true the output also carries an <see cref="IModPlugin"/> entry point, so the DLL can be
    /// dropped straight into a Mods folder and loaded. When false it is a plain stub assembly,
    /// only useful as a compile-time reference.
    /// </param>
    /// <returns>Path of the written assembly, or null if generation failed.</returns>
    public static string? GenerateToDir(string outputDir, bool asModDll = false)
    {
        // Reading enum literals boxes values, which needs the thread attached to the runtime.
        if (RuntimeManager.IsIl2Cpp) Il2CppDomain.Current?.ThreadAttach();
        try
        {
            return GenerateCore(outputDir, asModDll);
        }
        finally
        {
            if (RuntimeManager.IsIl2Cpp) Il2CppDomain.Current?.ThreadDetach();
        }
    }

    private static string? GenerateCore(string outputDir, bool asModDll)
    {
        if (!RuntimeManager.IsAvailable)
            RuntimeManager.Detect();

        if (!RuntimeManager.IsAvailable)
        {
            Logger.Error("AssemblyEmitter", "No runtime backend detected");
            return null;
        }

        var domain = RuntimeManager.GetDomain();
        if (domain == null)
        {
            Logger.Error("AssemblyEmitter", "Failed to get app domain");
            return null;
        }

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        Logger.Info("AssemblyEmitter", $"Emitting to {outputDir}");

        InitReflectionHandles();

        // Get all non-skipped assemblies
        var assemblies = domain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Name) && !SkipAssemblies.Contains(StripDll(a.Name!)))
            .ToList();

        if (assemblies.Count == 0)
        {
            Logger.Warn("AssemblyEmitter", "No game assemblies found");
            return null;
        }

        var asmName = new AssemblyName(OutputName);
        var coreAssembly = typeof(object).Assembly;
        var assembly = new PersistedAssemblyBuilder(asmName, coreAssembly);
        _module = assembly.DefineDynamicModule("MainModule");

        ResetState();

        // Phase 1: collect type metadata from all assemblies
        foreach (var asm in assemblies)
            CollectAssemblyTypes(asm);

        // Phase 2: define all TypeBuilders (base types resolved on demand, so
        // declaration order no longer decides whether a real base type is used)
        foreach (var klass in _pendingTypes.ToList())
            EnsureTypeDefined(klass);

        // Phase 2.5: wire up interfaces once every TypeBuilder exists
        foreach (var (klass, tb) in _typeBuilders.ToList())
            AddInterfaces(tb, klass);

        // Phase 3: emit method bodies for all types
        foreach (var (klass, tb) in _typeBuilders.ToList())
            EmitTypeBody(tb, klass);

        // Commit: nested types before their declaring type, base types before subclasses
        foreach (var tb in BuildCreationOrder())
            tb.CreateTypeInfo();

        if (asModDll)
            EmitModEntryPoint(assembly);

        var outPath = Path.Combine(outputDir, OutputFileName);
        assembly.Save(outPath);

        Logger.Info("AssemblyEmitter",
            $"Saved: {outPath} ({_typeBuilders.Count} types, {_enumUnderlying.Count} enums" +
            (asModDll ? ", mod entry point" : "") + ")");
        return outPath;
    }

    // ── Mod entry point ──

    /// <summary>
    /// Emits a minimal <see cref="IModPlugin"/> implementation so ModLoader can pick the assembly
    /// up: it scans for the first concrete IModPlugin type and instantiates it with a
    /// parameterless constructor, reading the metadata off these properties.
    /// OnBackgroundGUI/OnForegroundGUI are default interface methods and need no implementation.
    /// </summary>
    private static void EmitModEntryPoint(PersistedAssemblyBuilder assembly)
    {
        var iface = typeof(IModPlugin);
        var entryName = OutputName + "Mod";
        var tb = _module.DefineType(UniqueTypeName(entryName, entryName),
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(object));
        tb.AddInterfaceImplementation(iface);

        var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
            Type.EmptyTypes);
        var cIl = ctor.GetILGenerator();
        cIl.Emit(OpCodes.Ldarg_0);
        cIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        cIl.Emit(OpCodes.Ret);

        foreach (var (prop, value) in new[]
                 {
                     ("Id", OutputName),
                     ("Name", ModDisplayName),
                     ("Version", "1.0.0"),
                     ("Author", "StArray.ModManager"),
                     ("Description",
                         "Managed stubs generated from the game's unmanaged types."),
                 })
            EmitConstantStringProperty(tb, iface, prop, value);

        // Dependencies => Array.Empty<string>()
        var depType = typeof(IReadOnlyList<string>);
        var depGet = tb.DefineMethod("get_Dependencies", InterfaceImplAttrs, depType, Type.EmptyTypes);
        var dIl = depGet.GetILGenerator();
        dIl.Emit(OpCodes.Call, typeof(Array).GetMethod("Empty")!.MakeGenericMethod(typeof(string)));
        dIl.Emit(OpCodes.Ret);
        tb.DefineProperty("Dependencies", System.Reflection.PropertyAttributes.None, depType, null)
            .SetGetMethod(depGet);
        tb.DefineMethodOverride(depGet, iface.GetProperty("Dependencies")!.GetGetMethod()!);

        foreach (var hook in new[] { "OnLoad", "OnUnload" })
        {
            var mb = tb.DefineMethod(hook, InterfaceImplAttrs, typeof(void), Type.EmptyTypes);
            mb.GetILGenerator().Emit(OpCodes.Ret);
            tb.DefineMethodOverride(mb, iface.GetMethod(hook)!);
        }

        var created = tb.CreateTypeInfo();

        // Point the loader straight at the entry type so it never has to enumerate the
        // tens of thousands of stub types just to find one plugin.
        assembly.SetCustomAttribute(new CustomAttributeBuilder(
            typeof(ModEntryPointAttribute).GetConstructor([typeof(Type)])!,
            [created.AsType()]));
    }

    private const MethodAttributes InterfaceImplAttrs =
        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual |
        MethodAttributes.NewSlot | MethodAttributes.Final;

    private static void EmitConstantStringProperty(TypeBuilder tb, Type iface, string name, string value)
    {
        var getter = tb.DefineMethod($"get_{name}", InterfaceImplAttrs | MethodAttributes.SpecialName,
            typeof(string), Type.EmptyTypes);
        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldstr, value);
        il.Emit(OpCodes.Ret);

        tb.DefineProperty(name, System.Reflection.PropertyAttributes.None, typeof(string), null)
            .SetGetMethod(getter);
        tb.DefineMethodOverride(getter, iface.GetProperty(name)!.GetGetMethod()!);
    }

    private static void InitReflectionHandles()
    {
        _unmanagedObjectType = typeof(UnmanagedObject);
        _unmanagedObjectCtor = _unmanagedObjectType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, [typeof(nint)], null)!;
        _unmanagedTypeAttrCtor = typeof(UnmanagedTypeAttribute)
            .GetConstructor([typeof(string), typeof(string), typeof(string)])!;
        _typeNameAttrCtor = typeof(UnmanagedTypeNameAttribute)
            .GetConstructor([typeof(string)])!;
        _get_Ptr = _unmanagedObjectType.GetProperty("Ptr")!.GetGetMethod()!;
        _instanceVoid = typeof(RuntimeHelpers)
            .GetMethod("InstanceVoid", [typeof(nint), typeof(string), typeof(int), typeof(nint[])])!;
        _instanceRet = typeof(RuntimeHelpers)
            .GetMethod("InstanceRet", [typeof(nint), typeof(string), typeof(int), typeof(nint[])])!;
        _staticVoid = typeof(RuntimeHelpers)
            .GetMethod("StaticVoid", [typeof(string), typeof(string), typeof(string), typeof(string), typeof(int), typeof(nint[])])!;
        _staticRet = typeof(RuntimeHelpers)
            .GetMethod("StaticRet", [typeof(string), typeof(string), typeof(string), typeof(string), typeof(int), typeof(nint[])])!;

        var helpers = typeof(RuntimeHelpers);
        _instanceRetUnboxDef = helpers.GetMethods().First(m =>
            m.Name == "InstanceRetUnbox" && m.IsGenericMethodDefinition);
        _staticRetUnboxDef = helpers.GetMethods().First(m =>
            m.Name == "StaticRetUnbox" && m.IsGenericMethodDefinition);
        _getFieldDef = helpers.GetMethods().First(m =>
            m.Name == "GetField" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
        _setFieldDef = helpers.GetMethods().First(m =>
            m.Name == "SetField" && m.IsGenericMethodDefinition && m.GetParameters().Length == 3);
        _getStaticFieldDef = helpers.GetMethods().First(m =>
            m.Name == "GetStaticField" && m.IsGenericMethodDefinition && m.GetParameters().Length == 4);
        _setStaticFieldDef = helpers.GetMethods().First(m =>
            m.Name == "SetStaticField" && m.IsGenericMethodDefinition && m.GetParameters().Length == 5);

        _getFieldCache.Clear();
        _setFieldCache.Clear();
        _getStaticFieldCache.Clear();
        _setStaticFieldCache.Clear();
        _instanceRetUnboxCache.Clear();
        _staticRetUnboxCache.Clear();
    }

    private static void ResetState()
    {
        _typeBuilders.Clear();
        _nameToTypeBuilder.Clear();
        _typeCtors.Clear();
        _ctorByBuilder.Clear();
        _nestingMap.Clear();
        _childrenMap.Clear();
        _asmOfType.Clear();
        _emittedBase.Clear();
        _pendingTypes.Clear();
        _defining.Clear();
        _usedTypeNames.Clear();
        _nameChainCache.Clear();
        _enumUnderlying.Clear();
        ResolvedTypes.Clear();
    }

    /// <summary>Bare name, for matching against <see cref="SkipAssemblies"/>.</summary>
    private static string StripDll(string name) =>
        name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;

    /// <summary>
    /// Name as the runtime wants it. Both backends resolve assemblies through
    /// <c>OpenAssembly</c>, which expects the file name including the ".dll" suffix
    /// (see how hand-written stubs pass "Assembly-CSharp.dll").
    /// </summary>
    private static string WithDll(string name) =>
        string.IsNullOrEmpty(name) || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? name : name + ".dll";

    // ── Collection ──

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
        var asmName = WithDll(asm.Name ?? "");
        var classCount = Il2CppFunctions.il2cpp_image_get_class_count(image);

        var nameToPtr = new Dictionary<string, nint>(StringComparer.Ordinal);
        var classes = new List<nint>((int)classCount);
        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;
            classes.Add(klass);
            var cname = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            nameToPtr[cname] = klass;
        }

        // Nesting relationships must be complete before any name chain is computed.
        foreach (var klass in classes)
        {
            var iter = IntPtr.Zero;
            while (true)
            {
                var nested = Il2CppFunctions.il2cpp_class_get_nested_types(klass, ref iter);
                if (nested == 0) break;
                if (nested != klass && !_nestingMap.ContainsKey(nested))
                    LinkNesting(nested, klass);
            }

            var className = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            if (className.Contains('+') && !_nestingMap.ContainsKey(klass))
            {
                var parts = className.Split('+');
                var parentName = string.Join("+", parts, 0, parts.Length - 1);
                if (nameToPtr.TryGetValue(parentName, out var parentPtr) && parentPtr != klass)
                    LinkNesting(klass, parentPtr);
            }
        }

        foreach (var klass in classes)
            RegisterCandidate(klass, asmName);
    }

    private static void CollectMonoTypes(MonoAssembly asm)
    {
        var image = MonoFunctions.MonoAssemblyGetImage(asm.Ptr);
        if (image == 0) return;
        var asmName = WithDll(asm.Name ?? "");
        var table = Methods.mono_image_get_table_info((_MonoImage*)image,
            (int)MonoMetaTableEnum.MONO_TABLE_TYPEDEF);
        if (table == null) return;
        var rows = Methods.mono_table_info_get_rows(table);

        var classes = new List<nint>(rows);
        for (int i = 1; i <= rows; i++)
        {
            var klass = Methods.mono_class_get((_MonoImage*)image, (uint)i);
            if (klass == null) continue;
            classes.Add((nint)klass);
        }

        foreach (var klass in classes)
        {
            var parent = MonoFunctions.MonoClassGetNestingType(klass);
            if (parent != 0 && parent != klass && !_nestingMap.ContainsKey(klass))
                LinkNesting(klass, parent);
        }

        foreach (var klass in classes)
            RegisterCandidate(klass, asmName);
    }

    private static void LinkNesting(nint nested, nint outer)
    {
        _nestingMap[nested] = outer;
        if (!_childrenMap.TryGetValue(outer, out var list))
            _childrenMap[outer] = list = new List<nint>();
        list.Add(nested);
    }

    private static void RegisterCandidate(nint klass, string asmName)
    {
        var (ns, name) = GetClassNamespaceAndName(klass);
        if (string.IsNullOrEmpty(name)) return;

        _asmOfType.TryAdd(klass, asmName);
        _pendingTypes.Add(klass);

        var safeNs = SanitizeNamespace(ns);
        var safeName = SanitizeIdentifier(name);
        ResolvedTypes.Add(string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}");
    }

    // ── Class name helpers ──

    private static string GetRawClassName(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klassPtr)) ?? "";
        if (RuntimeManager.IsMono)
            return MonoFunctions.MonoClassGetName(klassPtr) ?? "";
        return "";
    }

    private static string GetRawNamespace(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
            return Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_namespace(klassPtr)) ?? "";
        if (RuntimeManager.IsMono)
            return MonoFunctions.MonoClassGetNamespace(klassPtr) ?? "";
        return "";
    }

    private static nint GetNestingParent(nint klassPtr)
    {
        if (_nestingMap.TryGetValue(klassPtr, out var outer)) return outer;
        if (RuntimeManager.IsMono) return MonoFunctions.MonoClassGetNestingType(klassPtr);
        return 0;
    }

    /// <summary>Outer-to-inner name segments, e.g. ["Outer", "Inner"].</summary>
    private static string[] GetNameChain(nint klassPtr, int depth = 0)
    {
        if (_nameChainCache.TryGetValue(klassPtr, out var cached)) return cached;

        var raw = GetRawClassName(klassPtr);
        string[] chain;

        if (raw.Contains('+'))
        {
            // Il2Cpp already encodes the full nesting path in the name.
            chain = raw.Split('+');
        }
        else
        {
            var outer = depth < 16 ? GetNestingParent(klassPtr) : 0;
            chain = outer != 0 && outer != klassPtr
                ? [.. GetNameChain(outer, depth + 1), raw]
                : [raw];
        }

        if (depth == 0) _nameChainCache[klassPtr] = chain;
        return chain;
    }

    private static string GetSimpleClassName(nint klassPtr) => GetNameChain(klassPtr)[^1];

    /// <summary>Name used for runtime lookup — nesting joined with '+' (both backends map '+' to '/').</summary>
    private static string GetRuntimeLookupName(nint klassPtr) => string.Join("+", GetNameChain(klassPtr));

    /// <summary>Namespace of the outermost declaring type — nested classes report an empty namespace.</summary>
    private static string GetRootNamespace(nint klassPtr)
    {
        var root = klassPtr;
        for (int i = 0; i < 16; i++)
        {
            var outer = GetNestingParent(root);
            if (outer == 0 || outer == root) break;
            root = outer;
        }
        return GetRawNamespace(root);
    }

    /// <summary>Display name: namespace plus the dot-joined nesting chain.</summary>
    private static (string ns, string name) GetClassNamespaceAndName(nint klassPtr)
    {
        if (!RuntimeManager.IsIl2Cpp && !RuntimeManager.IsMono) return ("", "");
        return (GetRootNamespace(klassPtr), string.Join(".", GetNameChain(klassPtr)));
    }

    // ── Type definition ──

    private static void EnsureTypeDefined(nint klassPtr)
    {
        if (_typeBuilders.ContainsKey(klassPtr)) return;

        // A nested type can only be defined through its declaring type.
        var root = klassPtr;
        for (int i = 0; i < 16; i++)
        {
            var outer = GetNestingParent(root);
            if (outer == 0 || outer == root) break;
            root = outer;
        }

        if (_typeBuilders.ContainsKey(root) || _defining.Contains(root)) return;
        DefineTypeAndNested(root, null);
    }

    private static TypeBuilder? DefineTypeAndNested(nint klassPtr, TypeBuilder? parentTb)
    {
        if (_typeBuilders.TryGetValue(klassPtr, out var existing)) return existing;
        if (!_defining.Add(klassPtr)) return null;

        try
        {
            var (ns, displayName) = GetClassNamespaceAndName(klassPtr);
            var simpleName = GetSimpleClassName(klassPtr);
            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(simpleName)) return null;
            if (simpleName.StartsWith('<') || simpleName.StartsWith("__") ||
                simpleName is "<Module>" or "<PrivateImplementationDetails>" or "Array" or "ValueType" or "Enum")
                return null;
            if (IsGeneric(klassPtr)) return null;

            // Enums become real managed enums when their members can be read; otherwise they
            // fall through and are emitted as an ordinary wrapper class.
            if (IsEnumClass(klassPtr))
            {
                var enumTb = TryDefineEnum(klassPtr, parentTb, ns, simpleName);
                if (enumTb != null)
                {
                    _typeBuilders[klassPtr] = enumTb;
                    RegisterTypeNames(enumTb, ns, displayName, simpleName);
                    enumTb.SetCustomAttribute(new CustomAttributeBuilder(_unmanagedTypeAttrCtor,
                        [_asmOfType.GetValueOrDefault(klassPtr, ""), ns, GetRuntimeLookupName(klassPtr)]));
                    return enumTb;
                }
            }

            var isInterface = IsInterface(klassPtr);

            // Base type — define the base first if it is also part of this run.
            Type? baseType = null;
            nint basePtr = 0;
            if (!isInterface)
            {
                basePtr = GetParentClass(klassPtr);
                if (basePtr != 0 && basePtr != klassPtr)
                {
                    if (!_typeBuilders.ContainsKey(basePtr) && _pendingTypes.Contains(basePtr))
                        EnsureTypeDefined(basePtr);
                    // Only usable as a base if it is a class we emitted a ctor(nint) for.
                    if (_typeBuilders.TryGetValue(basePtr, out var baseTb) && baseTb != parentTb &&
                        _typeCtors.ContainsKey(basePtr))
                        baseType = baseTb;
                }
                if (baseType == null) basePtr = 0;
                baseType ??= _unmanagedObjectType;
            }

            var attr = TypeAttributes.Public | TypeAttributes.Class;
            if (isInterface) attr = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;

            TypeBuilder tb;
            if (parentTb != null)
            {
                var nestedAttr = (attr & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
                var name = UniqueTypeName($"{parentTb.FullName}+{simpleName}", simpleName);
                tb = parentTb.DefineNestedType(name, nestedAttr, baseType);
            }
            else
            {
                var safeNs = SanitizeNamespace(ns);
                var safeName = SanitizeIdentifier(simpleName);
                var full = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
                var name = UniqueTypeName(full, full);
                tb = _module.DefineType(name, attr, baseType);
            }

            _typeBuilders[klassPtr] = tb;
            if (basePtr != 0) _emittedBase[klassPtr] = basePtr;

            RegisterTypeNames(tb, ns, displayName, simpleName);

            // UnmanagedType attribute carries the names needed for runtime lookup.
            tb.SetCustomAttribute(new CustomAttributeBuilder(_unmanagedTypeAttrCtor,
                [_asmOfType.GetValueOrDefault(klassPtr, ""), ns, GetRuntimeLookupName(klassPtr)]));

            if (!isInterface)
            {
                var ctor = tb.DefineConstructor(MethodAttributes.Public,
                    CallingConventions.Standard, [typeof(nint)]);
                var cIl = ctor.GetILGenerator();
                cIl.Emit(OpCodes.Ldarg_0);
                cIl.Emit(OpCodes.Ldarg_1);
                cIl.Emit(OpCodes.Call, baseType == _unmanagedObjectType
                    ? _unmanagedObjectCtor
                    : _typeCtors[basePtr]);
                cIl.Emit(OpCodes.Ret);
                _typeCtors[klassPtr] = ctor;
                _ctorByBuilder[tb] = ctor;
            }

            if (_childrenMap.TryGetValue(klassPtr, out var children))
            {
                foreach (var child in children)
                    DefineTypeAndNested(child, tb);
            }

            return tb;
        }
        finally
        {
            _defining.Remove(klassPtr);
        }
    }

    private static void RegisterTypeNames(TypeBuilder tb, string ns, string displayName, string simpleName)
    {
        _nameToTypeBuilder.TryAdd(tb.FullName!, tb);
        _nameToTypeBuilder.TryAdd(SanitizeNamespace(ns) is { Length: > 0 } sns
            ? $"{sns}.{SanitizeIdentifier(displayName)}"
            : SanitizeIdentifier(displayName), tb);
        _nameToTypeBuilder.TryAdd(SanitizeIdentifier(simpleName), tb);
    }

    private static string UniqueTypeName(string uniquenessKey, string desiredName)
    {
        if (_usedTypeNames.Add(uniquenessKey)) return desiredName;
        for (int i = 2; ; i++)
        {
            var candidate = $"{desiredName}_{i}";
            if (_usedTypeNames.Add($"{uniquenessKey}_{i}")) return candidate;
        }
    }

    private static nint GetParentClass(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_class_get_parent(klassPtr);
        if (RuntimeManager.IsMono) return MonoFunctions.MonoClassGetParent(klassPtr);
        return 0;
    }

    private static void AddInterfaces(TypeBuilder tb, nint klassPtr)
    {
        foreach (var iface in EnumerateInterfaces(klassPtr))
        {
            if (iface == 0 || iface == klassPtr) continue;
            if (_typeBuilders.TryGetValue(iface, out var ifaceTb) && ifaceTb != tb && IsInterface(iface))
                tb.AddInterfaceImplementation(ifaceTb);
        }
    }

    private static IEnumerable<nint> EnumerateInterfaces(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp)
        {
            nint iter = 0;
            while (true)
            {
                var iface = Il2CppFunctions.il2cpp_class_get_interfaces(klassPtr, ref iter);
                if (iface == 0) break;
                yield return iface;
            }
        }
        // Mono interface enumeration is not available through the current bindings.
    }

    /// <summary>All interface methods a concrete type must expose, keyed by "name|paramCount".</summary>
    private static HashSet<string> CollectInterfaceMethodKeys(nint klassPtr)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<nint>();
        var queue = new Queue<nint>();

        foreach (var iface in EnumerateInterfaces(klassPtr))
            queue.Enqueue(iface);

        while (queue.Count > 0)
        {
            var iface = queue.Dequeue();
            if (iface == 0 || !seen.Add(iface)) continue;
            if (!_typeBuilders.ContainsKey(iface)) continue;

            foreach (var m in EnumerateMethods(iface))
            {
                if (IsInterfaceDeclarable(m))
                    keys.Add($"{m.name}|{m.paramTypes.Length}");
            }
            foreach (var parent in EnumerateInterfaces(iface))
                queue.Enqueue(parent);
        }

        return keys;
    }

    // ── Creation order ──

    private static List<TypeBuilder> BuildCreationOrder()
    {
        var order = new List<TypeBuilder>(_typeBuilders.Count);
        var visited = new HashSet<nint>();

        void Visit(nint klass)
        {
            if (!_typeBuilders.ContainsKey(klass) || !visited.Add(klass)) return;

            if (_emittedBase.TryGetValue(klass, out var basePtr))
                Visit(basePtr);
            if (_childrenMap.TryGetValue(klass, out var children))
            {
                foreach (var child in children)
                    Visit(child);
            }

            order.Add(_typeBuilders[klass]);
        }

        foreach (var klass in _typeBuilders.Keys.ToList())
            Visit(klass);

        return order;
    }

    // ── Type resolution ──

    private static Type ResolveManagedType(nint typePtr, string resolvedTypeName, bool il2cpp)
    {
        switch (resolvedTypeName)
        {
            case "void": return typeof(void);
            case "bool": return typeof(bool);
            case "int": return typeof(int);
            case "uint": return typeof(uint);
            case "float": return typeof(float);
            case "double": return typeof(double);
            case "long": return typeof(long);
            case "ulong": return typeof(ulong);
            case "short": return typeof(short);
            case "ushort": return typeof(ushort);
            case "byte": return typeof(byte);
            case "sbyte": return typeof(sbyte);
            case "char": return typeof(char);
            case "nint": return typeof(nint);
            case "nuint": return typeof(nuint);
            case "decimal": return typeof(nint); // no unmanaged mapping — pass the boxed pointer
            case "string":
            case "RuntimeString": return typeof(RuntimeString);
            case "RuntimeArray": return typeof(RuntimeArray);
        }

        if (resolvedTypeName.StartsWith(ArrayPrefix, StringComparison.Ordinal) &&
            resolvedTypeName.EndsWith(">", StringComparison.Ordinal))
        {
            var innerName = resolvedTypeName[ArrayPrefix.Length..^1];
            var innerType = ResolveTypeByName(innerName);
            // RuntimeArray<T> is constrained to `unmanaged`; reference elements fall back
            // to the untyped RuntimeArray, which indexes by nint.
            return IsUnmanagedElement(innerType)
                ? typeof(RuntimeArray<>).MakeGenericType(innerType!)
                : typeof(RuntimeArray);
        }

        if (typePtr != 0)
        {
            var cls = il2cpp
                ? Il2CppFunctions.il2cpp_class_from_type(typePtr)
                : MonoFunctions.MonoTypeGetClass(typePtr);
            if (cls != 0 && _typeBuilders.TryGetValue(cls, out var tb))
                return tb;
        }

        var byName = ResolveTypeByName(resolvedTypeName);
        return byName ?? typeof(nint);
    }

    /// <summary>True when <paramref name="t"/> is valid as a RuntimeArray&lt;T&gt; type argument.</summary>
    private static bool IsUnmanagedElement(Type? t)
    {
        if (t == null || t == typeof(void) || t is TypeBuilder) return false;
        if (t.IsPrimitive) return true; // includes nint/nuint
        if (t == typeof(RuntimeString) || t == typeof(RuntimeArray)) return true;
        if (t.IsGenericType && !t.IsGenericTypeDefinition &&
            t.GetGenericTypeDefinition() == typeof(RuntimeArray<>))
            return true;
        return false;
    }

    /// <summary>
    /// True for returns that come back boxed from runtime_invoke and must be unboxed.
    /// nint/nuint are excluded: they stand for object pointers or types we could not
    /// resolve, where the raw pointer is what the caller wants.
    /// </summary>
    private static bool NeedsUnboxedReturn(Type t)
    {
        if (IsEmittedEnum(t)) return true; // boxed enum — unbox as its storage type
        return t.IsPrimitive && t != typeof(nint) && t != typeof(nuint);
    }

    private static bool IsPtrHolderStruct(Type t) =>
        t == typeof(RuntimeString) || t == typeof(RuntimeArray) ||
        (t.IsGenericType && !t.IsGenericTypeDefinition &&
         t.GetGenericTypeDefinition() == typeof(RuntimeArray<>));

    private static Type? ResolveTypeByName(string name)
    {
        switch (name)
        {
            case "void": return typeof(void);
            case "bool": return typeof(bool);
            case "int": return typeof(int);
            case "uint": return typeof(uint);
            case "float": return typeof(float);
            case "double": return typeof(double);
            case "long": return typeof(long);
            case "ulong": return typeof(ulong);
            case "short": return typeof(short);
            case "ushort": return typeof(ushort);
            case "byte": return typeof(byte);
            case "sbyte": return typeof(sbyte);
            case "char": return typeof(char);
            case "IntPtr":
            case "nint": return typeof(nint);
            case "UIntPtr":
            case "nuint": return typeof(nuint);
            case "string":
            case "String":
            case "RuntimeString": return typeof(RuntimeString);
            case "RuntimeArray": return typeof(RuntimeArray);
            case "object":
            case "Object": return typeof(nint);
        }

        if (name.StartsWith(ArrayPrefix, StringComparison.Ordinal) &&
            name.EndsWith(">", StringComparison.Ordinal))
        {
            var inner = ResolveTypeByName(name[ArrayPrefix.Length..^1]);
            return IsUnmanagedElement(inner)
                ? typeof(RuntimeArray<>).MakeGenericType(inner!)
                : typeof(RuntimeArray);
        }

        if (_nameToTypeBuilder.TryGetValue(name, out var tb))
            return tb;

        var dot = name.LastIndexOf('.');
        if (dot >= 0 && _nameToTypeBuilder.TryGetValue(name[(dot + 1)..], out tb))
            return tb;

        return null;
    }

    // ── Body emission ──

    private sealed class TypeCtx
    {
        public readonly HashSet<string> MethodSigs = new(StringComparer.Ordinal);
        public readonly HashSet<string> MemberNames = new(StringComparer.Ordinal);
        public HashSet<string> InterfaceKeys = new(StringComparer.Ordinal);
    }

    /// <summary>Lazily-declared scratch locals for bit-reinterpreting float/double against nint.</summary>
    private sealed class Scratch(ILGenerator il)
    {
        private LocalBuilder? _f, _d, _n;
        public LocalBuilder Float => _f ??= il.DeclareLocal(typeof(float));
        public LocalBuilder Double => _d ??= il.DeclareLocal(typeof(double));
        public LocalBuilder NInt => _n ??= il.DeclareLocal(typeof(nint));
    }

    private static void EmitTypeBody(TypeBuilder tb, nint klassPtr)
    {
        if (IsEmittedEnum(tb)) return; // literal fields are the whole body

        var ctx = new TypeCtx();
        var isInterface = IsInterface(klassPtr);
        var ns = GetRootNamespace(klassPtr);
        var lookupName = GetRuntimeLookupName(klassPtr);
        var asmName = _asmOfType.GetValueOrDefault(klassPtr, "");

        if (isInterface)
        {
            EmitInterfaceMethods(tb, klassPtr, ctx);
            return;
        }

        ctx.InterfaceKeys = CollectInterfaceMethodKeys(klassPtr);
        EmitMethodsAndProperties(tb, klassPtr, asmName, ns, lookupName, ctx);
        EmitFields(tb, klassPtr, asmName, ns, lookupName, ctx);
        EmitMissingInterfaceMethods(tb, klassPtr, asmName, ns, lookupName, ctx);
    }

    // ── Runtime type-name hints ──
    //
    // Generic collections (HashSet<T>, List<T>, Dictionary<K,V>, …) live in assemblies we skip,
    // so they resolve to nint. The pointer is still usable — wrap it in UnmanagedEnumerable —
    // but the declared type tells you nothing. Record the runtime's own name alongside it.

    private static bool NeedsTypeNameHint(Type resolved, string raw)
    {
        if (raw.Length == 0) return false;

        // Fully degraded: nothing about the declared type says what this is.
        if (resolved == typeof(nint))
            return raw is not ("System.IntPtr" or "System.UIntPtr" or "nint" or "nuint");

        // Partially degraded: known to be an array, but the element type was not resolved.
        if (resolved == typeof(RuntimeArray) || resolved == typeof(RuntimeArray<nint>))
            return true;

        return false;
    }

    private static CustomAttributeBuilder TypeNameHint(string raw) =>
        new(_typeNameAttrCtor, [raw]);

    private static void AnnotateReturn(MethodBuilder mb, Type resolved, string raw)
    {
        if (NeedsTypeNameHint(resolved, raw))
            mb.SetCustomAttribute(TypeNameHint(raw));
    }

    private static void AnnotateParams(MethodBuilder mb, Type[] resolved, string[] raw)
    {
        for (int i = 0; i < resolved.Length && i < raw.Length; i++)
        {
            if (!NeedsTypeNameHint(resolved[i], raw[i])) continue;
            mb.DefineParameter(i + 1, ParameterAttributes.None, null)
                .SetCustomAttribute(TypeNameHint(raw[i]));
        }
    }

    private static void AnnotateMethod(MethodBuilder mb, Type returnType, Type[] paramTypes, MethodMeta m)
    {
        AnnotateReturn(mb, returnType, m.rawReturnType);
        AnnotateParams(mb, paramTypes, m.rawParamTypes);
    }

    private static string SigKey(string name, Type[] paramTypes) =>
        $"{name}({string.Join(",", paramTypes.Select(t => t.FullName ?? t.Name))})";

    /// <summary>
    /// Reserves a method name. Overloads that differ only in types erased to nint would
    /// otherwise collide, so a numeric suffix is appended; the runtime lookup string
    /// emitted into the body still uses the original name.
    /// </summary>
    private static string? ReserveMethod(TypeCtx ctx, string desiredName, Type[] paramTypes)
    {
        if (ctx.MethodSigs.Add(SigKey(desiredName, paramTypes))) return desiredName;
        for (int i = 2; i < 64; i++)
        {
            var candidate = $"{desiredName}_{i}";
            if (ctx.MethodSigs.Add(SigKey(candidate, paramTypes))) return candidate;
        }
        return null;
    }

    private static string? ReserveMember(TypeCtx ctx, string desiredName)
    {
        if (ctx.MemberNames.Add(desiredName)) return desiredName;
        for (int i = 2; i < 64; i++)
        {
            var candidate = $"{desiredName}_{i}";
            if (ctx.MemberNames.Add(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Which interface members actually get declared on the interface TypeBuilder. The same
    /// predicate gates the implementing side, so the two never disagree about what must exist.
    /// </summary>
    private static bool IsInterfaceDeclarable(MethodMeta m) =>
        !m.isStatic &&
        !m.name.StartsWith("get_", StringComparison.Ordinal) &&
        !m.name.StartsWith("set_", StringComparison.Ordinal) &&
        !m.name.StartsWith("add_", StringComparison.Ordinal) &&
        !m.name.StartsWith("remove_", StringComparison.Ordinal) &&
        !SpecialNames.Contains(m.name);

    private static void EmitInterfaceMethods(TypeBuilder tb, nint klassPtr, TypeCtx ctx)
    {
        var isIl2cpp = RuntimeManager.IsIl2Cpp;
        foreach (var m in EnumerateMethods(klassPtr))
        {
            if (!IsInterfaceDeclarable(m)) continue;

            var paramTypes = ResolveParams(m, isIl2cpp);
            var returnType = ResolveReturn(m, isIl2cpp);
            var name = ReserveMethod(ctx, SanitizeMemberName(m.name), paramTypes);
            if (name == null) continue;

            var mb = tb.DefineMethod(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
                MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                returnType, paramTypes);
            AnnotateMethod(mb, returnType, paramTypes, m);
        }
    }

    private static Type[] ResolveParams(MethodMeta m, bool il2cpp)
    {
        var types = new Type[m.paramTypes.Length];
        for (int i = 0; i < types.Length; i++)
            types[i] = ResolveManagedType(m.paramTypePtrs[i], m.paramTypes[i], il2cpp);
        return types;
    }

    private static Type ResolveReturn(MethodMeta m, bool il2cpp) =>
        m.returnType == "void" ? typeof(void) : ResolveManagedType(m.retTypePtr, m.returnType, il2cpp);

    private static void EmitMethodsAndProperties(TypeBuilder tb, nint klassPtr,
        string asmName, string ns, string lookupName, TypeCtx ctx)
    {
        var methods = EnumerateMethods(klassPtr).ToList();
        var isIl2cpp = RuntimeManager.IsIl2Cpp;

        var regularMethods = new List<MethodMeta>();
        var propertyGetters = new Dictionary<string, MethodMeta>(StringComparer.Ordinal);
        var propertySetters = new Dictionary<string, MethodMeta>(StringComparer.Ordinal);
        var eventAdders = new Dictionary<string, MethodMeta>(StringComparer.Ordinal);
        var eventRemovers = new Dictionary<string, MethodMeta>(StringComparer.Ordinal);

        foreach (var m in methods)
        {
            if (SpecialNames.Contains(m.name)) { regularMethods.Add(m); continue; }

            if (m.name.StartsWith("get_") && !propertyGetters.ContainsKey(m.name[4..]))
                propertyGetters[m.name[4..]] = m;
            else if (m.name.StartsWith("set_") && !propertySetters.ContainsKey(m.name[4..]))
                propertySetters[m.name[4..]] = m;
            else if (m.name.StartsWith("add_") && !eventAdders.ContainsKey(m.name[4..]))
                eventAdders[m.name[4..]] = m;
            else if (m.name.StartsWith("remove_") && !eventRemovers.ContainsKey(m.name[7..]))
                eventRemovers[m.name[7..]] = m;
            else
                regularMethods.Add(m);
        }

        // ── Regular methods ──
        foreach (var m in regularMethods)
        {
            var paramTypes = ResolveParams(m, isIl2cpp);
            var returnType = ResolveReturn(m, isIl2cpp);
            var name = ReserveMethod(ctx, SanitizeMemberName(m.name), paramTypes);
            if (name == null) continue;

            var access = MethodAttributes.Public | MethodAttributes.HideBySig;
            if (m.isStatic) access |= MethodAttributes.Static;
            else if (ctx.InterfaceKeys.Contains($"{m.name}|{m.paramTypes.Length}"))
                access |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;

            var mb = tb.DefineMethod(name, access, returnType, paramTypes);
            AnnotateMethod(mb, returnType, paramTypes, m);
            EmitMethodBody(mb.GetILGenerator(), m, asmName, ns, lookupName, paramTypes, returnType);
        }

        // ── Properties ──
        var allPropNames = new HashSet<string>(propertyGetters.Keys, StringComparer.Ordinal);
        allPropNames.UnionWith(propertySetters.Keys);
        foreach (var propName in allPropNames)
        {
            var hasGet = propertyGetters.TryGetValue(propName, out var getM);
            var hasSet = propertySetters.TryGetValue(propName, out var setM);
            var isStatic = hasGet ? getM.isStatic : setM.isStatic;

            var getParamTypes = hasGet ? ResolveParams(getM, isIl2cpp) : [];
            var setParamTypes = hasSet ? ResolveParams(setM, isIl2cpp) : [];

            var propType = hasGet
                ? ResolveReturn(getM, isIl2cpp)
                : setParamTypes.Length > 0 ? setParamTypes[^1] : typeof(nint);
            if (propType == typeof(void)) propType = typeof(nint);

            MethodBuilder? getMb = null, setMb = null;

            if (hasGet)
            {
                var gname = ReserveMethod(ctx, $"get_{SanitizeMemberName(propName)}", getParamTypes);
                if (gname != null)
                {
                    getMb = tb.DefineMethod(gname, PropAccessorAttrs(isStatic, getM, ctx),
                        propType, getParamTypes);
                    AnnotateMethod(getMb, propType, getParamTypes, getM);
                    EmitMethodBody(getMb.GetILGenerator(), getM, asmName, ns, lookupName,
                        getParamTypes, propType);
                }
            }

            if (hasSet)
            {
                var sname = ReserveMethod(ctx, $"set_{SanitizeMemberName(propName)}", setParamTypes);
                if (sname != null)
                {
                    setMb = tb.DefineMethod(sname, PropAccessorAttrs(isStatic, setM, ctx),
                        typeof(void), setParamTypes);
                    AnnotateParams(setMb, setParamTypes, setM.rawParamTypes);
                    EmitMethodBody(setMb.GetILGenerator(), setM, asmName, ns, lookupName,
                        setParamTypes, typeof(void));
                }
            }

            if (getMb == null && setMb == null) continue;

            var indexTypes = hasGet
                ? getParamTypes
                : setParamTypes.Length > 0 ? setParamTypes[..^1] : [];

            var declaredName = ReserveMember(ctx, SanitizeMemberName(propName));
            if (declaredName == null) continue;

            var prop = tb.DefineProperty(declaredName, System.Reflection.PropertyAttributes.None,
                propType, indexTypes.Length > 0 ? indexTypes : null);

            var propRaw = hasGet ? getM.rawReturnType
                : setM.rawParamTypes.Length > 0 ? setM.rawParamTypes[^1] : "";
            if (NeedsTypeNameHint(propType, propRaw))
                prop.SetCustomAttribute(TypeNameHint(propRaw));

            if (getMb != null) prop.SetGetMethod(getMb);
            if (setMb != null) prop.SetSetMethod(setMb);
        }

        // ── Events ──
        var allEventNames = new HashSet<string>(eventAdders.Keys, StringComparer.Ordinal);
        allEventNames.UnionWith(eventRemovers.Keys);
        foreach (var evtName in allEventNames)
        {
            var hasAdd = eventAdders.TryGetValue(evtName, out var addM);
            var hasRemove = eventRemovers.TryGetValue(evtName, out var removeM);
            var isStatic = hasAdd ? addM.isStatic : removeM.isStatic;

            var addParamTypes = hasAdd ? ResolveParams(addM, isIl2cpp) : [];
            var removeParamTypes = hasRemove ? ResolveParams(removeM, isIl2cpp) : [];

            var eventType = addParamTypes.Length > 0 ? addParamTypes[0]
                : removeParamTypes.Length > 0 ? removeParamTypes[0]
                : typeof(nint);

            MethodBuilder? addMb = null, removeMb = null;

            if (hasAdd)
            {
                var aname = ReserveMethod(ctx, $"add_{SanitizeMemberName(evtName)}", addParamTypes);
                if (aname != null)
                {
                    addMb = tb.DefineMethod(aname, PropAccessorAttrs(isStatic, addM, ctx),
                        typeof(void), addParamTypes);
                    AnnotateParams(addMb, addParamTypes, addM.rawParamTypes);
                    EmitMethodBody(addMb.GetILGenerator(), addM, asmName, ns, lookupName,
                        addParamTypes, typeof(void));
                }
            }

            if (hasRemove)
            {
                var rname = ReserveMethod(ctx, $"remove_{SanitizeMemberName(evtName)}", removeParamTypes);
                if (rname != null)
                {
                    removeMb = tb.DefineMethod(rname, PropAccessorAttrs(isStatic, removeM, ctx),
                        typeof(void), removeParamTypes);
                    AnnotateParams(removeMb, removeParamTypes, removeM.rawParamTypes);
                    EmitMethodBody(removeMb.GetILGenerator(), removeM, asmName, ns, lookupName,
                        removeParamTypes, typeof(void));
                }
            }

            if (addMb == null && removeMb == null) continue;

            var declaredName = ReserveMember(ctx, SanitizeMemberName(evtName) + "E");
            if (declaredName == null) continue;

            var eventBuilder = tb.DefineEvent(declaredName,
                System.Reflection.EventAttributes.None, eventType);
            if (addMb != null) eventBuilder.SetAddOnMethod(addMb);
            if (removeMb != null) eventBuilder.SetRemoveOnMethod(removeMb);
        }
    }

    private static MethodAttributes PropAccessorAttrs(bool isStatic, MethodMeta m, TypeCtx ctx)
    {
        var attrs = MethodAttributes.Public | MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName;
        if (isStatic) return attrs | MethodAttributes.Static;
        if (ctx.InterfaceKeys.Contains($"{m.name}|{m.paramTypes.Length}"))
            attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
        return attrs;
    }

    /// <summary>
    /// Concrete types that declare an interface must expose every interface method, or the
    /// stub assembly fails to load. Anything the type itself did not define is forwarded here.
    /// </summary>
    private static void EmitMissingInterfaceMethods(TypeBuilder tb, nint klassPtr,
        string asmName, string ns, string lookupName, TypeCtx ctx)
    {
        if (ctx.InterfaceKeys.Count == 0) return;
        var isIl2cpp = RuntimeManager.IsIl2Cpp;
        var seen = new HashSet<nint>();
        var queue = new Queue<nint>();

        foreach (var iface in EnumerateInterfaces(klassPtr))
            queue.Enqueue(iface);

        while (queue.Count > 0)
        {
            var iface = queue.Dequeue();
            if (iface == 0 || !seen.Add(iface)) continue;
            if (!_typeBuilders.ContainsKey(iface)) continue;

            foreach (var parent in EnumerateInterfaces(iface))
                queue.Enqueue(parent);

            foreach (var m in EnumerateMethods(iface))
            {
                if (!IsInterfaceDeclarable(m)) continue;
                var paramTypes = ResolveParams(m, isIl2cpp);
                var declared = SanitizeMemberName(m.name);
                if (ctx.MethodSigs.Contains(SigKey(declared, paramTypes))) continue;

                var name = ReserveMethod(ctx, declared, paramTypes);
                if (name == null) continue;

                var returnType = ResolveReturn(m, isIl2cpp);
                var mb = tb.DefineMethod(name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual |
                    MethodAttributes.NewSlot | MethodAttributes.Final,
                    returnType, paramTypes);
                AnnotateMethod(mb, returnType, paramTypes, m);
                EmitMethodBody(mb.GetILGenerator(), m, asmName, ns, lookupName, paramTypes, returnType);
            }
        }
    }

    private static void EmitMethodBody(ILGenerator il, MethodMeta m,
        string asmName, string ns, string lookupName, Type[] managedParamTypes, Type returnType)
    {
        var scratch = new Scratch(il);
        var paramCount = m.paramTypes.Length;
        var isVoid = returnType == typeof(void);

        // runtime_invoke boxes value-type returns, so those go through the unbox helper and
        // come back already typed. Reference results stay raw pointers and get wrapped below.
        // StackTypeOf keeps enums out of the generic argument: TypeBuilder cannot satisfy
        // `where T : unmanaged`, and an enum is its storage type on the stack anyway.
        var unboxAs = NeedsUnboxedReturn(returnType) ? StackTypeOf(returnType) : null;

        if (m.isStatic)
        {
            il.Emit(OpCodes.Ldstr, asmName);
            il.Emit(OpCodes.Ldstr, ns);
            il.Emit(OpCodes.Ldstr, lookupName);
            il.Emit(OpCodes.Ldstr, m.name);
            il.Emit(OpCodes.Ldc_I4, paramCount);
            EmitNIntArray(il, managedParamTypes, 0, scratch);
            il.Emit(OpCodes.Call, isVoid ? _staticVoid
                : unboxAs != null ? GenericField(_staticRetUnboxCache, _staticRetUnboxDef, unboxAs)
                : _staticRet);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _get_Ptr);
            il.Emit(OpCodes.Ldstr, m.name);
            il.Emit(OpCodes.Ldc_I4, paramCount);
            EmitNIntArray(il, managedParamTypes, 1, scratch);
            il.Emit(OpCodes.Call, isVoid ? _instanceVoid
                : unboxAs != null ? GenericField(_instanceRetUnboxCache, _instanceRetUnboxDef, unboxAs)
                : _instanceRet);
        }

        if (!isVoid && unboxAs == null)
            EmitConvertFromNInt(il, returnType, scratch);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitNIntArray(ILGenerator il, Type[] managedTypes, int argOffset, Scratch scratch)
    {
        if (managedTypes.Length == 0)
        {
            il.Emit(OpCodes.Ldnull);
            return;
        }

        il.Emit(OpCodes.Ldc_I4, managedTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(nint));

        for (int i = 0; i < managedTypes.Length; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            EmitLoadArgAsNInt(il, argOffset + i, managedTypes[i], scratch);
            il.Emit(OpCodes.Stelem_I);
        }
    }

    private static void EmitLdarg(ILGenerator il, int index)
    {
        switch (index)
        {
            case 0: il.Emit(OpCodes.Ldarg_0); break;
            case 1: il.Emit(OpCodes.Ldarg_1); break;
            case 2: il.Emit(OpCodes.Ldarg_2); break;
            case 3: il.Emit(OpCodes.Ldarg_3); break;
            default:
                if (index <= byte.MaxValue) il.Emit(OpCodes.Ldarg_S, (byte)index);
                else il.Emit(OpCodes.Ldarg, checked((short)index));
                break;
        }
    }

    private static void EmitLdarga(ILGenerator il, int index)
    {
        if (index <= byte.MaxValue) il.Emit(OpCodes.Ldarga_S, (byte)index);
        else il.Emit(OpCodes.Ldarga, checked((short)index));
    }

    /// <summary>Pushes argument <paramref name="index"/> onto the stack as a native int.</summary>
    private static void EmitLoadArgAsNInt(ILGenerator il, int index, Type type, Scratch scratch)
    {
        // An enum argument sits on the stack as its storage type.
        if (IsEmittedEnum(type)) type = StackTypeOf(type);

        if (type == typeof(float))
        {
            // Reinterpret the bits rather than converting the value, matching
            // UnmanagedStubGenerator's `*(nint*)&value`. Zero-extend so a negative
            // float does not smear sign bits across the upper half.
            EmitLdarg(il, index);
            il.Emit(OpCodes.Stloc, scratch.Float);
            il.Emit(OpCodes.Ldloca, scratch.Float);
            il.Emit(OpCodes.Ldind_U4);
            il.Emit(OpCodes.Conv_U);
            return;
        }

        if (type == typeof(double))
        {
            EmitLdarg(il, index);
            il.Emit(OpCodes.Stloc, scratch.Double);
            il.Emit(OpCodes.Ldloca, scratch.Double);
            il.Emit(OpCodes.Ldind_I8);
            il.Emit(OpCodes.Conv_I);
            return;
        }

        if (IsPtrHolderStruct(type))
        {
            // Instance property on a value type needs a managed pointer as `this`.
            EmitLdarga(il, index);
            il.Emit(OpCodes.Call, type.GetProperty("Ptr")!.GetGetMethod()!);
            return;
        }

        if (type == typeof(nint) || type == typeof(nuint))
        {
            EmitLdarg(il, index);
            return;
        }

        if (type.IsPrimitive)
        {
            // Unsigned sources zero-extend, signed ones sign-extend — same as the
            // `(nint)value` / `(nint)(long)value` casts the source generator emits.
            EmitLdarg(il, index);
            il.Emit(type == typeof(byte) || type == typeof(ushort) || type == typeof(char) ||
                    type == typeof(uint) || type == typeof(ulong) || type == typeof(bool)
                ? OpCodes.Conv_U
                : OpCodes.Conv_I);
            return;
        }

        if (!type.IsValueType)
        {
            // Reference wrapper: null maps to a zero pointer instead of throwing.
            var notNull = il.DefineLabel();
            var done = il.DefineLabel();
            EmitLdarg(il, index);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, notNull);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Br, done);
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Call, _get_Ptr);
            il.MarkLabel(done);
            return;
        }

        // Unknown value type — no meaningful pointer, pass zero.
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Conv_I);
    }

    /// <summary>Converts the native int on the stack into <paramref name="type"/>.</summary>
    private static void EmitConvertFromNInt(ILGenerator il, Type type, Scratch scratch)
    {
        if (type == typeof(nint) || type == typeof(nuint)) return;

        if (type == typeof(bool))
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Cgt_Un);
            return;
        }

        if (type == typeof(float))
        {
            il.Emit(OpCodes.Stloc, scratch.NInt);
            il.Emit(OpCodes.Ldloca, scratch.NInt);
            il.Emit(OpCodes.Ldind_R4);
            return;
        }

        if (type == typeof(double))
        {
            il.Emit(OpCodes.Stloc, scratch.NInt);
            il.Emit(OpCodes.Ldloca, scratch.NInt);
            il.Emit(OpCodes.Ldind_R8);
            return;
        }

        if (type == typeof(sbyte)) { il.Emit(OpCodes.Conv_I1); return; }
        if (type == typeof(byte)) { il.Emit(OpCodes.Conv_U1); return; }
        if (type == typeof(short)) { il.Emit(OpCodes.Conv_I2); return; }
        if (type == typeof(ushort) || type == typeof(char)) { il.Emit(OpCodes.Conv_U2); return; }
        if (type == typeof(int)) { il.Emit(OpCodes.Conv_I4); return; }
        if (type == typeof(uint)) { il.Emit(OpCodes.Conv_U4); return; }
        if (type == typeof(long)) { il.Emit(OpCodes.Conv_I8); return; }
        if (type == typeof(ulong)) { il.Emit(OpCodes.Conv_U8); return; }

        if (IsPtrHolderStruct(type))
        {
            il.Emit(OpCodes.Newobj, type.GetConstructor([typeof(nint)])!);
            return;
        }

        if (!type.IsValueType)
        {
            var ctor = FindPtrCtor(type);
            if (ctor != null)
            {
                var notNull = il.DefineLabel();
                var done = il.DefineLabel();
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue, notNull);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Br, done);
                il.MarkLabel(notNull);
                il.Emit(OpCodes.Newobj, ctor);
                il.MarkLabel(done);
            }
            else
            {
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldnull);
            }
            return;
        }

        // Unknown value type — yield default.
        var tmp = il.DeclareLocal(type);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldloca, tmp);
        il.Emit(OpCodes.Initobj, type);
        il.Emit(OpCodes.Ldloc, tmp);
    }

    private static ConstructorInfo? FindPtrCtor(Type type)
    {
        if (type is TypeBuilder tb)
            return _ctorByBuilder.GetValueOrDefault(tb);

        try { return type.GetConstructor([typeof(nint)]); }
        catch (NotSupportedException) { return null; }
    }

    // ── Field emission ──

    private static void EmitFields(TypeBuilder tb, nint klassPtr,
        string asmName, string ns, string lookupName, TypeCtx ctx)
    {
        var isIl2cpp = RuntimeManager.IsIl2Cpp;
        var methodNames = EnumerateMethods(klassPtr).Select(m => m.name).ToHashSet(StringComparer.Ordinal);

        foreach (var f in EnumerateFields(klassPtr))
        {
            if (f.name.StartsWith('<')) continue;
            if (methodNames.Contains($"get_{f.name}")) continue;

            var fieldType = ResolveManagedType(f.typePtr, f.typeName, isIl2cpp);
            if (fieldType == typeof(void)) fieldType = typeof(nint);

            // GetField<T>/SetField<T> are constrained to `unmanaged`; reference wrappers are
            // read as nint and wrapped afterwards so the accessor still has the declared type.
            // An enum reads as its storage type and needs no conversion — same stack type.
            var isEnumField = IsEmittedEnum(fieldType);
            var storageType = isEnumField ? StackTypeOf(fieldType)
                : IsUnmanagedElement(fieldType) || IsPtrHolderStruct(fieldType) ? fieldType
                : typeof(nint);
            var needsWrap = storageType != fieldType && !isEnumField;

            var safeName = SanitizeMemberName(f.name);
            var getterName = ReserveMethod(ctx, $"get_{safeName}", []);
            var setterName = ReserveMethod(ctx, $"set_{safeName}", [fieldType]);
            if (getterName == null || setterName == null) continue;

            var accessorAttrs = MethodAttributes.Public | MethodAttributes.HideBySig |
                                MethodAttributes.SpecialName |
                                (f.isStatic ? MethodAttributes.Static : 0);

            // Getter
            var getter = tb.DefineMethod(getterName, accessorAttrs, fieldType, Type.EmptyTypes);
            if (NeedsTypeNameHint(fieldType, f.rawTypeName))
                getter.SetCustomAttribute(TypeNameHint(f.rawTypeName));
            var il = getter.GetILGenerator();
            var scratch = new Scratch(il);

            if (f.isStatic)
            {
                il.Emit(OpCodes.Ldstr, asmName);
                il.Emit(OpCodes.Ldstr, ns);
                il.Emit(OpCodes.Ldstr, lookupName);
                il.Emit(OpCodes.Ldstr, f.name);
                il.Emit(OpCodes.Call, GenericField(_getStaticFieldCache, _getStaticFieldDef, storageType));
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, _get_Ptr);
                il.Emit(OpCodes.Ldstr, f.name);
                il.Emit(OpCodes.Call, GenericField(_getFieldCache, _getFieldDef, storageType));
            }
            if (needsWrap)
                EmitConvertFromNInt(il, fieldType, scratch);
            il.Emit(OpCodes.Ret);

            // Setter
            var setter = tb.DefineMethod(setterName, accessorAttrs, typeof(void), [fieldType]);
            il = setter.GetILGenerator();
            scratch = new Scratch(il);
            var valueArg = f.isStatic ? 0 : 1;

            if (f.isStatic)
            {
                il.Emit(OpCodes.Ldstr, asmName);
                il.Emit(OpCodes.Ldstr, ns);
                il.Emit(OpCodes.Ldstr, lookupName);
                il.Emit(OpCodes.Ldstr, f.name);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, _get_Ptr);
                il.Emit(OpCodes.Ldstr, f.name);
            }

            if (needsWrap)
                EmitLoadArgAsNInt(il, valueArg, fieldType, scratch);
            else
                EmitLdarg(il, valueArg);

            il.Emit(OpCodes.Call, f.isStatic
                ? GenericField(_setStaticFieldCache, _setStaticFieldDef, storageType)
                : GenericField(_setFieldCache, _setFieldDef, storageType));
            il.Emit(OpCodes.Ret);

            var propName = ReserveMember(ctx, safeName);
            if (propName == null) continue;

            var prop = tb.DefineProperty(propName, System.Reflection.PropertyAttributes.None,
                fieldType, null);
            if (NeedsTypeNameHint(fieldType, f.rawTypeName))
                prop.SetCustomAttribute(TypeNameHint(f.rawTypeName));
            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);
        }
    }

    private static MethodInfo GenericField(Dictionary<Type, MethodInfo> cache,
        MethodInfo definition, Type arg)
    {
        if (cache.TryGetValue(arg, out var cached)) return cached;
        var made = definition.MakeGenericMethod(arg);
        cache[arg] = made;
        return made;
    }

    // ── Metadata helpers ──

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
        if (RuntimeManager.IsMono)
            return false;
        return true;
    }

    /// <summary>
    /// <paramref name="returnType"/>/<paramref name="paramTypes"/> hold the resolved stub type
    /// names; the <c>raw*</c> counterparts keep the runtime's own names (generic arguments
    /// included) so members that degrade to nint can still be annotated with what they really are.
    /// </summary>
    private readonly record struct MethodMeta(
        string name, string returnType, string[] paramTypes, bool isStatic,
        nint retTypePtr, nint[] paramTypePtrs,
        string rawReturnType, string[] rawParamTypes);

    private static IEnumerable<MethodMeta> EnumerateMethods(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp) return EnumerateIl2CppMethods(klassPtr);
        if (RuntimeManager.IsMono) return EnumerateMonoMethods(klassPtr);
        return [];
    }

    private static IEnumerable<MethodMeta> EnumerateIl2CppMethods(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var m = Il2CppFunctions.il2cpp_class_get_methods(klassPtr, ref iter);
            if (m == 0) break;

            var name = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_method_get_name(m)) ?? "";
            if (name.Length == 0 || name.StartsWith('.') || name.StartsWith('<')) continue;

            uint pc = Il2CppFunctions.il2cpp_method_get_param_count(m);
            uint flagsDummy = 0;
            uint flags = Il2CppFunctions.il2cpp_method_get_flags(m, ref flagsDummy);
            bool isStatic = (flags & 0x10) != 0;

            var retType = "void";
            var rawRet = "";
            var retTypePtr = Il2CppFunctions.il2cpp_method_get_return_type(m);
            if (retTypePtr != 0)
            {
                rawRet = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(retTypePtr)) ?? "";
                retType = ResolveTypeName(retTypePtr, rawRet, true);
            }

            var paramTypes = new string[pc];
            var paramTypePtrs = new nint[pc];
            var rawParams = new string[pc];
            for (uint i = 0; i < pc; i++)
            {
                var p = Il2CppFunctions.il2cpp_method_get_param(m, i);
                paramTypePtrs[i] = p;
                rawParams[i] = p != 0
                    ? Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(p)) ?? ""
                    : "";
                paramTypes[i] = p != 0 ? ResolveTypeName(p, rawParams[i], true) : "nint";
            }

            yield return new MethodMeta(name, retType, paramTypes, isStatic, retTypePtr, paramTypePtrs,
                rawRet, rawParams);
        }
    }

    private static IEnumerable<MethodMeta> EnumerateMonoMethods(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var m = MonoFunctions.MonoClassGetMethods(klassPtr, ref iter);
            if (m == 0) break;

            var name = MonoFunctions.MonoMethodGetName(m);
            if (string.IsNullOrEmpty(name) || name.StartsWith('.') || name.StartsWith('<')) continue;

            var sig = MonoFunctions.MonoMethodSignature(m);
            if (sig == 0) continue;

            uint pc = MonoFunctions.MonoSignatureGetParamCount(sig);
            uint flags = MonoFunctions.MonoMethodGetFlags(m);
            bool isStatic = (flags & 0x10) != 0;

            var retType = "void";
            var rawRet = "";
            var retTypePtr = MonoFunctions.MonoSignatureGetReturnType(sig);
            if (retTypePtr != 0)
            {
                rawRet = MonoFunctions.MonoTypeGetName(retTypePtr) ?? "";
                retType = ResolveTypeName(retTypePtr, rawRet, false);
            }

            var paramTypes = new string[pc];
            var paramTypePtrs = new nint[pc];
            var rawParams = new string[pc];
            GetMonoMethodParamTypes(sig, pc, paramTypes, paramTypePtrs, rawParams);

            yield return new MethodMeta(name, retType, paramTypes, isStatic, retTypePtr, paramTypePtrs,
                rawRet, rawParams);
        }
    }

    private static void GetMonoMethodParamTypes(nint sig, uint count,
        string[] typesOut, nint[] ptrsOut, string[] rawOut)
    {
        void* iter = null;
        for (uint i = 0; i < count; i++)
        {
            var pt = MonoFunctions.MonoSignatureGetParams(sig, ref iter);
            ptrsOut[i] = pt;
            rawOut[i] = pt != 0 ? MonoFunctions.MonoTypeGetName(pt) ?? "" : "";
            typesOut[i] = pt != 0 ? ResolveTypeName(pt, rawOut[i], false) : "nint";
        }
    }

    private static string ResolveTypeName(nint typePtr, string rawTypeName, bool il2cpp)
    {
        if (typePtr == 0 || string.IsNullOrEmpty(rawTypeName)) return "nint";

        var isArray = rawTypeName.EndsWith("]", StringComparison.Ordinal);

        nint cls = il2cpp
            ? Il2CppFunctions.il2cpp_class_from_type(typePtr)
            : MonoFunctions.MonoTypeGetClass(typePtr);

        if (cls == 0)
            return isArray ? $"{ArrayPrefix}nint>" : MapType(rawTypeName);

        if (isArray)
        {
            nint elemCls = il2cpp
                ? Il2CppFunctions.il2cpp_class_get_element_class(cls)
                : MonoFunctions.MonoClassGetElementClass(cls);
            if (elemCls != 0 && elemCls != cls)
            {
                var elemTypeName = ResolveArrayElementType(elemCls);
                if (elemTypeName != null)
                    return $"{ArrayPrefix}{elemTypeName}>";
            }
            return $"{ArrayPrefix}nint>";
        }

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

        // System type (int, float, string, …) — look up via MapType
        var rawFull = string.IsNullOrEmpty(ens) ? ename : $"{ens}.{ename}";
        var mapped = MapType(rawFull);
        if (mapped != "nint") return mapped;

        mapped = MapType(elemFullName);
        return mapped != "nint" ? mapped : null;
    }

    private readonly record struct FieldMeta(string name, bool isStatic, string typeName, nint typePtr,
        string rawTypeName, nint fieldPtr, uint flags)
    {
        private const uint FieldLiteral = 0x40;
        public bool IsLiteral => (flags & FieldLiteral) != 0;
    }

    private static IEnumerable<FieldMeta> EnumerateFields(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp) return EnumerateIl2CppFields(klassPtr);
        if (RuntimeManager.IsMono) return EnumerateMonoFields(klassPtr);
        return [];
    }

    private static IEnumerable<FieldMeta> EnumerateIl2CppFields(nint klassPtr)
    {
        nint iter = 0;
        while (true)
        {
            var f = Il2CppFunctions.il2cpp_class_get_fields(klassPtr, ref iter);
            if (f == 0) break;

            var name = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_field_get_name(f)) ?? "";
            if (name.Length == 0) continue;

            var flags = Il2CppFunctions.il2cpp_field_get_flags(f);
            bool isStatic = (flags & 0x10) != 0;

            var typeName = "nint";
            var raw = "";
            var typePtr = Il2CppFunctions.il2cpp_field_get_type(f);
            if (typePtr != 0)
            {
                raw = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(typePtr)) ?? "";
                typeName = ResolveTypeName(typePtr, raw, true);
            }

            yield return new FieldMeta(name, isStatic, typeName, typePtr, raw, f, (uint)flags);
        }
    }

    private static IEnumerable<FieldMeta> EnumerateMonoFields(nint klassPtr)
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
            var raw = "";
            var typePtr = MonoFunctions.MonoFieldGetType(f);
            if (typePtr != 0)
            {
                raw = MonoFunctions.MonoTypeGetName(typePtr) ?? "";
                typeName = ResolveTypeName(typePtr, raw, false);
            }

            yield return new FieldMeta(name, isStatic, typeName, typePtr, raw, f, flags);
        }
    }

    // ── Enums ──

    private static bool IsEnumClass(nint klassPtr)
    {
        if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_class_is_enum(klassPtr);
        if (RuntimeManager.IsMono) return MonoFunctions.MonoClassIsEnum(klassPtr);
        return false;
    }

    private static bool IsIntegralPrimitive(Type t) =>
        t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
        t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
        t == typeof(char) || t == typeof(bool);

    /// <summary>
    /// The enum's storage type, taken from its <c>value__</c> instance field — available on both
    /// backends, unlike <c>enum_basetype</c>.
    /// </summary>
    private static Type? GetEnumUnderlying(nint klassPtr)
    {
        foreach (var f in EnumerateFields(klassPtr))
        {
            if (f.isStatic || f.name != "value__") continue;
            var t = ResolveManagedType(f.typePtr, f.typeName, RuntimeManager.IsIl2Cpp);
            return IsIntegralPrimitive(t) ? t : null;
        }
        return null;
    }

    /// <summary>
    /// Reads every literal member. Returns null if any value could not be read — a partially
    /// populated enum would silently report wrong values, so the type stays a plain class instead.
    /// </summary>
    private static List<(string name, object value)>? ReadEnumMembers(nint klassPtr, Type underlying)
    {
        var members = new List<(string, object)>();
        foreach (var f in EnumerateFields(klassPtr))
        {
            if (!f.isStatic || !f.IsLiteral || f.name.Length == 0) continue;
            var value = ReadLiteralValue(f.fieldPtr, underlying);
            if (value == null) return null;
            members.Add((f.name, value));
        }
        return members;
    }

    private static object? ReadLiteralValue(nint fieldPtr, Type underlying)
    {
        if (fieldPtr == 0) return null;

        nint boxed;
        if (RuntimeManager.IsIl2Cpp)
        {
            boxed = Il2CppFunctions.il2cpp_field_get_value_object(fieldPtr, 0);
        }
        else if (RuntimeManager.IsMono)
        {
            var domain = MonoFunctions.MonoGetRootDomain();
            if (domain == 0) return null;
            boxed = (nint)Methods.mono_field_get_value_object(
                (_MonoDomain*)domain, (_MonoClassField*)fieldPtr, null);
        }
        else return null;

        if (boxed == 0) return null;

        var p = RuntimeManager.IsIl2Cpp
            ? Il2CppFunctions.il2cpp_object_unbox(boxed)
            : MonoFunctions.MonoObjectUnbox(boxed);
        if (p == 0) return null;

        if (underlying == typeof(int)) return *(int*)p;
        if (underlying == typeof(uint)) return *(uint*)p;
        if (underlying == typeof(byte)) return *(byte*)p;
        if (underlying == typeof(sbyte)) return *(sbyte*)p;
        if (underlying == typeof(short)) return *(short*)p;
        if (underlying == typeof(ushort)) return *(ushort*)p;
        if (underlying == typeof(long)) return *(long*)p;
        if (underlying == typeof(ulong)) return *(ulong*)p;
        if (underlying == typeof(char)) return *(char*)p;
        if (underlying == typeof(bool)) return *(bool*)p;
        return null;
    }

    /// <summary>Defines a real managed enum. Returns null when the metadata was not usable.</summary>
    private static TypeBuilder? TryDefineEnum(nint klassPtr, TypeBuilder? parentTb,
        string ns, string simpleName)
    {
        var underlying = GetEnumUnderlying(klassPtr);
        if (underlying == null) return null;

        var members = ReadEnumMembers(klassPtr, underlying);
        if (members == null) return null;

        const TypeAttributes enumAttrs = TypeAttributes.Sealed;
        TypeBuilder tb;
        if (parentTb != null)
        {
            var name = UniqueTypeName($"{parentTb.FullName}+{simpleName}", simpleName);
            tb = parentTb.DefineNestedType(name, enumAttrs | TypeAttributes.NestedPublic, typeof(Enum));
        }
        else
        {
            var safeNs = SanitizeNamespace(ns);
            var safeName = SanitizeIdentifier(simpleName);
            var full = string.IsNullOrEmpty(safeNs) ? safeName : $"{safeNs}.{safeName}";
            tb = _module.DefineType(UniqueTypeName(full, full), enumAttrs | TypeAttributes.Public,
                typeof(Enum));
        }

        tb.DefineField("value__", underlying,
            FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, value) in members)
        {
            var safe = SanitizeMemberName(name);
            if (!used.Add(safe)) continue;
            tb.DefineField(safe, tb,
                    FieldAttributes.Public | FieldAttributes.Static |
                    FieldAttributes.Literal | FieldAttributes.HasDefault)
                .SetConstant(value);
        }

        _enumUnderlying[tb] = underlying;
        return tb;
    }

    /// <summary>Enums are laid out as their storage type on the stack, so IL treats them as such.</summary>
    private static Type StackTypeOf(Type t) => _enumUnderlying.GetValueOrDefault(t, t);

    private static bool IsEmittedEnum(Type t) => _enumUnderlying.ContainsKey(t);

    private static string MapType(string runtimeTypeName)
    {
        if (string.IsNullOrEmpty(runtimeTypeName)) return "nint";
        if (TypeMap.TryGetValue(runtimeTypeName, out var mapped)) return mapped;
        return "nint";
    }

    /// <summary>
    /// Member name. Explicit interface implementations are named with the full interface path at
    /// runtime (<c>System.Collections.IEnumerable.GetEnumerator</c>). Such a name is legal IL, but
    /// C# has no syntax to call it — the member would exist and be unreachable. Keep only the last
    /// segment; <see cref="ReserveMethod"/> / <see cref="ReserveMember"/> handle any collision.
    /// </summary>
    private static string SanitizeMemberName(string name)
    {
        var dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
            name = name[(dot + 1)..];
        return SanitizeIdentifier(name);
    }

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "_";
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '.' ? c : '_');
        var result = sb.ToString();
        if (result.Length == 0) return "_";
        if (char.IsDigit(result[0])) result = "_" + result;
        return result;
    }

    private static string SanitizeNamespace(string ns)
    {
        if (string.IsNullOrEmpty(ns)) return "";
        var parts = ns.Split('.');
        for (int i = 0; i < parts.Length; i++)
            parts[i] = SanitizeIdentifier(parts[i]);
        return string.Join(".", parts.Where(p => p.Length > 0));
    }
}
