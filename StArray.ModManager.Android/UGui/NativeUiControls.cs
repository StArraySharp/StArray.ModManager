using System.Numerics;

namespace StArray.ModManager.Android.UGui;

[UiType("UnityEngine.CoreModule", "UnityEngine", "Vector2")]
public struct UiVector2(float x, float y)
{
    public float X = x;
    public float Y = y;
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Vector3")]
public struct UiVector3(float x, float y, float z)
{
    public float X = x;
    public float Y = y;
    public float Z = z;
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Color")]
public struct UiColor(float r, float g, float b, float a = 1)
{
    public float R = r;
    public float G = g;
    public float B = b;
    public float A = a;

    public static implicit operator UiColor(Vector4 value) => new(value.X, value.Y, value.Z, value.W);
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Component")]
public abstract class UiComponent(nint pointer) : NativeUiObject(pointer)
{
    private static readonly UiMethod GameObjectGetter = new(typeof(UiComponent), "get_gameObject", true);
    private static readonly UiMethod TransformGetter = new(typeof(UiComponent), "get_transform", true);
    private static readonly UiMethod GetComponentMethod = new(typeof(UiComponent), "GetComponent", false, "System.Type");

    public UiGameObject GameObject => NativeUi.Wrap<UiGameObject>(GameObjectGetter.Invoke(this))!;
    public UiTransform Transform => NativeUi.Wrap<UiTransform>(TransformGetter.Invoke(this))!;

    public T? GetComponent<T>() where T : UiComponent
        => NativeUi.Wrap<T>(GetComponentMethod.Invoke(this, UiArg.Type(typeof(T))));
}

internal sealed class UiEvent(nint pointer) : NativeUiObject(pointer);

[UiType("UnityEngine.CoreModule", "UnityEngine", "GameObject")]
public sealed class UiGameObject(nint pointer) : NativeUiObject(pointer)
{
    private static readonly UiMethod CreateMethod = new(typeof(UiGameObject), ".ctor", false, "System.String");
    private static readonly UiMethod CreateComponentsMethod = new(typeof(UiGameObject), ".ctor", false,
        "System.String", "System.Type[]");
    private static readonly UiMethod AddMethod = new(typeof(UiGameObject), "AddComponent", false, "System.Type");
    private static readonly UiMethod GetMethod = new(typeof(UiGameObject), "GetComponent", false, "System.Type");
    private static readonly UiMethod ActiveSetter = new(typeof(UiGameObject), "SetActive", false, "System.Boolean");
    private static readonly UiMethod FindMethod = new(typeof(UiGameObject), "Find", false, "System.String");
    private static readonly UiMethod TransformGetter = new(typeof(UiGameObject), "get_transform", true);
    private static readonly UiMethod DestroyMethod = new(typeof(UiObject), "Destroy", false, "UnityEngine.Object");

    public static UiGameObject Create(string name) => NativeUi.Wrap<UiGameObject>(CreateMethod.Create(UiArg.String(name)))!;
    public static UiGameObject Create(string name, params Type[] components)
        => NativeUi.Wrap<UiGameObject>(CreateComponentsMethod.Create(UiArg.String(name), UiArg.Types(components)))!;
    public static UiGameObject CreateRect(string name, UiTransform? parent = null)
    {
        var gameObject = Create(name, typeof(UiRectTransform));
        if (parent != null) gameObject.Transform.SetParent(parent, false);
        return gameObject;
    }
    public static UiGameObject? Find(string name) => NativeUi.Wrap<UiGameObject>(FindMethod.Invoke(null, [UiArg.String(name)]));
    public void SetActive(bool active) => ActiveSetter.Invoke(this, [UiArg.Value(active)]);
    public UiTransform Transform => NativeUi.Wrap<UiTransform>(TransformGetter.Invoke(this))!;
    public void Destroy() => DestroyMethod.Invoke(null, [UiArg.Reference(this)]);

    public T? GetComponent<T>() where T : UiComponent
        => NativeUi.Wrap<T>(GetMethod.Invoke(this, [UiArg.Type(typeof(T))]));

