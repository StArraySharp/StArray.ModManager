using StArray.ModManager.Resources;
using System.Collections.Concurrent;
using System.Numerics;
using ImGuiNET;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Inspector;

partial class ModInspector
{
    private static string WL(Entry e) => $"##{e.Label}";
    private static void Pre(Entry e)
    {
        if (e.Side == LabelSide.Top)
        {
            ImGui.SetCursorPosX(_leftMargin);
            ImGui.Text(e.Label);
            ImGui.SetNextItemWidth(_controlWidth);
        }
        else if (e.Side == LabelSide.Left)
        {
            ImGui.AlignTextToFramePadding();
            var pos = ImGui.GetCursorPosX();
            ImGui.Text(e.Label);
            ImGui.SameLine(pos + _maxLabelWidth + ImGui.GetStyle().ItemInnerSpacing.X);
        }
        else if (e.Side == LabelSide.Right)
        {
            ImGui.SetNextItemWidth(_controlWidth);
        }
    }
    private static void Post(Entry e)
    {
        if (e.Side == LabelSide.Right) { ImGui.SameLine(); ImGui.Text(e.Label); }
    }

    private static bool TryDrawField(Entry e, object? value, out object? newValue)
    {
        newValue = value;
        var type = e.ValueType;

        // ---- 基本类型 ----
        if (type == typeof(bool) && value is bool b)     { Pre(e); var v = b; if (Bool(WL(e), ref v)) { newValue = v; Post(e); return true; } Post(e); return false; }
        if (type == typeof(int) && value is int i)       { Pre(e); var v = i; bool ch = e.HasRange ? SliderInt(WL(e), ref v, (int)e.RangeMin, (int)e.RangeMax) : Int(WL(e), ref v); if (ch) { newValue = v; Post(e); return true; } Post(e); return false; }
        if (type == typeof(long) && value is long l)     { Pre(e); var v = l; if (Long(WL(e), ref v)) { newValue = v; Post(e); return true; } Post(e); return false; }
        if (type == typeof(float) && value is float f)   { Pre(e); var v = f; bool ch = e.HasRange ? SliderFloat(WL(e), ref v, e.RangeMin, e.RangeMax) : Float(WL(e), ref v); if (ch) { newValue = v; Post(e); return true; } Post(e); return false; }
        if (type == typeof(double) && value is double dv) { Pre(e); var v = dv; if (Double(WL(e), ref v)) { newValue = v; Post(e); return true; } Post(e); return false; }
        if (type.IsEnum) { Pre(e); var v = value ?? Activator.CreateInstance(type); var r = TryDrawEnum(WL(e), type, v, out newValue); Post(e); return r; }

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
                        new Vector2(Math.Max(300, ImGui.GetContentRegionAvail().X - 20), ImGui.GetTextLineHeight() * e.JsonLines));

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
                            ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), $"JSON: {ex.Message}");
                        }
                    }
                    ImGui.TreePop();
                }
                return false;
            }
            Pre(e);
            if (Text(WL(e), ref v)) { newValue = v; Post(e); return true; }
            Post(e);
            return false;
        }

        if (type == typeof(Vector2) && value is Vector2 v2)
        {
            Pre(e);
            var v = v2;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat2(WL(e), ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat2(WL(e), ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec2(WL(e), ref v);
            if (ch) { newValue = v; Post(e); return true; }
            Post(e);
            return false;
        }
        if (type == typeof(Vector3) && value is Vector3 v3)
        {
            Pre(e);
            var v = v3;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat3(WL(e), ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat3(WL(e), ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec3(WL(e), ref v);
            if (ch) { newValue = v; Post(e); return true; }
            Post(e);
            return false;
        }
        if (type == typeof(Vector4) && value is Vector4 v4)
        {
            Pre(e);
            var v = v4;
            bool ch;
            if (e.VecMins != null && e.VecMaxs != null)
                ch = ImGui.SliderFloat4(WL(e), ref v, e.VecMins[0], e.VecMaxs[0]);
            else if (e.HasRange)
                ch = ImGui.SliderFloat4(WL(e), ref v, e.RangeMin, e.RangeMax);
            else
                ch = Vec4(WL(e), ref v);
            if (ch) { newValue = v; Post(e); return true; }
            Post(e);
            return false;
        }

        if (value != null && type.IsGenericType && type.Name.StartsWith("ValueTuple`"))
        {
            var args = type.GetGenericArguments();
            if (args is [Type t1, Type t2] && IsNumeric(t1) && IsNumeric(t2))
            {
                var x = Convert.ToSingle(type.GetField("Item1")!.GetValue(value));
                var y = Convert.ToSingle(type.GetField("Item2")!.GetValue(value));
                var v = new Vector2(x, y);
                Pre(e);
                if (Vec2(WL(e), ref v))
                {
                    type.GetField("Item1")!.SetValue(value, Convert.ChangeType(v.X, t1));
                    type.GetField("Item2")!.SetValue(value, Convert.ChangeType(v.Y, t2));
                    newValue = value; Post(e); return true;
                }
                Post(e);
                return false;
            }
            if (args is [Type t1b, Type t2b, Type t3b] && IsNumeric(t1b) && IsNumeric(t2b) && IsNumeric(t3b))
            {
                var x = Convert.ToSingle(type.GetField("Item1")!.GetValue(value));
                var y = Convert.ToSingle(type.GetField("Item2")!.GetValue(value));
                var z = Convert.ToSingle(type.GetField("Item3")!.GetValue(value));
                var v = new Vector3(x, y, z);
                Pre(e);
                if (Vec3(WL(e), ref v)) { newValue = value; Post(e); return true; }
                Post(e);
                return false;
            }
        }

        if (CustomDrawers.TryGetValue(type, out var d)) { d.DynamicInvoke(value); return false; }

        if (value is IModSettingCustomDraw cd)
        {
            if (ImGui.TreeNode(e.Label)) { Draw(value); ImGui.Separator(); cd.DrawInspector(); ImGui.TreePop(); }
            return type.IsValueType;
        }

        if (value != null && (value is System.Collections.IEnumerable && !(value is string)))
        {
            return DrawJsonEditor(e, type, value, out newValue);
        }

        if (!type.IsPrimitive && type != typeof(string) && !type.IsEnum
            && type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(Vector4)
            && value != null && !type.IsGenericType)
        {
            if (ImGui.TreeNode(e.Label)) { Draw(value); ImGui.TreePop(); }
            return type.IsValueType;
        }

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

    private static readonly ConcurrentDictionary<string, string> JsonEditCache = new();

    private static bool DrawJsonEditor(Entry e, Type type, object value, out object? newValue)
    {
        newValue = value;
        var key = $"{type.FullName}_{e.Label}";

        if (!ImGui.TreeNode(e.Label)) return type.IsValueType;

        var currentJson = System.Text.Json.JsonSerializer.Serialize(value,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var editText = JsonEditCache.GetOrAdd(key, currentJson);

        if (editText != currentJson && !ImGui.IsItemActive())
            editText = JsonEditCache[key] = currentJson;

        var lines = Math.Max(3, editText.Split('\n').Length);
        var width = Math.Max(300, ImGui.GetContentRegionAvail().X - 20);
        var changed = ImGui.InputTextMultiline($"##json_{key}", ref editText, 65536,
            new Vector2(width, ImGui.GetTextLineHeight() * Math.Min(lines, 12)));

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
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), L10n.Get("Inspector_DeserializeNull"));
            }
            catch (Exception ex)
            {
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), L10n.Get("Inspector_JsonError", ex.Message));
            }
        }

        ImGui.TreePop();
        return type.IsValueType;
    }

    /// <summary>标签相对于控件的摆放位置</summary>
    public enum LabelSide
    {
        /// <summary>标签在上方</summary>
        Top,
        /// <summary>标签在左侧</summary>
        Left,
        /// <summary>标签在右侧</summary>
        Right
    }
}
