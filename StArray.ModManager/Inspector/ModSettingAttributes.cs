namespace StArray.ModManager.Inspector;

/// <summary>
/// 标记字段不在自动检查器面板中显示（类似 Unity 的 HideInInspector）
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingIgnoreAttribute : Attribute
{
}

/// <summary>
/// 标记字段以指定显示名称
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingLabelAttribute : Attribute
{
    /// <summary>显示名称</summary>
    public string Label { get; }
    /// <summary>指定字段显示名称</summary>
    public ModSettingLabelAttribute(string label) => Label = label;
}

/// <summary>
/// 标记 int/float/Vec 字段以指定范围。Vec2 传 4 个值 (xMin,xMax, yMin,yMax)，Vec3 传 6 个，Vec4 传 8 个
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ModSettingRangeAttribute : Attribute
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
public class ModSettingLabelSideAttribute : Attribute
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
public class ModSettingJsonAttribute : Attribute
{
    /// <summary>编辑器行数</summary>
    public int Lines { get; set; } = 6;
}