    public T AddComponent<T>() where T : UiComponent
    {
        var component = NativeUi.Wrap<T>(AddMethod.Invoke(this, [UiArg.Type(typeof(T))]));
        return component ?? throw new InvalidOperationException($"Unity failed to add {typeof(T).Name}.");
    }
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Transform")]
public class UiTransform(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ParentSetter = new(typeof(UiTransform), "SetParent", true,
        "UnityEngine.Transform", "System.Boolean");
    private static readonly UiMethod PositionGetter = new(typeof(UiTransform), "get_localPosition", true);
    private static readonly UiMethod PositionSetter = new(typeof(UiTransform), "set_localPosition", true, "UnityEngine.Vector3");
    private static readonly UiMethod ScaleGetter = new(typeof(UiTransform), "get_localScale", true);
    private static readonly UiMethod ScaleSetter = new(typeof(UiTransform), "set_localScale", true, "UnityEngine.Vector3");

    public UiVector3 LocalPosition
    {
        get => NativeUi.Unbox<UiVector3>(PositionGetter.Invoke(this));
        set => PositionSetter.Invoke(this, [UiArg.Value(value)]);
    }

    public UiVector3 LocalScale
    {
        get => NativeUi.Unbox<UiVector3>(ScaleGetter.Invoke(this));
        set => ScaleSetter.Invoke(this, [UiArg.Value(value)]);
    }

    public void SetParent(UiTransform? parent, bool worldPositionStays = false)
        => ParentSetter.Invoke(this, [UiArg.Reference(parent), UiArg.Value(worldPositionStays)]);
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "RectTransform")]
public sealed class UiRectTransform(nint pointer) : UiTransform(pointer)
{
    private static readonly UiMethod AnchorMinGet = new(typeof(UiRectTransform), "get_anchorMin", true);
    private static readonly UiMethod AnchorMinSet = new(typeof(UiRectTransform), "set_anchorMin", true, "UnityEngine.Vector2");
    private static readonly UiMethod AnchorMaxGet = new(typeof(UiRectTransform), "get_anchorMax", true);
    private static readonly UiMethod AnchorMaxSet = new(typeof(UiRectTransform), "set_anchorMax", true, "UnityEngine.Vector2");
    private static readonly UiMethod PivotGet = new(typeof(UiRectTransform), "get_pivot", true);
    private static readonly UiMethod PivotSet = new(typeof(UiRectTransform), "set_pivot", true, "UnityEngine.Vector2");
    private static readonly UiMethod SizeGet = new(typeof(UiRectTransform), "get_sizeDelta", true);
    private static readonly UiMethod SizeSet = new(typeof(UiRectTransform), "set_sizeDelta", true, "UnityEngine.Vector2");
    private static readonly UiMethod AnchoredGet = new(typeof(UiRectTransform), "get_anchoredPosition", true);
    private static readonly UiMethod AnchoredSet = new(typeof(UiRectTransform), "set_anchoredPosition", true, "UnityEngine.Vector2");

    public UiVector2 AnchorMin { get => NativeUi.Unbox<UiVector2>(AnchorMinGet.Invoke(this)); set => AnchorMinSet.Invoke(this, [UiArg.Value(value)]); }
    public UiVector2 AnchorMax { get => NativeUi.Unbox<UiVector2>(AnchorMaxGet.Invoke(this)); set => AnchorMaxSet.Invoke(this, [UiArg.Value(value)]); }
    public UiVector2 Pivot { get => NativeUi.Unbox<UiVector2>(PivotGet.Invoke(this)); set => PivotSet.Invoke(this, [UiArg.Value(value)]); }
    public UiVector2 SizeDelta { get => NativeUi.Unbox<UiVector2>(SizeGet.Invoke(this)); set => SizeSet.Invoke(this, [UiArg.Value(value)]); }
    public UiVector2 AnchoredPosition { get => NativeUi.Unbox<UiVector2>(AnchoredGet.Invoke(this)); set => AnchoredSet.Invoke(this, [UiArg.Value(value)]); }
}

[UiType("UnityEngine.UIModule", "UnityEngine", "Canvas")]
public sealed class UiCanvas(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod RenderModeSet = new(typeof(UiCanvas), "set_renderMode", true, "UnityEngine.RenderMode");
    private static readonly UiMethod SortOrderSet = new(typeof(UiCanvas), "set_sortingOrder", true, "System.Int32");

    public void SetScreenSpaceOverlay() => RenderModeSet.Invoke(this, [UiArg.Value(0)]);
    public int SortingOrder { set => SortOrderSet.Invoke(this, [UiArg.Value(value)]); }

    public static UiCanvas CreateOverlay(string name, int sortingOrder = 100)
    {
        using var scope = NativeUiSession.RequireCurrent().Enter();
        var root = UiGameObject.Create(name, typeof(UiRectTransform), typeof(UiCanvas),
            typeof(UiCanvasScaler), typeof(UiGraphicRaycaster));
        var canvas = root.GetComponent<UiCanvas>()
            ?? throw new InvalidOperationException("Unity did not create a Canvas component.");
        canvas.SetScreenSpaceOverlay();
        canvas.SortingOrder = sortingOrder;
        return canvas;
    }
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Object")]
public sealed class UiObject(nint pointer) : NativeUiObject(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "CanvasScaler")]
public sealed class UiCanvasScaler(nint pointer) : UiComponent(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "GraphicRaycaster")]
public sealed class UiGraphicRaycaster(nint pointer) : UiComponent(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "Image")]
public sealed class UiImage(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ColorGet = new(typeof(UiImage), "get_color", true);
    private static readonly UiMethod ColorSet = new(typeof(UiImage), "set_color", true, "UnityEngine.Color");
    public UiColor Color { get => NativeUi.Unbox<UiColor>(ColorGet.Invoke(this)); set => ColorSet.Invoke(this, [UiArg.Value(value)]); }
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "Text")]
public sealed class UiText(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod TextGet = new(typeof(UiText), "get_text", true);
    private static readonly UiMethod TextSet = new(typeof(UiText), "set_text", true, "System.String");
    private static readonly UiMethod ColorSet = new(typeof(UiText), "set_color", true, "UnityEngine.Color");
    private static readonly UiMethod FontSizeSet = new(typeof(UiText), "set_fontSize", true, "System.Int32");
    private static readonly UiMethod AlignmentSet = new(typeof(UiText), "set_alignment", true, "UnityEngine.TextAnchor");
    private static readonly UiMethod FontSet = new(typeof(UiText), "set_font", true, "UnityEngine.Font");

    public string Text { get => NativeUi.ReadString(TextGet.Invoke(this)) ?? ""; set => TextSet.Invoke(this, [UiArg.String(value)]); }
    public UiColor Color { set => ColorSet.Invoke(this, [UiArg.Value(value)]); }
    public int FontSize { set => FontSizeSet.Invoke(this, [UiArg.Value(value)]); }
    public int Alignment { set => AlignmentSet.Invoke(this, [UiArg.Value(value)]); }
    public UiFont? Font { set => FontSet.Invoke(this, [UiArg.Reference(value)]); }
}

