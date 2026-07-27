using System.Reflection;
using System.Reflection.Emit;

namespace StArray.ModManager.Inspector;

partial class ModInspector
{
    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;

    private static Entry[] BuildEntries(Type type)
    {
        var list = new List<Entry>();
        var seq = 0;

        foreach (var f in type.GetFields(AllMembers))
        {
            // 编译器生成的自动属性后备字段等，一律跳过 —— 属性本身会被单独收集
            if (f.IsSpecialName || f.Name.Contains('<')) continue;
            if (!IsMarked(f, out var attrs)) continue;
            if (type.GetEvent(f.Name) != null) continue;

            var readOnly = f.IsInitOnly || f.IsLiteral ||
                           attrs.OfType<ModSettingReadOnlyAttribute>().Any();
            list.Add(MakeEntry(f.Name, f.FieldType, f.IsStatic, readOnly, seq++, attrs,
                BuildFieldGetter(f), readOnly ? null : BuildFieldSetter(f)));
        }

        foreach (var p in type.GetProperties(AllMembers))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            if (!IsMarked(p, out var attrs)) continue;

            var getter = p.GetGetMethod(true);
            if (getter == null) continue;
            var setter = p.GetSetMethod(true);
            var readOnly = setter == null || attrs.OfType<ModSettingReadOnlyAttribute>().Any();

            list.Add(MakeEntry(p.Name, p.PropertyType, getter.IsStatic, readOnly, seq++, attrs,
                BuildCall(getter, p.PropertyType, p.DeclaringType!),
                readOnly || setter == null ? null : BuildCallSet(setter, p.PropertyType, p.DeclaringType!)));
        }

        // 稳定排序：先按 Order，再按声明顺序
        return list.OrderBy(e => e.Order).ThenBy(e => e.Sequence).ToArray();
    }

    /// <summary>
    /// 是否被纳入检查器。语义是「显式标记才收」：带任意 <see cref="ModSettingAttributeBase"/>
    /// 派生特性即算标记，<see cref="ModSettingIgnoreAttribute"/> 一票否决。
    /// 访问修饰符不参与判断 —— private 字段照样可以是设置项。
    /// </summary>
    private static bool IsMarked(MemberInfo m, out Attribute[] attrs)
    {
        attrs = [];
        if (m.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) return false;
        var all = m.GetCustomAttributes().ToArray();
        if (!all.OfType<ModSettingAttributeBase>().Any()) return false;
        attrs = all;
        return true;
    }

    private static Entry MakeEntry(string name, Type vt, bool isStatic, bool readOnly, int seq,
        Attribute[] attrs, Func<object, object?> get, Action<object, object?>? set)
    {
        var label = attrs.OfType<ModSettingLabelAttribute>().FirstOrDefault()?.Label
                    ?? (isStatic ? $"[S] {name}" : name);
        var range = attrs.OfType<ModSettingRangeAttribute>().FirstOrDefault();
        var json = attrs.OfType<ModSettingJsonAttribute>().FirstOrDefault();
        var side = attrs.OfType<ModSettingLabelSideAttribute>().FirstOrDefault();
        var tip = attrs.OfType<ModSettingTooltipAttribute>().FirstOrDefault();
        var header = attrs.OfType<ModSettingHeaderAttribute>().FirstOrDefault();
        var showIf = attrs.OfType<ModSettingShowIfAttribute>().FirstOrDefault();
        var color = attrs.OfType<ModSettingColorAttribute>().FirstOrDefault();
        var order = attrs.OfType<ModSettingOrderAttribute>().FirstOrDefault();
        var noSave = attrs.OfType<ModSettingNoSaveAttribute>().Any();

        return new Entry(
            Name: name,
            Label: label,
            ValueType: vt,
            Get: get,
            Set: set,
            IsStatic: isStatic,
            ReadOnly: readOnly,
            Persist: !noSave && set != null,
            Sequence: seq,
            Order: order?.Order ?? 0,
            RangeMin: range?.Min ?? 0f,
            RangeMax: range?.Max ?? 0f,
            HasRange: range != null && range.Mins == null,
            VecMins: range?.Mins,
            VecMaxs: range?.Maxs,
            JsonLines: json?.Lines ?? 0,
            Side: side?.Side ?? LabelSide.Top,
            Tooltip: tip?.Text,
            Header: header?.Title,
            HeaderOpen: header?.DefaultOpen ?? true,
            ShowIfMember: showIf?.Member,
            ShowIfInvert: showIf?.Invert ?? false,
            IsColor: color != null,
            ColorAlpha: color?.Alpha ?? true,
            ColorPicker: color?.Picker ?? false);
    }

    // ── 访问器 ──
    //
    // 用 DynamicMethod 而非 Expression.Compile：后者生成的委托受可见性检查约束，
    // 访问 private 成员会在运行时抛 FieldAccessException。skipVisibility 绕开这一点，
    // 这是支持「不论权限修饰」所必需的。

    private static Func<object, object?> BuildFieldGetter(FieldInfo f)
    {
        var owner = f.DeclaringType!;
        var dm = new DynamicMethod($"get_{owner.Name}_{f.Name}", typeof(object),
            [typeof(object)], owner.Module, skipVisibility: true);
        var il = dm.GetILGenerator();

        if (f.IsStatic)
        {
            il.Emit(OpCodes.Ldsfld, f);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(owner.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, owner);
            il.Emit(OpCodes.Ldfld, f);
        }

        if (f.FieldType.IsValueType) il.Emit(OpCodes.Box, f.FieldType);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Func<object, object?>>();
    }

    private static Action<object, object?> BuildFieldSetter(FieldInfo f)
    {
        var owner = f.DeclaringType!;
        var dm = new DynamicMethod($"set_{owner.Name}_{f.Name}", null,
            [typeof(object), typeof(object)], owner.Module, skipVisibility: true);
        var il = dm.GetILGenerator();

        if (!f.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(owner.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, owner);
        }

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(f.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, f.FieldType);
        il.Emit(f.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, f);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Action<object, object?>>();
    }

    private static Func<object, object?> BuildCall(MethodInfo getter, Type valueType, Type owner)
    {
        var dm = new DynamicMethod($"get_{owner.Name}_{getter.Name}", typeof(object),
            [typeof(object)], owner.Module, skipVisibility: true);
        var il = dm.GetILGenerator();

        if (!getter.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(owner.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, owner);
        }

        il.Emit(getter.IsStatic || owner.IsValueType ? OpCodes.Call : OpCodes.Callvirt, getter);
        if (valueType.IsValueType) il.Emit(OpCodes.Box, valueType);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Func<object, object?>>();
    }

    private static Action<object, object?> BuildCallSet(MethodInfo setter, Type valueType, Type owner)
    {
        var dm = new DynamicMethod($"set_{owner.Name}_{setter.Name}", null,
            [typeof(object), typeof(object)], owner.Module, skipVisibility: true);
        var il = dm.GetILGenerator();

        if (!setter.IsStatic)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(owner.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, owner);
        }

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(valueType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, valueType);
        il.Emit(setter.IsStatic || owner.IsValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Action<object, object?>>();
    }

    private static bool IsNumeric(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double)
        || t == typeof(short) || t == typeof(byte) || t == typeof(decimal)
        || t == typeof(sbyte) || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong);
}
