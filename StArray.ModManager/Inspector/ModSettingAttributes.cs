namespace StArray.ModManager.Inspector;

/// <summary>
/// 所有检查器特性的基类。带有任意一个此类特性的成员即视为「被标记」，
/// 会出现在检查器面板中，并参与设置的保存 / 读取 —— 无论其访问修饰符
/// （private / protected / internal 字段与属性同样有效）。
/// 未标记的成员一律忽略。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ModSettingAttributeBase : Attribute
{
}

/// <summary>
/// 显式把成员纳入检查器与设置持久化。
/// 只需要默认外观时用它即可；要改标签用 <see cref="ModSettingLabelAttribute"/>。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingAttribute : ModSettingAttributeBase
{
}

/// <summary>
/// 标记字段不在自动检查器面板中显示（类似 Unity 的 HideInInspector）。
/// 优先级最高：即使带有其它标记也会被排除，且不参与持久化。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingIgnoreAttribute : Attribute
{
}

/// <summary>
/// 标记字段以指定显示名称
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingLabelAttribute : ModSettingAttributeBase
{
    /// <summary>显示名称</summary>
    public string Label { get; }
    /// <summary>指定字段显示名称</summary>
    public ModSettingLabelAttribute(string label) => Label = label;
}

/// <summary>
/// 标记 int/float/Vec 字段以指定范围。Vec2 传 4 个值 (xMin,xMax, yMin,yMax)，Vec3 传 6 个，Vec4 传 8 个。
/// 多分量模式下每个分量会各自渲染成一条滑块。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingRangeAttribute : ModSettingAttributeBase
{
    /// <summary>范围最小值</summary>
    public float Min { get; }
    /// <summary>范围最大值</summary>
    public float Max { get; }
    /// <summary>最小值数组（Vec 多分量模式）</summary>
    public float[]? Mins { get; }
    /// <summary>最大值数组（Vec 多分量模式）</summary>
    public float[]? Maxs { get; }

    /// <summary>单值范围 (int/float)</summary>
    public ModSettingRangeAttribute(float min, float max) { Min = min; Max = max; }

    /// <summary>Vec2 范围 (xMin, xMax, yMin, yMax)</summary>
    public ModSettingRangeAttribute(float xMin, float xMax, float yMin, float yMax)
    { Mins = new[] { xMin, yMin }; Maxs = new[] { xMax, yMax }; }

    /// <summary>Vec3 范围 (xMin, xMax, yMin, yMax, zMin, zMax)</summary>
    public ModSettingRangeAttribute(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
    { Mins = new[] { xMin, yMin, zMin }; Maxs = new[] { xMax, yMax, zMax }; }

    /// <summary>Vec4 范围 (xMin, xMax, yMin, yMax, zMin, zMax, wMin, wMax)</summary>
    public ModSettingRangeAttribute(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax, float wMin, float wMax)
    { Mins = new[] { xMin, yMin, zMin, wMin }; Maxs = new[] { xMax, yMax, zMax, wMax }; }
}

/// <summary>
/// 标记字段标签位置
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingLabelSideAttribute : ModSettingAttributeBase
{
    /// <summary>标签位置</summary>
    public ModInspector.LabelSide Side { get; }
    /// <summary>指定字段标签位置</summary>
    public ModSettingLabelSideAttribute(ModInspector.LabelSide side) => Side = side;
}

/// <summary>
/// 标记 string 字段为 JSON 内容，检查器使用多行编辑器
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingJsonAttribute : ModSettingAttributeBase
{
    /// <summary>编辑器行数</summary>
    public int Lines { get; set; } = 6;
}

/// <summary>
/// 鼠标悬停时显示的说明文字。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingTooltipAttribute : ModSettingAttributeBase
{
    /// <summary>说明文字</summary>
    public string Text { get; }
    /// <summary>指定悬停说明</summary>
    public ModSettingTooltipAttribute(string text) => Text = text;
}

/// <summary>
/// 在此成员之前插入一个可折叠分组标题。后续成员归入该分组，直到下一个标题。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingHeaderAttribute : ModSettingAttributeBase
{
    /// <summary>分组标题</summary>
    public string Title { get; }
    /// <summary>分组默认是否展开</summary>
    public bool DefaultOpen { get; set; } = true;
    /// <summary>指定分组标题</summary>
    public ModSettingHeaderAttribute(string title) => Title = title;
}

/// <summary>
/// 依赖另一个 bool 成员：为 false 时本项变灰不可编辑（而非隐藏，避免布局跳动）。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingShowIfAttribute : ModSettingAttributeBase
{
    /// <summary>被依赖成员的名称，建议用 nameof</summary>
    public string Member { get; }
    /// <summary>取反：被依赖成员为 true 时才禁用</summary>
    public bool Invert { get; set; }
    /// <summary>指定依赖成员</summary>
    public ModSettingShowIfAttribute(string member) => Member = member;
}

/// <summary>
/// 把 Vector4 / uint 当作颜色编辑。
/// Vector4 按 RGBA 各分量 0~1；uint 按 ImGui 的 packed ABGR（与 draw list 一致）。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingColorAttribute : ModSettingAttributeBase
{
    /// <summary>是否包含 Alpha 通道</summary>
    public bool Alpha { get; set; } = true;
    /// <summary>使用色轮选择器而非行内色块</summary>
    public bool Picker { get; set; }
}

/// <summary>
/// 只读展示：绘制但不可编辑，适合展示运行时状态。仍会参与持久化。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingReadOnlyAttribute : ModSettingAttributeBase
{
}

/// <summary>
/// 指定排序权重，小的在前。未标记者视为 0，同权重按声明顺序。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingOrderAttribute : ModSettingAttributeBase
{
    /// <summary>排序权重</summary>
    public int Order { get; }
    /// <summary>指定排序权重</summary>
    public ModSettingOrderAttribute(int order) => Order = order;
}

/// <summary>
/// 排除出持久化：显示在检查器里，但不写入 settings.json。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ModSettingNoSaveAttribute : ModSettingAttributeBase
{
}