[UiType("UnityEngine.TextRenderingModule", "UnityEngine", "Font")]
public sealed class UiFont(nint pointer) : NativeUiObject(pointer)
{
    private static readonly UiMethod BuiltinMethod = new(typeof(UiResources), "GetBuiltinResource", false,
        "System.Type", "System.String");

    public static UiFont LoadBuiltin(string name = "LegacyRuntime.ttf")
        => NativeUi.Wrap<UiFont>(BuiltinMethod.Invoke(null, [UiArg.Type(typeof(UiFont)), UiArg.String(name)]))
            ?? throw new InvalidOperationException($"Unity builtin font '{name}' was not found.");
}

[UiType("UnityEngine.CoreModule", "UnityEngine", "Resources")]
public sealed class UiResources(nint pointer) : NativeUiObject(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "Button")]
public sealed class UiButton(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ClickGet = new(typeof(UiButton), "get_onClick", true);
    public void OnClick(Action callback)
    {
        var click = new UiEvent(ClickGet.Invoke(this));
        NativeUiSession.Listen(this, click, callback);
    }
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "Toggle")]
public sealed class UiToggle(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ValueGet = new(typeof(UiToggle), "get_isOn", true);
    private static readonly UiMethod ValueSet = new(typeof(UiToggle), "set_isOn", true, "System.Boolean");
    public bool IsOn { get => NativeUi.Unbox<bool>(ValueGet.Invoke(this)); set => ValueSet.Invoke(this, [UiArg.Value(value)]); }
    public void OnValueChanged(Action<bool> callback)
        => NativeUiSession.Listen(this, new UiEvent(NativeUiSession.ReadReferenceField(this, typeof(UiToggle), "onValueChanged")), callback);
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "Slider")]
public sealed class UiSlider(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ValueGet = new(typeof(UiSlider), "get_value", true);
    private static readonly UiMethod ValueSet = new(typeof(UiSlider), "set_value", true, "System.Single");
    private static readonly UiMethod MinSet = new(typeof(UiSlider), "set_minValue", true, "System.Single");
    private static readonly UiMethod MaxSet = new(typeof(UiSlider), "set_maxValue", true, "System.Single");
    private static readonly UiMethod ChangedGet = new(typeof(UiSlider), "get_onValueChanged", true);
    public float Value { get => NativeUi.Unbox<float>(ValueGet.Invoke(this)); set => ValueSet.Invoke(this, [UiArg.Value(value)]); }
    public float MinValue { set => MinSet.Invoke(this, [UiArg.Value(value)]); }
    public float MaxValue { set => MaxSet.Invoke(this, [UiArg.Value(value)]); }
    public void OnValueChanged(Action<float> callback)
        => NativeUiSession.Listen(this, new UiEvent(ChangedGet.Invoke(this)), callback);
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "InputField")]
public sealed class UiInputField(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod TextGet = new(typeof(UiInputField), "get_text", true);
    private static readonly UiMethod TextSet = new(typeof(UiInputField), "set_text", true, "System.String");
    private static readonly UiMethod ChangedGet = new(typeof(UiInputField), "get_onValueChanged", true);
    public string Text { get => NativeUi.ReadString(TextGet.Invoke(this)) ?? ""; set => TextSet.Invoke(this, [UiArg.String(value)]); }
    public void OnValueChanged(Action<string> callback)
        => NativeUiSession.Listen(this, new UiEvent(ChangedGet.Invoke(this)), callback);
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "Dropdown")]
public sealed class UiDropdown(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ValueGet = new(typeof(UiDropdown), "get_value", true);
    private static readonly UiMethod ValueSet = new(typeof(UiDropdown), "set_value", true, "System.Int32");
    private static readonly UiMethod ChangedGet = new(typeof(UiDropdown), "get_onValueChanged", true);
    public int Value { get => NativeUi.Unbox<int>(ValueGet.Invoke(this)); set => ValueSet.Invoke(this, [UiArg.Value(value)]); }
    public void OnValueChanged(Action<int> callback)
        => NativeUiSession.Listen(this, new UiEvent(ChangedGet.Invoke(this)), callback);
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "ScrollRect")]
public sealed class UiScrollRect(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ContentSet = new(typeof(UiScrollRect), "set_content", true, "UnityEngine.RectTransform");
    private static readonly UiMethod ViewportSet = new(typeof(UiScrollRect), "set_viewport", true, "UnityEngine.RectTransform");
    private static readonly UiMethod HorizontalSet = new(typeof(UiScrollRect), "set_horizontal", true, "System.Boolean");
    private static readonly UiMethod VerticalSet = new(typeof(UiScrollRect), "set_vertical", true, "System.Boolean");
    private static readonly UiMethod SensitivitySet = new(typeof(UiScrollRect), "set_scrollSensitivity", true, "System.Single");
    public UiRectTransform? Content { set => ContentSet.Invoke(this, [UiArg.Reference(value)]); }
    public UiRectTransform? Viewport { set => ViewportSet.Invoke(this, [UiArg.Reference(value)]); }
    public bool Horizontal { set => HorizontalSet.Invoke(this, [UiArg.Value(value)]); }
    public bool Vertical { set => VerticalSet.Invoke(this, [UiArg.Value(value)]); }
    public float ScrollSensitivity { set => SensitivitySet.Invoke(this, [UiArg.Value(value)]); }
}

