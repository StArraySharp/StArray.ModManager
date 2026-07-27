using System.Text;
using System.Text.Json.Serialization;
using ImGuiNET;

namespace StArray.ModManager.Inspector;

/// <summary>
/// 可配置的快捷键：一个主键加若干修饰键。
/// 检查器会渲染成一个「点击后按键捕获」的按钮，可直接作为设置字段持久化。
/// </summary>
public struct Hotkey : IEquatable<Hotkey>
{
    /// <summary>主键；<see cref="ImGuiKey.None"/> 表示未绑定</summary>
    public ImGuiKey Key;
    /// <summary>需要按住 Ctrl</summary>
    public bool Ctrl;
    /// <summary>需要按住 Shift</summary>
    public bool Shift;
    /// <summary>需要按住 Alt</summary>
    public bool Alt;

    /// <summary>指定主键与修饰键</summary>
    public Hotkey(ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false)
    {
        Key = key; Ctrl = ctrl; Shift = shift; Alt = alt;
    }

    /// <summary>是否已绑定按键</summary>
    [JsonIgnore]
    public readonly bool IsBound => Key != ImGuiKey.None;

    /// <summary>本帧是否刚被按下（含修饰键判定）</summary>
    public readonly bool IsPressed()
    {
        if (!IsBound) return false;
        var io = ImGui.GetIO();
        if (Ctrl != io.KeyCtrl || Shift != io.KeyShift || Alt != io.KeyAlt) return false;
        return ImGui.IsKeyPressed(Key, false);
    }

    /// <summary>是否处于按住状态（含修饰键判定）</summary>
    public readonly bool IsDown()
    {
        if (!IsBound) return false;
        var io = ImGui.GetIO();
        if (Ctrl != io.KeyCtrl || Shift != io.KeyShift || Alt != io.KeyAlt) return false;
        return ImGui.IsKeyDown(Key);
    }

    /// <summary>形如 "Ctrl+Shift+F5" 的可读文本</summary>
    public override readonly string ToString()
    {
        if (!IsBound) return "—";
        var sb = new StringBuilder();
        if (Ctrl) sb.Append("Ctrl+");
        if (Shift) sb.Append("Shift+");
        if (Alt) sb.Append("Alt+");
        sb.Append(KeyName(Key));
        return sb.ToString();
    }

    private static string KeyName(ImGuiKey key)
    {
        var n = key.ToString();
        // ImGuiKey 的成员名带前缀，去掉后更接近键盘印字
        if (n.StartsWith("Keypad", StringComparison.Ordinal)) return "Num" + n[6..];
        return n;
    }

    /// <summary>修饰键之外的实体按键范围（跳过鼠标、手柄与修饰键本身）</summary>
    internal static IEnumerable<ImGuiKey> CapturableKeys()
    {
        for (var k = ImGuiKey.NamedKey_BEGIN; k < ImGuiKey.NamedKey_END; k++)
        {
            switch (k)
            {
                case ImGuiKey.LeftCtrl or ImGuiKey.RightCtrl:
                case ImGuiKey.LeftShift or ImGuiKey.RightShift:
                case ImGuiKey.LeftAlt or ImGuiKey.RightAlt:
                case ImGuiKey.LeftSuper or ImGuiKey.RightSuper:
                case ImGuiKey.ModCtrl or ImGuiKey.ModShift or ImGuiKey.ModAlt or ImGuiKey.ModSuper:
                    continue;
            }
            if (k >= ImGuiKey.MouseLeft && k <= ImGuiKey.MouseWheelY) continue;
            if (k >= ImGuiKey.GamepadStart && k <= ImGuiKey.GamepadRStickDown) continue;
            yield return k;
        }
    }

    /// <inheritdoc/>
    public readonly bool Equals(Hotkey other)
        => Key == other.Key && Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is Hotkey h && Equals(h);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => HashCode.Combine(Key, Ctrl, Shift, Alt);

    /// <summary>相等比较</summary>
    public static bool operator ==(Hotkey a, Hotkey b) => a.Equals(b);
    /// <summary>不等比较</summary>
    public static bool operator !=(Hotkey a, Hotkey b) => !a.Equals(b);
}
