using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Runtime.Metadata;

/// <summary>
/// Il2Cpp 后端元数据提供者。
/// </summary>
/// <remarks>
/// Il2Cpp 元数据读取全程安全（无历史崩溃），但保持与 Mono 相同的"收集期快照"纪律：
/// 类型清单在 <see cref="CollectTypes"/> 一次性登记，后续成员读取只针对已收集类型。
/// </remarks>
public sealed class Il2CppMetadataProvider : IRuntimeMetadataProvider
{
    private readonly HashSet<nint> _collected = [];

    // ── 程序集 ──

    public IEnumerable<(string Name, string? Filename, nint Ptr)> EnumerateAssemblies()
    {
        var list = new List<(string, string?, nint)>();
        Il2CppDomain.Current?.GetAssemblies(); // 侧效果：确保域可用
        // il2cpp_assembly_foreach 不可用，走 domain 抽象（内部为指针列表）
        foreach (var asm in Il2CppDomain.Current?.GetAssemblies() ?? [])
            list.Add((asm.Name, asm.Filename, asm.Ptr));
        return list;
    }

    public IEnumerable<(nint TypePtr, TypeIdentity Identity, nint NestingParent)> CollectTypes(nint assemblyPtr)
    {
        var image = Il2CppFunctions.il2cpp_assembly_get_image(assemblyPtr);
        if (image == 0) yield break;

        var classCount = Il2CppFunctions.il2cpp_image_get_class_count(image);

        // 名字 → 指针表：Il2Cpp 的嵌套路径编码在类名里（"Outer+Inner"），用于补嵌套关系
        var nameToPtr = new Dictionary<string, nint>();
        var classes = new List<nint>((int)classCount);
        for (uint i = 0; i < classCount; i++)
        {
            var klass = Il2CppFunctions.il2cpp_image_get_class(image, i);
            if (klass == 0) continue;
            classes.Add(klass);
            var cname = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            nameToPtr[cname] = klass;
        }

        foreach (var klass in classes)
        {
            _collected.Add(klass);

            var rawName = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_name(klass)) ?? "";
            var ns = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_class_get_namespace(klass)) ?? "";

            // 嵌套：优先嵌套枚举 API，回退到 "A+B" 名字链推断
            nint parent = 0;
            var iter = IntPtr.Zero;
            while (true)
            {
                var nested = Il2CppFunctions.il2cpp_class_get_nested_types(klass, ref iter);
                if (nested == 0) break;
                // 本循环只用于建立 children；parent 关系下面统一定
                break;
            }

            var dot = rawName.LastIndexOf('+');
            if (dot > 0)
            {
                var parentName = rawName[..dot];
                if (nameToPtr.TryGetValue(parentName, out var p) && p != klass)
                    parent = p;
            }

