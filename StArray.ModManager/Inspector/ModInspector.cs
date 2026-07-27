using System.Collections.Concurrent;
using System.Numerics;
using ImGuiNET;

namespace StArray.ModManager.Inspector;

/// <summary>
/// 自动检查器 / Auto inspector — reflection-based ImGui controls (Unity Inspector style)。
///
/// 成员需要<b>显式标记</b>才会出现：带任意 <see cref="ModSettingAttributeBase"/> 派生特性
/// （<see cref="ModSettingAttribute"/>、<see cref="ModSettingLabelAttribute"/> 等）即可，
/// 与访问修饰符无关 —— private 字段同样可以是设置项。
/// </summary>
public static partial class ModInspector
{
    private static readonly ConcurrentDictionary<Type, Entry[]> Cache = new();
    private static readonly ConcurrentDictionary<Type, Delegate> CustomDrawers = new();
    private static float _maxLabelWidth;
    private static float _leftMargin;
    private static float _controlWidth;

    private sealed record Entry(
        string Name, string Label, Type ValueType,
        Func<object, object?> Get, Action<object, object?>? Set,
        bool IsStatic, bool ReadOnly, bool Persist,
        int Sequence, int Order,
        float RangeMin, float RangeMax, bool HasRange,
        float[]? VecMins, float[]? VecMaxs,
        int JsonLines, LabelSide Side,
        string? Tooltip, string? Header, bool HeaderOpen,
        string? ShowIfMember, bool ShowIfInvert,
        bool IsColor, bool ColorAlpha, bool ColorPicker);

    /// <summary>被检查器纳入的成员：名称 + 类型 + 读写器。供设置持久化复用。</summary>
    public readonly record struct SettingMember(
        string Name, Type ValueType,
        Func<object, object?> Get, Action<object, object?> Set);

    /// <summary>
    /// 获取参与持久化的成员。与检查器面板使用同一份元数据，
    /// 因此在面板里能改的（含属性、静态成员、private 字段）都能被保存。
    /// 标记 <see cref="ModSettingNoSaveAttribute"/> 或只读的成员不在其中。
    /// </summary>
    public static IReadOnlyList<SettingMember> GetSettingMembers(Type type)
    {
        var list = new List<SettingMember>();
        foreach (var e in Cache.GetOrAdd(type, BuildEntries))
        {
            if (!e.Persist || e.Set == null) continue;
            list.Add(new SettingMember(e.Name, e.ValueType, e.Get, e.Set));
        }
        return list;
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

        var groupOpen = true;   // 当前分组是否展开；无分组时恒为 true
        var inGroup = false;

        foreach (var e in entries)
        {
            if (e.Header != null)
            {
                if (inGroup) ImGui.Unindent();
                var flags = e.HeaderOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
                groupOpen = ImGui.CollapsingHeader(e.Header, flags);
                inGroup = true;
                if (groupOpen) ImGui.Indent();
            }

            if (inGroup && !groupOpen) continue;

            var disabled = e.ReadOnly || !IsShowIfSatisfied(entries, target, e);
            if (disabled) ImGui.BeginDisabled();

            var val = e.Get(target);
            var changed = TryDrawField(e, val, out var newVal);

            if (disabled) ImGui.EndDisabled();
            DrawTooltip(e);

            if (changed && !disabled && e.Set != null)
                e.Set(target, newVal);
        }

        if (inGroup && groupOpen) ImGui.Unindent();
    }

    /// <summary>依赖成员为 false（或取反后为 false）时，本项变灰。</summary>
    private static bool IsShowIfSatisfied(Entry[] entries, object target, Entry e)
    {
        if (e.ShowIfMember == null) return true;

        foreach (var other in entries)
        {
            if (other.Name != e.ShowIfMember) continue;
            var v = other.Get(target);
            var on = v switch
            {
                bool b => b,
                null => false,
                _ => true,
            };
            return e.ShowIfInvert ? !on : on;
        }
        return true; // 找不到依赖成员就不禁用，避免静默失效
    }

    private static void DrawTooltip(Entry e)
    {
        if (e.Tooltip == null) return;
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
        ImGui.TextUnformatted(e.Tooltip);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
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
    /// <summary>Drag (long) / long drag，全 64 位精度</summary>
    public static unsafe bool Long(string label, ref long v)
    {
        fixed (long* p = &v)
            return ImGui.DragScalar(label, ImGuiDataType.S64, (nint)p, 1f);
    }
    /// <summary>DragFloat / float drag</summary>
    public static bool Float(string label, ref float v) => ImGui.DragFloat(label, ref v, 0.1f);
    /// <summary>SliderFloat / float slider</summary>
    public static bool SliderFloat(string label, ref float v, float min, float max) => ImGui.SliderFloat(label, ref v, min, max);
    /// <summary>Drag (double) / double drag，不降精度到 float</summary>
    public static unsafe bool Double(string label, ref double v)
    {
        fixed (double* p = &v)
            return ImGui.DragScalar(label, ImGuiDataType.Double, (nint)p, 0.1f);
    }
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
