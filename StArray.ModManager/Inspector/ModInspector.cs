using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Inspector;

/// <summary>
/// 自动检查器 —— 反射字段自动生成 ImGui 控件（类似 Unity Inspector）
/// </summary>
public static class ModInspector
{
    // ====== 缓存 ======

    private static readonly ConcurrentDictionary<Type, Entry[]> Cache = new();
    private static readonly ConcurrentDictionary<Type, Delegate> CustomDrawers = new();

    private sealed record Entry(
        string Label, Type ValueType,
        Func<object, object?> Get, Action<object, object?> Set,
        float RangeMin, float RangeMax, bool HasRange, int JsonLines,
        float[]? VecMins, float[]? VecMaxs);

    // ====== 公开 API ======

    /// <summary>为目标对象自动绘制检查器</summary>
    public static void Draw(object target)
    {
        var type = target.GetType();
        var entries = Cache.GetOrAdd(type, BuildEntries);
        foreach (var e in entries)
        {
            var val = e.Get(target);
            if (TryDrawField(e, val, out var newVal))
                e.Set(target, newVal);
        }
    }

    /// <summary>注册自定义类型绘制器（用于无法修改源码的类型）</summary>
    public static void RegisterDrawer<T>(Action<T> draw) where T : notnull
        => CustomDrawers[typeof(T)] = draw;

    // ---- 供 Mod 手动调用的控件方法 ----

    public static bool Bool(string label, ref bool v) => ImGui.Checkbox(label, ref v);
    public static bool Int(string label, ref int v) => ImGui.DragInt(label, ref v, 0.5f);
    public static bool SliderInt(string label, ref int v, int min, int max) => ImGui.SliderInt(label, ref v, min, max);
    public static bool Long(string label, ref long v) { var i = (int)v; if (ImGui.DragInt(label, ref i, 1f)) { v = i; return true; } return false; }
    public static bool Float(string label, ref float v) => ImGui.DragFloat(label, ref v, 0.1f);
    public static bool SliderFloat(string label, ref float v, float min, float max) => ImGui.SliderFloat(label, ref v, min, max);
    public static bool Double(string label, ref double v) { var f = (float)v; if (ImGui.DragFloat(label, ref f, 0.1f)) { v = f; return true; } return false; }
    public static bool Text(string label, ref string v, uint maxLen = 256) => ImGui.InputText(label, ref v, maxLen);
    public static bool Enum<T>(string label, ref T v) where T : struct, System.Enum
    {
        var names = System.Enum.GetNames<T>();
        var idx = Array.IndexOf(names, v.ToString());
        if (idx < 0) idx = 0;
        if (ImGui.Combo(label, ref idx, names, names.Length))
        {
            v = System.Enum.Parse<T>(names[idx]);
            return true;
        }
        return false;
    }
    public static bool Vec2(string label, ref Vector2 v) => ImGui.DragFloat2(label, ref v, 0.1f);
    public static bool Vec3(string label, ref Vector3 v) => ImGui.DragFloat3(label, ref v, 0.1f);
    public static bool Vec4(string label, ref Vector4 v) => ImGui.DragFloat4(label, ref v, 0.1f);
    public static bool Vec2(string label, ref Vector2 v, float min, float max) => ImGui.SliderFloat2(label, ref v, min, max);
    public static bool Vec3(string label, ref Vector3 v, float min, float max) => ImGui.SliderFloat3(label, ref v, min, max);
    public static bool Vec4(string label, ref Vector4 v, float min, float max) => ImGui.SliderFloat4(label, ref v, min, max);

    // ====== 内部实现 ======

    private static Entry[] BuildEntries(Type type)
    {
        var list = new List<Entry>();

        // 实例字段
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (type.GetEvent(f.Name) != null) continue; // 跳过事件字段
            var attrs = f.GetCustomAttributes().ToArray();
            list.Add(MakeEntry(f.Name, f.FieldType,
                attrs.OfType<ModSettingLabelAttribute>().FirstOrDefault(),
                attrs.OfType<ModSettingRangeAttribute>().FirstOrDefault(),
                t => f.GetValue(t), (t, v) => f.SetValue(t, v), attrs));
        }