[UiType("UnityEngine.UI", "UnityEngine.UI", "Mask")]
public sealed class UiMask(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod ShowGraphicSet = new(typeof(UiMask), "set_showMaskGraphic", true, "System.Boolean");
    public bool ShowMaskGraphic { set => ShowGraphicSet.Invoke(this, [UiArg.Value(value)]); }
}

public abstract class UiLayoutGroup(nint pointer) : UiComponent(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "VerticalLayoutGroup")]
public sealed class UiVerticalLayoutGroup(nint pointer) : UiLayoutGroup(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "HorizontalLayoutGroup")]
public sealed class UiHorizontalLayoutGroup(nint pointer) : UiLayoutGroup(pointer);

[UiType("UnityEngine.UI", "UnityEngine.UI", "LayoutElement")]
public sealed class UiLayoutElement(nint pointer) : UiComponent(pointer)
{
    private static readonly UiMethod MinWidthSet = new(typeof(UiLayoutElement), "set_minWidth", true, "System.Single");
    private static readonly UiMethod MinHeightSet = new(typeof(UiLayoutElement), "set_minHeight", true, "System.Single");
    private static readonly UiMethod PreferredWidthSet = new(typeof(UiLayoutElement), "set_preferredWidth", true, "System.Single");
    private static readonly UiMethod PreferredHeightSet = new(typeof(UiLayoutElement), "set_preferredHeight", true, "System.Single");
    public float MinWidth { set => MinWidthSet.Invoke(this, [UiArg.Value(value)]); }
    public float MinHeight { set => MinHeightSet.Invoke(this, [UiArg.Value(value)]); }
    public float PreferredWidth { set => PreferredWidthSet.Invoke(this, [UiArg.Value(value)]); }
    public float PreferredHeight { set => PreferredHeightSet.Invoke(this, [UiArg.Value(value)]); }
}
