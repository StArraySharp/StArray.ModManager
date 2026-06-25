using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace StArray.ModManager.Inspector;

/// <summary>自动检查器 / Auto inspector — reflection-based ImGui controls (Unity Inspector style)</summary>
public static partial class ModInspector
{
    private static readonly ConcurrentDictionary<Type, Entry[]> Cache = new();
    private static readonly ConcurrentDictionary<Type, Delegate> CustomDrawers = new();
    private static float _maxLabelWidth;
    private static float _leftMargin;
    private static float _controlWidth;

    private sealed record Entry(
        string Label, Type ValueType,
        Func<object, object?> Get, Action<object, object?> Set,
        float RangeMin, float RangeMax, bool HasRange, int JsonLines,
        float[]? VecMins, float[]? VecMaxs, LabelSide Side);

    /// <summary>获取检查器展示的实例字段（排除 IModPlugin 属性等）</summary>
    public static FieldInfo[] GetInspectorFields(Type type)
    {
        var list = new List<FieldInfo>();
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.GetCustomAttribute<ModSettingIgnoreAttribute>() != null) continue;
            if (type.GetEvent(f.Name) != null) continue;
            list.Add(f);
        }
        return list.ToArray();
    }

    /// <summary>为目标对象自动绘制检查器 / Draw inspector for target object</summary>
    public static void Draw(object target)
    {
        var type = target.GetType();
        var entries = Cache.GetOrAdd(type, BuildEntries);

        // 计算 Left 侧标签最大宽度，用于对齐控件
        _maxLabelWidth = 0;
        foreach (var e in entries)
        {
            if (e.Side == LabelSide.Left)
            {
                var sz = ImGui.CalcTextSize(e.Label);
                if (sz.X > _maxLabelWidth) _maxLabelWidth = sz.X;
            }
        }

        // 控件可用宽度：保持与 Left 标签列对齐
        var style = ImGui.GetStyle();
        _leftMargin = ImGui.GetCursorPosX();
        _controlWidth = Math.Max(80, ImGui.GetContentRegionAvail().X - _maxLabelWidth
            - style.ItemInnerSpacing.X - style.FramePadding.X * 2);

        foreach (var e in entries)
        {
            var val = e.Get(target);
            if (TryDrawField(e, val, out var newVal))
                e.Set(target, newVal);
        }
    }

    /// <summary>注册自定义类型绘制器 / Register custom type drawer</summary>
    public static void RegisterDrawer<T>(Action<T> draw) where T : notnull
        => CustomDrawers[typeof(T)] = draw;

    /// <summary>Checkbox / bool checkbox</summary>
    public static bool Bool(string label, ref bool v) => ImGui.Checkbox(label, ref v);
    /// <summary>DragInt / int drag</summary>
    public static bool Int(string label, ref int v) => ImGui.DragInt(label, ref v, 0.5f);
    /// <summary>SliderInt / int slider</summary>
    public static bool SliderInt(string label, ref int v, int min, int max) => ImGui.SliderInt(label, ref v, min, max);
    /// <summary>Drag (long) / long drag</summary>
    public static bool Long(string label, ref long v) { var i = (int)v; if (ImGui.DragInt(label, ref i, 1f)) { v = i; return true; } return false; }
    /// <summary>DragFloat / float drag</summary>
    public static bool Float(string label, ref float v) => ImGui.DragFloat(label, ref v, 0.1f);
    /// <summary>SliderFloat / float slider</summary>
    public static bool SliderFloat(string label, ref float v, float min, float max) => ImGui.SliderFloat(label, ref v, min, max);
    /// <summary>Drag (double) / double drag</summary>
    public static bool Double(string label, ref double v) { var f = (float)v; if (ImGui.DragFloat(label, ref f, 0.1f)) { v = f; return true; } return false; }
    /// <summary>InputText / string input</summary>
    public static bool Text(string label, ref string v, uint maxLen = 256) => ImGui.InputText(label, ref v, maxLen);
    /// <summary>Combo 枚举 / enum combo</summary>
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

    /// <summary>DragFloat2 / Vector2 drag</summary>
    public static bool Vec2(string label, ref Vector2 v) => ImGui.DragFloat2(label, ref v, 0.1f);
    /// <summary>DragFloat3 / Vector3 drag</summary>
    public static bool Vec3(string label, ref Vector3 v) => ImGui.DragFloat3(label, ref v, 0.1f);
    /// <summary>DragFloat4 / Vector4 drag</summary>
    public static bool Vec4(string label, ref Vector4 v) => ImGui.DragFloat4(label, ref v, 0.1f);
    /// <summary>SliderFloat2 / Vector2 slider</summary>
    public static bool Vec2(string label, ref Vector2 v, float min, float max) => ImGui.SliderFloat2(label, ref v, min, max);
    /// <summary>SliderFloat3 / Vector3 slider</summary>
    public static bool Vec3(string label, ref Vector3 v, float min, float max) => ImGui.SliderFloat3(label, ref v, min, max);
    /// <summary>SliderFloat4 / Vector4 slider</summary>
    public static bool Vec4(string label, ref Vector4 v, float min, float max) => ImGui.SliderFloat4(label, ref v, min, max);
}