        // 静态字段
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (type.GetEvent(f.Name) != null) continue;
            list.Add(MakeEntry($"[S] {f.Name}", f.FieldType,
                f.GetCustomAttribute<ModSettingLabelAttribute>(),
                f.GetCustomAttribute<ModSettingRangeAttribute>(),
                _ => f.GetValue(null), (_, v) => f.SetValue(null, v)));
        }

        // 实例属性
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

        // 静态属性
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
        Attribute[]? extraAttrs = null)
    {
        var json = extraAttrs?.OfType<ModSettingJsonAttribute>().FirstOrDefault();
        return new(la?.Label ?? name, vt, get, set,
            ra?.Min ?? 0f, ra?.Max ?? 0f, ra != null, json?.Lines ?? 0,
            ra?.Mins, ra?.Maxs);
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

    // ====== 类型分发 ======

    private static bool TryDrawField(Entry e, object? value, out object? newValue)
    {
        newValue = value;
        var type = e.ValueType;

        // ---- 基本类型 ----
        if (type == typeof(bool) && value is bool b)     { var v = b; if (Bool(e.Label, ref v)) { newValue = v; return true; } return false; }
        if (type == typeof(int) && value is int i)       { var v = i; bool ch = e.HasRange ? SliderInt(e.Label, ref v, (int)e.RangeMin, (int)e.RangeMax) : Int(e.Label, ref v); if (ch) { newValue = v; return true; } return false; }
        if (type == typeof(long) && value is long l)     { var v = l; if (Long(e.Label, ref v)) { newValue = v; return true; } return false; }
        if (type == typeof(float) && value is float f)   { var v = f; bool ch = e.HasRange ? SliderFloat(e.Label, ref v, e.RangeMin, e.RangeMax) : Float(e.Label, ref v); if (ch) { newValue = v; return true; } return false; }
        if (type == typeof(double) && value is double dv) { var v = dv; if (Double(e.Label, ref v)) { newValue = v; return true; } return false; }
        if (type.IsEnum) { var v = value ?? Activator.CreateInstance(type); return TryDrawEnum(e.Label, type, v, out newValue); }

        // ---- string（含 JSON 多行/树状 + 诊断） ----
        if (type == typeof(string) && value is string s)
        {
            var v = s ?? "";
            if (e.JsonLines > 0)
            {
                if (ImGui.TreeNode(e.Label))
                {
                    var key = $"str_{e.Label}";
                    var cur = JsonEditCache.GetOrAdd(key, v);
                    if (cur != v && !ImGui.IsItemActive()) cur = JsonEditCache[key] = v;

                    var changed = ImGui.InputTextMultiline($"##{key}", ref cur, 65536,
                        new Vector2(400, ImGui.GetTextLineHeight() * e.JsonLines));

                    if (changed)
                    {
                        JsonEditCache[key] = cur;
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(cur);
                            newValue = cur;
                            ImGui.TreePop();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), $"JSON 错误: {ex.Message}");
                        }
                    }
                    ImGui.TreePop();
                }
                return false;
            }
            if (Text(e.Label, ref v)) { newValue = v; return true; }
            return false;
        }

        // ---- Vector 类型（含 Range） ----
        if (type == typeof(Vector2) && value is Vector2 v2)
        {
            var v = v2;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat2(e.Label, ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat2(e.Label, ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec2(e.Label, ref v);
            if (ch) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(Vector3) && value is Vector3 v3)
        {
            var v = v3;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat3(e.Label, ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat3(e.Label, ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec3(e.Label, ref v);
            if (ch) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(Vector4) && value is Vector4 v4)
        {
            var v = v4;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat4(e.Label, ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat4(e.Label, ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec4(e.Label, ref v);
            if (ch) { newValue = v; return true; }
            return false;
        }

        // ---- ValueTuple<数字> → Vec2/3/4 ----
        if (value != null && type.IsGenericType && type.Name.StartsWith("ValueTuple`"))
        {
            var args = type.GetGenericArguments();
            if (args is [Type t1, Type t2] && IsNumeric(t1) && IsNumeric(t2))
            {
                var x = Convert.ToSingle(type.GetField("Item1")!.GetValue(value));
                var y = Convert.ToSingle(type.GetField("Item2")!.GetValue(value));
                var v = new Vector2(x, y);
                if (Vec2(e.Label, ref v))
                {
                    type.GetField("Item1")!.SetValue(value, Convert.ChangeType(v.X, t1));
                    type.GetField("Item2")!.SetValue(value, Convert.ChangeType(v.Y, t2));
                    newValue = value; return true;
                }
                return false;
            }
            if (args is [Type t1b, Type t2b, Type t3b] && IsNumeric(t1b) && IsNumeric(t2b) && IsNumeric(t3b))
            {
                var x = Convert.ToSingle(type.GetField("Item1")!.GetValue(value));
                var y = Convert.ToSingle(type.GetField("Item2")!.GetValue(value));
                var z = Convert.ToSingle(type.GetField("Item3")!.GetValue(value));
                var v = new Vector3(x, y, z);
                if (Vec3(e.Label, ref v)) { /* can't easily write back ValueTuple */ newValue = value; return true; }
                return false;
            }
        }

        // ---- 注册处理器 ----
        if (CustomDrawers.TryGetValue(type, out var d)) { d.DynamicInvoke(value); return false; }

        // ---- IModSettingCustomDraw ----
        if (value is IModSettingCustomDraw cd)
        {
            if (ImGui.TreeNode(e.Label)) { Draw(value); ImGui.Separator(); cd.DrawInspector(); ImGui.TreePop(); }
            return type.IsValueType;
        }

        // ---- JSON 编辑器：IEnumerable / IDictionary / 容器类型 ----
        if (value != null && (value is System.Collections.IEnumerable && !(value is string)))
        {
            return DrawJsonEditor(e, type, value, out newValue);
        }

        // ---- 递归展开（用户定义结构体/类） ----
        if (!type.IsPrimitive && type != typeof(string) && !type.IsEnum
            && type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(Vector4)
            && value != null && !type.IsGenericType)
        {
            if (ImGui.TreeNode(e.Label)) { Draw(value); ImGui.TreePop(); }
            return type.IsValueType;
        }

        // ---- JSON 兜底（Generic types that couldn't be handled above） ----
        if (value != null && !type.IsPrimitive && type != typeof(string))
        {
            return DrawJsonEditor(e, type, value, out newValue);
        }

        ImGui.TextDisabled(value == null ? $"{e.Label}: null" : $"{e.Label}: {value}");
        return false;
    }

    private static bool TryDrawEnum(string label, Type type, object value, out object? newValue)
    {
        var names = System.Enum.GetNames(type);
        var idx = Math.Max(0, Array.IndexOf(names, value.ToString() ?? ""));
        if (ImGui.Combo(label, ref idx, names, names.Length))
        {
            newValue = System.Enum.Parse(type, names[idx]);
            return true;
        }
        newValue = value;
        return false;
    }

    private static bool IsNumeric(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(float) || t == typeof(double)
        || t == typeof(short) || t == typeof(byte) || t == typeof(decimal);

    // ====== JSON 编辑器 ======

    private static readonly ConcurrentDictionary<string, string> JsonEditCache = new();

    private static bool DrawJsonEditor(Entry e, Type type, object value, out object? newValue)
    {
        newValue = value;
        var key = $"{type.FullName}_{e.Label}";

        if (!ImGui.TreeNode(e.Label)) return type.IsValueType;

        // 获取或初始化编辑文本
        var currentJson = System.Text.Json.JsonSerializer.Serialize(value,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var editText = JsonEditCache.GetOrAdd(key, currentJson);

        // 如果值被外部修改，更新编辑文本
        if (editText != currentJson && !ImGui.IsItemActive())
            editText = JsonEditCache[key] = currentJson;

        var lines = Math.Max(3, editText.Split('\n').Length);
        var changed = ImGui.InputTextMultiline($"##json_{key}", ref editText, 65536,
            new Vector2(400, ImGui.GetTextLineHeight() * Math.Min(lines, 12)));

        if (changed)
        {
            JsonEditCache[key] = editText;
            try
            {
                var deserialized = System.Text.Json.JsonSerializer.Deserialize(editText, type,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                if (deserialized != null)
                {
                    newValue = deserialized;
                    ImGui.TreePop();
                    return true;
                }
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "反序列化失败：结果为 null");
            }
            catch (Exception ex)
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), $"JSON 错误: {ex.Message}");
            }
        }

        ImGui.TreePop();
        return type.IsValueType;
    }
}