            yield return (klass, new TypeIdentity(ns, rawName.Replace('+', '.')), parent);
        }
    }

    // ── 类型 ──

    public bool IsEnum(nint typePtr)
        => _collected.Contains(typePtr) && Il2CppFunctions.il2cpp_class_is_enum(typePtr);

    public bool IsInterface(nint typePtr)
        => _collected.Contains(typePtr) && Il2CppFunctions.il2cpp_class_is_interface(typePtr);

    public bool IsGenericTypeDefinition(nint typePtr)
        => _collected.Contains(typePtr) && Il2CppFunctions.il2cpp_class_is_generic(typePtr);

    public nint GetParentClass(nint typePtr)
        => _collected.Contains(typePtr) ? Il2CppFunctions.il2cpp_class_get_parent(typePtr) : 0;

    public IEnumerable<nint> EnumerateInterfaces(nint typePtr)
    {
        nint iter = 0;
        while (true)
        {
            var iface = Il2CppFunctions.il2cpp_class_get_interfaces(typePtr, ref iter);
            if (iface == 0) yield break;
            yield return iface;
        }
    }

    public bool IsCollectedType(nint typePtr) => _collected.Contains(typePtr);

    // ── 成员 ──

    public IEnumerable<FieldSnapshot> EnumerateFields(nint typePtr)
    {
        if (!_collected.Contains(typePtr)) yield break;
        nint iter = 0;
        while (true)
        {
            var f = Il2CppFunctions.il2cpp_class_get_fields(typePtr, ref iter);
            if (f == 0) yield break;

            var name = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_field_get_name(f)) ?? "";
            if (name.Length == 0) continue;

            var flags = Il2CppFunctions.il2cpp_field_get_flags(f);
            var raw = "";
            var typePtrF = Il2CppFunctions.il2cpp_field_get_type(f);
            if (typePtrF != 0)
                raw = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(typePtrF)) ?? "";

            yield return new FieldSnapshot(name, (flags & 0x10) != 0, IsLiteral: false,
                "nint", raw, typePtrF, f, (uint)flags);
        }
    }

    public IEnumerable<MethodSnapshot> EnumerateMethods(nint typePtr)
    {
        if (!_collected.Contains(typePtr)) yield break;
        nint iter = 0;
        while (true)
        {
            var m = Il2CppFunctions.il2cpp_class_get_methods(typePtr, ref iter);
            if (m == 0) yield break;

            var name = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_method_get_name(m)) ?? "";
            if (name.Length == 0 || name.StartsWith('.') || name.StartsWith('<')) continue;

            uint pc = Il2CppFunctions.il2cpp_method_get_param_count(m);
            uint flagsDummy = 0;
            uint flags = Il2CppFunctions.il2cpp_method_get_flags(m, ref flagsDummy);

            var rawRet = "";
            var retTypePtr = Il2CppFunctions.il2cpp_method_get_return_type(m);
            if (retTypePtr != 0)
                rawRet = Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(retTypePtr)) ?? "";

            var paramTypePtrs = new nint[pc];
            var rawParams = new string[pc];
            for (uint i = 0; i < pc; i++)
            {
                var p = Il2CppFunctions.il2cpp_method_get_param(m, i);
                paramTypePtrs[i] = p;
                rawParams[i] = p != 0
                    ? Marshal.PtrToStringAnsi(Il2CppFunctions.il2cpp_type_get_name(p)) ?? ""
                    : "";
            }

            yield return new MethodSnapshot(name, "nint", new string[pc], (flags & 0x10) != 0,
                retTypePtr, paramTypePtrs, rawRet, rawParams,
                IsCtor: name == ".ctor" || name == ".cctor");
        }
    }

    public List<(string Name, object Value)>? ReadEnumMembers(nint typePtr, Type underlying)
    {
        var members = new List<(string, object)>();
        foreach (var f in EnumerateFields(typePtr))
        {
            if (!f.IsStatic || f.Name.Length == 0) continue;
            // Il2Cpp 枚举 literal flags 常缺，凡 static 同类型字段皆视为成员候选，
            // 经 il2cpp_field_get_value_object 装箱读取（Il2Cpp 路径历史安全）。
            var boxed = Il2CppFunctions.il2cpp_field_get_value_object(f.FieldPtr, 0);
            if (boxed == 0) continue;
            var p = Il2CppFunctions.il2cpp_object_unbox(boxed);
            if (p == 0) continue;

            object? value = ReadBoxed(p, underlying);
            if (value == null) continue;
            members.Add((f.Name, value));
        }
        return members;
    }

    private static unsafe object? ReadBoxed(nint p, Type underlying) => underlying == typeof(int) ? *(int*)p
        : underlying == typeof(uint) ? *(uint*)p
        : underlying == typeof(byte) ? *(byte*)p
        : underlying == typeof(sbyte) ? *(sbyte*)p
        : underlying == typeof(short) ? *(short*)p
        : underlying == typeof(ushort) ? *(ushort*)p
        : underlying == typeof(long) ? *(long*)p
        : underlying == typeof(ulong) ? *(ulong*)p
        : underlying == typeof(char) ? *(char*)p
        : underlying == typeof(bool) ? *(bool*)p
        : null;

    // ── 线程 ──

    private bool _attached;

    public bool AttachThread()
    {
        var domain = Il2CppDomain.Current;
        if (domain == null) return false;
        domain.ThreadAttach();
        return _attached = true;
    }

    public void DetachThread()
    {
        if (_attached)
        {
            Il2CppDomain.Current?.ThreadDetach();
            _attached = false;
        }
    }
}
