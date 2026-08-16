using System.Runtime.InteropServices;
using StArray.ModManager.Manager;
using StArray.ModManager.Mono;

namespace StArray.ModManager.Runtime.Metadata;

/// <summary>
/// Mono 后端元数据提供者。
/// </summary>
/// <remarks>
/// 历史崩溃教训（均已防护，勿移除）：
/// <list type="bullet">
/// <item>mono_class_get 抛 Mono 异常（TypeLoad）是正常行为——逐个跳过</item>
/// <item>嵌套类型的 mono_class_get_fields 迭代器在混淆程序集上第二次调用即 0xC0000005
/// ——字段快照在"收集期"一次性取完并缓存，之后绝不重入迭代器</item>
/// <item>对未收集类型指针取 name/namespace/nesting 会访问违例——全部经 <see cref="IsCollectedType"/> 拦截</item>
/// <item>mono_field_get_value_object 装箱路径崩溃——literal 读取走 mono_field_get_data 原始字节</item>
/// <item>mono_assembly_load_references / mono_class_get_checked(_MonoError) 会 failfast——禁用</item>
/// </list>
/// </remarks>
public sealed unsafe class MonoMetadataProvider : IRuntimeMetadataProvider
{
    private readonly HashSet<nint> _collected = [];
    private readonly Dictionary<nint, List<FieldSnapshot>> _fieldCache = [];

    // ── 程序集 ──

    public IEnumerable<(string Name, string? Filename, nint Ptr)> EnumerateAssemblies()
    {
        var list = new List<(string, string?, nint)>();
        MonoFunctions.MonoAssemblyForeach(asm =>
        {
            var image = MonoFunctions.MonoAssemblyGetImage(asm);
            var name = MonoFunctions.MonoImageGetName(image);
            if (string.IsNullOrEmpty(name)) return;
            list.Add((name!, MonoFunctions.MonoImageGetFilename(image), asm));
        });
        return list;
    }

    public IEnumerable<(nint TypePtr, TypeIdentity Identity, nint NestingParent)> CollectTypes(nint assemblyPtr)
    {
        var image = MonoFunctions.MonoAssemblyGetImage(assemblyPtr);
        if (image == 0) return [];

        var result = new List<(nint, TypeIdentity, nint)>();
        CollectTypesCore(image, result);
        return result;
    }

    /// <summary>迭代器方法不能标 unsafe，指针操作集中在此。</summary>
    private unsafe void CollectTypesCore(nint image, List<(nint, TypeIdentity, nint)> result)
    {
        var table = Methods.mono_image_get_table_info((_MonoImage*)image,
            (int)MonoMetaTableEnum.MONO_TABLE_TYPEDEF);
        if (table == null) return;
        var rows = Methods.mono_table_info_get_rows(table);

        for (int i = 1; i <= rows; i++)
        {
            _MonoClass* klass;
            try
            {
                // 完整 TypeDef token (0x02 << 24 | RID)，不是裸行号
                klass = Methods.mono_class_get((_MonoImage*)image, 0x02000000u | (uint)i);
            }
            catch
            {
                continue; // 解析失败的类型（TypeLoad 等）跳过，不中断收集
            }
            if (klass == null) continue;

            var ptr = (nint)klass;
            _collected.Add(ptr);

            // 字段快照：趁收集期一次性取全。嵌套类型上重入迭代器会崩溃，
            // 这里每个类型只走一遍 mono_class_get_fields。
            SnapshotFields(ptr);

            var name = MonoFunctions.MonoClassGetName(ptr) ?? "";
            var ns = MonoFunctions.MonoClassGetNamespace(ptr) ?? "";
            var parent = MonoFunctions.MonoClassGetNestingType(ptr);
            result.Add((ptr, new TypeIdentity(ns, name), parent != ptr ? parent : 0));
        }
    }

    /// <summary>一次性缓存字段快照，避免后续阶段重入 mono_class_get_fields。</summary>
    private void SnapshotFields(nint klassPtr)
    {
        var list = new List<FieldSnapshot>();
        nint iter = 0;
        while (true)
        {
            var f = MonoFunctions.MonoClassGetFields(klassPtr, ref iter);
            if (f == 0) break;

            var name = MonoFunctions.MonoFieldGetName(f);
            if (string.IsNullOrEmpty(name)) continue;

            var flags = MonoFunctions.MonoFieldGetFlags(f);
            bool isStatic = (flags & 0x10) != 0;

            var raw = "";
            var typePtr = MonoFunctions.MonoFieldGetType(f);
            if (typePtr != 0)
                raw = MonoFunctions.MonoTypeGetName(typePtr) ?? "";

            list.Add(new FieldSnapshot(name!, isStatic, (flags & 0x40) != 0,
                "nint", raw, typePtr, f, flags));
        }
        _fieldCache[klassPtr] = list;
    }

    // ── 类型 ──

    public bool IsEnum(nint typePtr)
        => _collected.Contains(typePtr) && MonoFunctions.MonoClassIsEnum(typePtr);

