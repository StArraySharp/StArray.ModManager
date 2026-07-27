using StArray.ModManager.Resources;
using System.Collections;
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

    /// <summary>包一层 Pre/Post，返回控件是否被修改。</summary>
    private static bool Framed(Entry e, Func<bool> draw)
    {
        Pre(e);
        var ch = draw();
        Post(e);
        return ch;
    }

    private static bool TryDrawField(Entry e, object? value, out object? newValue)
    {
        newValue = value;
        var type = e.ValueType;

        // ---- 颜色（优先于 Vector4 / uint 的默认绘制）----
        if (e.IsColor && TryDrawColor(e, value, out newValue)) return true;
        if (e.IsColor && (type == typeof(Vector4) || type == typeof(uint))) return false;

        // ---- 热键 ----
        if (type == typeof(Hotkey) && value is Hotkey hk)
        {
            var v = hk;
            if (Framed(e, () => DrawHotkey(WL(e), ref v))) { newValue = v; return true; }
            return false;
        }

        // ---- bool ----
        if (type == typeof(bool) && value is bool b)
        {
            var v = b;
            if (Framed(e, () => Bool(WL(e), ref v))) { newValue = v; return true; }
            return false;
        }

        // ---- 整数族：统一走 ImGui 的 scalar 控件，保持各自的位宽与符号 ----
        if (TryDrawIntegral(e, type, value, out newValue, out var handled)) return true;
        if (handled) return false;

        // ---- 浮点 ----
        if (type == typeof(float) && value is float f)
        {
            var v = f;
            if (Framed(e, () => e.HasRange ? SliderFloat(WL(e), ref v, e.RangeMin, e.RangeMax)
                                           : Float(WL(e), ref v))) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(double) && value is double dv)
        {
            var v = dv;
            if (Framed(e, () => e.HasRange
                    ? DragScalarRanged(WL(e), ImGuiDataType.Double, ref v, e.RangeMin, e.RangeMax)
                    : Double(WL(e), ref v))) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(decimal) && value is decimal dec)
        {
            // decimal 没有对应的 ImGui 标量类型，用 double 中转（超出范围时保留原值）
            var dd = (double)dec;
            if (Framed(e, () => Double(WL(e), ref dd)))
            {
                try { newValue = (decimal)dd; return true; }
                catch (OverflowException) { return false; }
            }
            return false;
        }

        if (type.IsEnum)
        {
            Pre(e);
            var v = value ?? Activator.CreateInstance(type);
            var r = TryDrawEnum(WL(e), type, v!, out newValue);
            Post(e);
            return r;
        }

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
            var v = v2;
            if (Framed(e, () => DrawVec(WL(e), e, ref v, 2))) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(Vector3) && value is Vector3 v3)
        {
            var v = v3;
            if (Framed(e, () => DrawVec(WL(e), e, ref v, 3))) { newValue = v; return true; }
            return false;
        }
        if (type == typeof(Vector4) && value is Vector4 v4)
        {
            var v = v4;
            if (Framed(e, () => DrawVec(WL(e), e, ref v, 4))) { newValue = v; return true; }
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

        // ---- 列表 / 数组：结构化编辑，失败再退回 JSON ----
        if (value is IList list && value is not string && TryDrawList(e, list, out newValue))
            return true;

        if (value != null && value is IEnumerable && value is not string)
            return DrawJsonEditor(e, type, value, out newValue);

        if (!type.IsPrimitive && type != typeof(string) && !type.IsEnum
            && type != typeof(Vector2) && type != typeof(Vector3) && type != typeof(Vector4)
            && value != null && !type.IsGenericType)
        {
            if (ImGui.TreeNode(e.Label)) { Draw(value); ImGui.TreePop(); }
            return type.IsValueType;
        }

        if (value != null && !type.IsPrimitive && type != typeof(string))
            return DrawJsonEditor(e, type, value, out newValue);

        ImGui.TextDisabled(value == null ? $"{e.Label}: null" : $"{e.Label}: {value}");
        return false;
    }

    // ── 整数族 ──

    /// <summary>
    /// 覆盖全部整数宽度与符号。之前只有 int/long 有分支，其余（byte/short/uint/…）
    /// 因为 IsPrimitive 为 true 绕过了所有兜底，最终落到只读文本。
    /// </summary>
    private static bool TryDrawIntegral(Entry e, Type type, object? value,
        out object? newValue, out bool handled)
    {
        newValue = value;
        handled = true;

        switch (value)
        {
            case int i:
            {
                var v = i;
                if (Framed(e, () => e.HasRange ? SliderInt(WL(e), ref v, (int)e.RangeMin, (int)e.RangeMax)
                                               : Int(WL(e), ref v))) { newValue = v; return true; }
                return false;
            }
            case long l:
            { var v = l; if (ScalarField(e, ImGuiDataType.S64, ref v)) { newValue = v; return true; } return false; }
            case byte by:
            { var v = by; if (ScalarField(e, ImGuiDataType.U8, ref v)) { newValue = v; return true; } return false; }
            case sbyte sb:
            { var v = sb; if (ScalarField(e, ImGuiDataType.S8, ref v)) { newValue = v; return true; } return false; }
            case short sh:
            { var v = sh; if (ScalarField(e, ImGuiDataType.S16, ref v)) { newValue = v; return true; } return false; }
            case ushort us:
            { var v = us; if (ScalarField(e, ImGuiDataType.U16, ref v)) { newValue = v; return true; } return false; }
            case uint ui:
            { var v = ui; if (ScalarField(e, ImGuiDataType.U32, ref v)) { newValue = v; return true; } return false; }
            case ulong ul:
            { var v = ul; if (ScalarField(e, ImGuiDataType.U64, ref v)) { newValue = v; return true; } return false; }
            case nint np:
            { var v = (long)np; if (ScalarField(e, ImGuiDataType.S64, ref v)) { newValue = (nint)v; return true; } return false; }
            case char c:
            {
                var str = c.ToString();
                if (Framed(e, () => ImGui.InputText(WL(e), ref str, 2)) && str.Length > 0)
                {
                    newValue = str[0];
                    return true;
                }
                return false;
            }
        }

        handled = false;
        return false;
    }

    /// <summary>
    /// 标量控件 + Pre/Post 包装。不能复用 <see cref="Framed"/>：C# 不允许在 lambda 内取局部变量地址。
    /// </summary>
    private static unsafe bool ScalarField<T>(Entry e, ImGuiDataType dt, ref T v) where T : unmanaged
    {
        Pre(e);
        bool ch;
        fixed (T* p = &v)
            ch = Scalar(WL(e), dt, p, e);
        Post(e);
        return ch;
    }

    /// <summary>有范围时用 Slider，否则用 Drag；两者都按目标类型的原生位宽操作。</summary>
    private static unsafe bool Scalar(string label, ImGuiDataType dt, void* p, Entry e)
    {
        if (!e.HasRange) return ImGui.DragScalar(label, dt, (nint)p, 1f);

        // 范围值以各自类型的形式落到栈上，交给 ImGui 逐类型比较
        switch (dt)
        {
            case ImGuiDataType.S64:
            { long lo = (long)e.RangeMin, hi = (long)e.RangeMax; return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.U64:
            { ulong lo = (ulong)Math.Max(0, e.RangeMin), hi = (ulong)Math.Max(0, e.RangeMax); return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.U32:
            { uint lo = (uint)Math.Max(0, e.RangeMin), hi = (uint)Math.Max(0, e.RangeMax); return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.S16:
            { short lo = (short)e.RangeMin, hi = (short)e.RangeMax; return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.U16:
            { ushort lo = (ushort)Math.Max(0, e.RangeMin), hi = (ushort)Math.Max(0, e.RangeMax); return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.S8:
            { sbyte lo = (sbyte)e.RangeMin, hi = (sbyte)e.RangeMax; return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            case ImGuiDataType.U8:
            { byte lo = (byte)Math.Max(0, e.RangeMin), hi = (byte)Math.Max(0, e.RangeMax); return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi)); }
            default:
                return ImGui.DragScalar(label, dt, (nint)p, 1f);
        }
    }

    private static unsafe bool DragScalarRanged(string label, ImGuiDataType dt, ref double v, float min, float max)
    {
        double lo = min, hi = max;
        fixed (double* p = &v)
            return ImGui.SliderScalar(label, dt, (nint)p, (nint)(&lo), (nint)(&hi));
    }

    // ── 向量：支持逐分量范围 ──

    /// <summary>
    /// Vec 绘制。带多分量范围时拆成逐分量滑块 —— ImGui 的 SliderFloatN 只接受统一范围，
    /// 之前的实现只用了第一个分量的上下限，其余被静默丢弃。
    /// </summary>
    private static bool DrawVec<T>(string label, Entry e, ref T v, int n) where T : struct
    {
        Span<float> c = stackalloc float[4];
        ReadVec(v, c, n);

        bool changed;
        if (e.VecMins is { } mins && e.VecMaxs is { } maxs && mins.Length >= n && maxs.Length >= n)
        {
            changed = false;
            ImGui.BeginGroup();
            var w = ImGui.CalcItemWidth();
            var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
            var each = Math.Max(30f, (w - spacing * (n - 1)) / n);
            for (var i = 0; i < n; i++)
            {
                if (i > 0) ImGui.SameLine(0, spacing);
                ImGui.SetNextItemWidth(each);
                var f = c[i];
                if (ImGui.SliderFloat($"{label}_{i}", ref f, mins[i], maxs[i]))
                {
                    c[i] = f;
                    changed = true;
                }
            }
            ImGui.EndGroup();
        }
        else if (e.HasRange)
        {
            changed = n switch
            {
                2 => SliderVec2(label, c, e.RangeMin, e.RangeMax),
                3 => SliderVec3(label, c, e.RangeMin, e.RangeMax),
                _ => SliderVec4(label, c, e.RangeMin, e.RangeMax),
            };
        }
        else
        {
            changed = n switch
            {
                2 => DragVec2(label, c),
                3 => DragVec3(label, c),
                _ => DragVec4(label, c),
            };
        }

        if (changed) v = WriteVec<T>(c, n);
        return changed;
    }

    private static void ReadVec<T>(T v, Span<float> c, int n) where T : struct
    {
        switch (v)
        {
            case Vector2 a: c[0] = a.X; c[1] = a.Y; break;
            case Vector3 a: c[0] = a.X; c[1] = a.Y; c[2] = a.Z; break;
            case Vector4 a: c[0] = a.X; c[1] = a.Y; c[2] = a.Z; c[3] = a.W; break;
        }
    }

    private static T WriteVec<T>(ReadOnlySpan<float> c, int n) where T : struct
    {
        object o = n switch
        {
            2 => new Vector2(c[0], c[1]),
            3 => new Vector3(c[0], c[1], c[2]),
            _ => new Vector4(c[0], c[1], c[2], c[3]),
        };
        return (T)o;
    }

    private static bool DragVec2(string l, Span<float> c) { var v = new Vector2(c[0], c[1]); if (!Vec2(l, ref v)) return false; c[0] = v.X; c[1] = v.Y; return true; }
    private static bool DragVec3(string l, Span<float> c) { var v = new Vector3(c[0], c[1], c[2]); if (!Vec3(l, ref v)) return false; c[0] = v.X; c[1] = v.Y; c[2] = v.Z; return true; }
    private static bool DragVec4(string l, Span<float> c) { var v = new Vector4(c[0], c[1], c[2], c[3]); if (!Vec4(l, ref v)) return false; c[0] = v.X; c[1] = v.Y; c[2] = v.Z; c[3] = v.W; return true; }
    private static bool SliderVec2(string l, Span<float> c, float lo, float hi) { var v = new Vector2(c[0], c[1]); if (!ImGui.SliderFloat2(l, ref v, lo, hi)) return false; c[0] = v.X; c[1] = v.Y; return true; }
    private static bool SliderVec3(string l, Span<float> c, float lo, float hi) { var v = new Vector3(c[0], c[1], c[2]); if (!ImGui.SliderFloat3(l, ref v, lo, hi)) return false; c[0] = v.X; c[1] = v.Y; c[2] = v.Z; return true; }
    private static bool SliderVec4(string l, Span<float> c, float lo, float hi) { var v = new Vector4(c[0], c[1], c[2], c[3]); if (!ImGui.SliderFloat4(l, ref v, lo, hi)) return false; c[0] = v.X; c[1] = v.Y; c[2] = v.Z; c[3] = v.W; return true; }

    // ── 颜色 ──

    private static bool TryDrawColor(Entry e, object? value, out object? newValue)
    {
        newValue = value;

        if (value is Vector4 col)
        {
            var v = col;
            var changed = Framed(e, () => e.ColorPicker
                ? ImGui.ColorPicker4(WL(e), ref v, e.ColorAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha)
                : ImGui.ColorEdit4(WL(e), ref v, e.ColorAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha));
            if (changed) { newValue = v; return true; }
            return false;
        }

        if (value is uint packed)
        {
            // ImGui draw list 用的是 packed ABGR
            var v = ImGui.ColorConvertU32ToFloat4(packed);
            var changed = Framed(e, () => e.ColorPicker
                ? ImGui.ColorPicker4(WL(e), ref v, e.ColorAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha)
                : ImGui.ColorEdit4(WL(e), ref v, e.ColorAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha));
            if (changed) { newValue = ImGui.ColorConvertFloat4ToU32(v); return true; }
            return false;
        }

        return false;
    }

    // ── 热键 ──

    private static readonly HashSet<string> CapturingHotkeys = new(StringComparer.Ordinal);

    private static bool DrawHotkey(string label, ref Hotkey hk)
    {
        var capturing = CapturingHotkeys.Contains(label);
        var text = capturing ? L10n.Get("Inspector_HotkeyPress") : hk.ToString();

        if (ImGui.Button($"{text}{label}", new Vector2(ImGui.CalcItemWidth(), 0)))
        {
            if (capturing) CapturingHotkeys.Remove(label);
            else CapturingHotkeys.Add(label);
            return false;
        }

        if (!capturing) return false;

        // Esc 取消，Backspace 解绑
        if (ImGui.IsKeyPressed(ImGuiKey.Escape, false))
        {
            CapturingHotkeys.Remove(label);
            return false;
        }
        if (ImGui.IsKeyPressed(ImGuiKey.Backspace, false))
        {
            CapturingHotkeys.Remove(label);
            hk = new Hotkey(ImGuiKey.None);
            return true;
        }

        foreach (var k in Hotkey.CapturableKeys())
        {
            if (!ImGui.IsKeyPressed(k, false)) continue;
            var io = ImGui.GetIO();
            hk = new Hotkey(k, io.KeyCtrl, io.KeyShift, io.KeyAlt);
            CapturingHotkeys.Remove(label);
            return true;
        }
        return false;
    }

    // ── 列表 ──

    /// <summary>
    /// List&lt;T&gt; / T[] 的结构化编辑：每项一行，带删除与追加。
    /// 仅在元素类型能用现有控件绘制时接管，否则交回 JSON 编辑器。
    /// </summary>
    private static bool TryDrawList(Entry e, IList list, out object? newValue)
    {
        newValue = list;

        var elemType = list.GetType().IsArray
            ? list.GetType().GetElementType()!
            : list.GetType().IsGenericType
                ? list.GetType().GetGenericArguments()[0]
                : typeof(object);

        if (!IsSimpleElement(elemType)) return false;

        if (!ImGui.TreeNode($"{e.Label} [{list.Count}]{WL(e)}")) return false;

        var changed = false;
        var removeAt = -1;

        for (var i = 0; i < list.Count; i++)
        {
            ImGui.PushID(i);
            ImGui.SetNextItemWidth(Math.Max(80, ImGui.GetContentRegionAvail().X - 60));

            var item = new Entry(e.Name, $"[{i}]", elemType, _ => null, null,
                false, false, false, 0, 0, 0, 0, false, null, null, 0, LabelSide.Right,
                null, null, true, null, false, false, true, false);

            if (TryDrawField(item, list[i], out var nv) && !list.IsReadOnly)
            {
                list[i] = nv;
                changed = true;
            }

            ImGui.SameLine();
            if (!list.IsFixedSize && ImGui.SmallButton("×")) removeAt = i;
            ImGui.PopID();
        }

        if (removeAt >= 0 && !list.IsFixedSize)
        {
            list.RemoveAt(removeAt);
            changed = true;
        }

        if (!list.IsFixedSize && ImGui.SmallButton($"+ {L10n.Get("Inspector_ListAdd")}"))
        {
            list.Add(elemType == typeof(string) ? "" : Activator.CreateInstance(elemType));
            changed = true;
        }

        ImGui.TreePop();
        return changed;
    }

    private static bool IsSimpleElement(Type t) =>
        t == typeof(string) || t == typeof(bool) || t.IsEnum || IsNumeric(t)
        || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4);

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
        var key = $"{type.FullName}_{e.Name}_{e.Label}";

        if (!ImGui.TreeNode(e.Label)) return type.IsValueType;

        var currentJson = System.Text.Json.JsonSerializer.Serialize(value, SettingsJson);
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
                var deserialized = System.Text.Json.JsonSerializer.Deserialize(editText, type, SettingsJson);
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

    /// <summary>
    /// Mod 设置用的序列化选项。走反射而非源生成上下文：设置字段的类型由各 mod 自由决定，
    /// 源生成无法预先覆盖（当前上下文只有 bool/float/string 等少数类型，遇到 int、
    /// Vector3、枚举会直接抛 "Metadata for type ... was not provided"）。
    /// 本项目全程依赖 JIT（Reflection.Emit / Expression / Assembly.LoadFrom），不受 AOT 约束。
    /// </summary>
    public static readonly System.Text.Json.JsonSerializerOptions SettingsJson = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

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
