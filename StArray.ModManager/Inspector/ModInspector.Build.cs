using System.Linq.Expressions;
using System.Reflection;

namespace StArray.ModManager.Inspector;

partial class ModInspector
{
    private static Entry[] BuildEntries(Type type)
    {
        var list = new List<Entry>();

        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (type.GetEvent(f.Name) != null) continue;
            var attrs = f.GetCustomAttributes().ToArray();
            list.Add(MakeEntry(f.Name, f.FieldType,
                attrs.OfType<ModSettingLabelAttribute>().FirstOrDefault(),
                attrs.OfType<ModSettingRangeAttribute>().FirstOrDefault(),
                t => f.GetValue(t), (t, v) => f.SetValue(t, v),
                attrs.OfType<ModSettingLabelSideAttribute>().FirstOrDefault(), attrs));
        }

        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (type.GetEvent(f.Name) != null) continue;
            list.Add(MakeEntry($"[S] {f.Name}", f.FieldType,
                f.GetCustomAttribute<ModSettingLabelAttribute>(),
                f.GetCustomAttribute<ModSettingRangeAttribute>(),
                _ => f.GetValue(null), (_, v) => f.SetValue(null, v)));
        }

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (p.GetIndexParameters().Length > 0 || !p.CanRead || !p.CanWrite) continue;
            var get = CompileGet(p, type);
            var set = CompileSet(p, type);
            if (get == null || set == null) continue;
            list.Add(MakeEntry(p.Name, p.PropertyType,
                p.GetCustomAttribute<ModSettingLabelAttribute>(),
                p.GetCustomAttribute<ModSettingRangeAttribute>(), get, set));
        }

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (p.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (p.GetIndexParameters().Length > 0 || !p.CanRead || !p.CanWrite) continue;
            var get = CompileStaticGet(p);
            var set = CompileStaticSet(p);
            if (get == null || set == null) continue;
            list.Add(MakeEntry($"[S] {p.Name}", p.PropertyType,
                p.GetCustomAttribute<ModSettingLabelAttribute>(),
                p.GetCustomAttribute<ModSettingRangeAttribute>(), get, set));
        }

        return list.ToArray();
    }

    private static Entry MakeEntry(string name, Type vt,
        ModSettingLabelAttribute? la, ModSettingRangeAttribute? ra,
        Func<object, object?> get, Action<object, object?> set,
        ModSettingLabelSideAttribute? ls = null, Attribute[]? extraAttrs = null)
    {
        var json = extraAttrs?.OfType<ModSettingJsonAttribute>().FirstOrDefault();
        return new(la?.Label ?? name, vt, get, set,
            ra?.Min ?? 0f, ra?.Max ?? 0f, ra != null, json?.Lines ?? 0,
            ra?.Mins, ra?.Maxs, ls?.Side ?? LabelSide.Top);
    }

    private static Func<object, object?>? CompileGet(PropertyInfo p, Type owner)
    {
        try
        {
            var t = Expression.Parameter(typeof(object));
            var access = Expression.Property(Expression.Convert(t, owner), p);
            return Expression.Lambda<Func<object, object?>>(Expression.Convert(access, typeof(object)), t).Compile();
        }
        catch { return null; }
    }

    private static Action<object, object?>? CompileSet(PropertyInfo p, Type owner)
    {
        try
        {
            var t = Expression.Parameter(typeof(object));
            var v = Expression.Parameter(typeof(object));
            var assign = Expression.Assign(
                Expression.Property(Expression.Convert(t, owner), p),
                Expression.Convert(v, p.PropertyType));
            return Expression.Lambda<Action<object, object?>>(assign, t, v).Compile();
        }
        catch { return null; }
    }

    private static Func<object, object?>? CompileStaticGet(PropertyInfo p)
    {
        try
        {
            var access = Expression.Property(null, p);
            return Expression.Lambda<Func<object, object?>>(Expression.Convert(access, typeof(object))).Compile();
        }
        catch { return null; }
    }

    private static Action<object, object?>? CompileStaticSet(PropertyInfo p)
    {
        try
        {
            var v = Expression.Parameter(typeof(object));
            var assign = Expression.Assign(
                Expression.Property(null, p),
                Expression.Convert(v, p.PropertyType));
            return Expression.Lambda<Action<object, object?>>(assign, v).Compile();
        }
        catch { return null; }
    }

    private static bool IsNumeric(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double)
        || t == typeof(short) || t == typeof(byte) || t == typeof(decimal);
}