    public bool IsInterface(nint typePtr)
        => _collected.Contains(typePtr) && (MonoFunctions.MonoClassGetFlags(typePtr) & 0x20) != 0;

    public bool IsGenericTypeDefinition(nint typePtr)
        => false; // Mono typedef 枚举只产出具体类型；泛型定义由名字 "`n" 识别并跳过

    public nint GetParentClass(nint typePtr)
        => _collected.Contains(typePtr) ? MonoFunctions.MonoClassGetParent(typePtr) : 0;

    public IEnumerable<nint> EnumerateInterfaces(nint typePtr)
    {
        yield break; // Mono 接口枚举在现有绑定下不可用（历史如此）
    }

    public bool IsCollectedType(nint typePtr) => _collected.Contains(typePtr);

    // ── 成员 ──

    public IEnumerable<FieldSnapshot> EnumerateFields(nint typePtr)
        => _fieldCache.GetValueOrDefault(typePtr) ?? [];

    public IEnumerable<MethodSnapshot> EnumerateMethods(nint typePtr)
    {
        if (!IsCollectedType(typePtr)) return [];
        var list = new List<MethodSnapshot>();
        EnumerateMethodsCore(typePtr, list);
        return list;
    }

    /// <summary>迭代器不能标 unsafe，指针操作集中在此。</summary>
    private unsafe void EnumerateMethodsCore(nint typePtr, List<MethodSnapshot> list)
    {
        nint iter = 0;
        while (true)
        {
            var m = MonoFunctions.MonoClassGetMethods(typePtr, ref iter);
            if (m == 0) break;

            var name = MonoFunctions.MonoMethodGetName(m);
            if (string.IsNullOrEmpty(name) || name.StartsWith('.') || name.StartsWith('<')) continue;

            var sig = MonoFunctions.MonoMethodSignature(m);
            if (sig == 0) continue;

            uint pc = MonoFunctions.MonoSignatureGetParamCount(sig);
            uint flags = MonoFunctions.MonoMethodGetFlags(m);
            bool isStatic = (flags & 0x10) != 0;

            var rawRet = "";
            var retTypePtr = MonoFunctions.MonoSignatureGetReturnType(sig);
            if (retTypePtr != 0)
                rawRet = MonoFunctions.MonoTypeGetName(retTypePtr) ?? "";

            var paramTypePtrs = new nint[pc];
            var rawParams = new string[pc];
            void* pit = null;
            for (uint i = 0; i < pc; i++)
            {
                var pt = MonoFunctions.MonoSignatureGetParams(sig, ref pit);
                paramTypePtrs[i] = pt;
                rawParams[i] = pt != 0 ? MonoFunctions.MonoTypeGetName(pt) ?? "" : "";
            }

            list.Add(new MethodSnapshot(name!, "nint", new string[pc], isStatic,
                retTypePtr, paramTypePtrs, rawRet, rawParams, IsCtor: name == ".ctor" || name == ".cctor"));
        }
    }

    public List<(string Name, object Value)>? ReadEnumMembers(nint typePtr, Type underlying)
    {
        var members = new List<(string, object)>();
        foreach (var f in EnumerateFields(typePtr))
        {
            if (!f.IsStatic || !f.IsLiteral || f.Name.Length == 0) continue;

            object? value = null;

            // 读取链（独立 mono 与 Unity mono 的 mono_field_get_data 语义不一致：
            // 后者对 literal 直接给常量指针；前者实测返回错误数据间距）。
            // 先试装箱版 mono_field_get_value_object（MSYS2 独立 mono 上安全），
            // 失败再退 raw data。
            var domain = MonoFunctions.MonoGetRootDomain();
            if (domain != 0)
            {
                try
                {
                    var boxed = (nint)Methods.mono_field_get_value_object(
                        (_MonoDomain*)domain, (_MonoClassField*)f.FieldPtr, null);
                    if (boxed != 0)
                    {
                        var p = MonoFunctions.MonoObjectUnbox(boxed);
                        if (p != 0)
                            value = ReadBoxed(p, underlying);
                    }
                }
                catch
                {
                    // 混淆程序集防崩：装箱失败不动声色，走 raw 回退
                }
            }

            value ??= ReadRawData(f.FieldPtr, underlying);

            if (value == null) return null; // 部分枚举不可信，整体降级
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

    private static unsafe object? ReadRawData(nint fieldPtr, Type underlying)
    {
        var data = MonoFunctions.MonoFieldGetData(fieldPtr);
        return data != 0 ? ReadBoxed(data, underlying) : null;
    }

    // ── 线程 ──

    private bool _attached;

    public bool AttachThread()
    {
        if (MonoFunctions.IsMonoThreadAttached()) return false;
        MonoDomain.Current?.ThreadAttach();
        return _attached = true;
    }

    public void DetachThread()
    {
        if (_attached)
        {
            MonoDomain.Current?.ThreadDetach();
            _attached = false;
        }
    }
}
