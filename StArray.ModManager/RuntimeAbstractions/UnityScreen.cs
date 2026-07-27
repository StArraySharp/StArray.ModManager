using System.Numerics;
using StArray.ModManager.Manager;

namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>
/// 世界坐标 → ImGui 屏幕坐标。用于让绘制的 UI 跟随 Unity 组件位置。
///
/// 时序上适合在 <c>OnGUI</c> 里用：present hook 发生在 Unity 渲染之后，
/// 此时 Transform 与相机矩阵都是本帧最终值，不存在一帧延迟。
///
/// 注意 il2cpp 的调用约定：<c>runtime_invoke</c> 的参数数组对<b>值类型传指向值的指针</b>，
/// 对引用类型才直接传对象指针。所以 Vector3 参数放的是 <c>&amp;vec</c>。
/// 返回的值类型是装箱对象，必须 unbox —— 这里统一走 <c>InvokeUnbox</c>。
/// </summary>
public static unsafe class UnityScreen
{
    private const string CoreModule = "UnityEngine.CoreModule.dll";

    private static IRuntimeClass? _cameraClass;
    private static IRuntimeMethod? _getMain;
    private static IRuntimeMethod? _worldToScreenPoint;
    private static IRuntimeMethod? _getPixelWidth;
    private static IRuntimeMethod? _getPixelHeight;
    private static IRuntimeMethod? _getTransform;
    private static IRuntimeMethod? _getPosition;
    private static bool _resolved;

    private static nint _cachedCamera;
    private static int _cameraAge;

    /// <summary>Camera.main 缓存的刷新间隔（帧）。场景切换后旧指针会失效。</summary>
    public static int CameraRefreshInterval { get; set; } = 120;

    /// <summary>丢弃缓存的类型/方法/相机句柄，下次调用重新解析。切换场景或重载后可调用。</summary>
    public static void Invalidate()
    {
        _resolved = false;
        _cameraClass = null;
        _getMain = _worldToScreenPoint = _getPixelWidth = _getPixelHeight = null;
        _getTransform = _getPosition = null;
        _cachedCamera = 0;
        _cameraAge = 0;
    }

    private static bool Resolve()
    {
        if (_resolved) return _cameraClass != null;
        _resolved = true;

        try
        {
            var domain = RuntimeManager.GetDomain();
            var core = domain?.OpenAssembly(CoreModule);
            if (core == null)
            {
                Logger.Warn(nameof(UnityScreen), $"{CoreModule} not found");
                return false;
            }

            _cameraClass = core.GetClass("UnityEngine", "Camera");
            if (_cameraClass == null)
            {
                Logger.Warn(nameof(UnityScreen), "UnityEngine.Camera not found");
                return false;
            }

            _getMain = _cameraClass.GetMethod("get_main", 0);
            _worldToScreenPoint = _cameraClass.GetMethod("WorldToScreenPoint", 1);
            _getPixelWidth = _cameraClass.GetMethod("get_pixelWidth", 0);
            _getPixelHeight = _cameraClass.GetMethod("get_pixelHeight", 0);

            var component = core.GetClass("UnityEngine", "Component");
            _getTransform = component?.GetMethod("get_transform", 0);

            var transform = core.GetClass("UnityEngine", "Transform");
            _getPosition = transform?.GetMethod("get_position", 0);

            return _worldToScreenPoint != null;
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(UnityScreen), $"Resolve: {ex.Message}");
            _cameraClass = null;
            return false;
        }
    }

    /// <summary>
    /// 当前主相机的 il2cpp 对象指针（带缓存）。Camera.main 内部是按 tag 查找，
    /// 每帧调用代价不小，所以缓存若干帧。
    /// </summary>
    public static nint MainCamera
    {
        get
        {
            if (!Resolve() || _getMain == null) return 0;

            if (_cachedCamera != 0 && ++_cameraAge < CameraRefreshInterval)
                return _cachedCamera;

            _cameraAge = 0;
            try { _cachedCamera = _getMain.InvokeStatic(); }
            catch (Exception ex)
            {
                Logger.Error(nameof(UnityScreen), $"Camera.main: {ex.Message}");
                _cachedCamera = 0;
            }
            return _cachedCamera;
        }
    }

    /// <summary>相机的像素尺寸；取不到时回退到 ImGui 的显示尺寸。</summary>
    private static Vector2 CameraPixelSize(nint camera)
    {
        try
        {
            if (_getPixelWidth != null && _getPixelHeight != null)
            {
                var w = _getPixelWidth.InvokeUnbox<int>(camera);
                var h = _getPixelHeight.InvokeUnbox<int>(camera);
                if (w > 0 && h > 0) return new Vector2(w, h);
            }
        }
        catch { /* 回退 */ }

        var size = ImGuiNET.ImGui.GetIO().DisplaySize;
        return size.Y > 0 ? size : new Vector2(1920, 1080);
    }

    /// <summary>
    /// 世界坐标 → ImGui 屏幕坐标。
    /// </summary>
    /// <param name="world">世界坐标</param>
    /// <param name="screen">ImGui 坐标（原点左上，Y 向下）</param>
    /// <param name="depth">到相机的距离（世界单位）；&lt;= 0 表示在相机背后</param>
    /// <returns>是否位于相机前方 —— 背后时 <paramref name="screen"/> 无意义</returns>
    public static bool TryWorldToScreen(Vector3 world, out Vector2 screen, out float depth)
    {
        screen = default;
        depth = 0;

        var camera = MainCamera;
        if (camera == 0 || _worldToScreenPoint == null) return false;

        Vector3 raw;
        try
        {
            // 值类型参数：传指向值的指针，不是值本身
            var arg = (nint)(&world);
            raw = _worldToScreenPoint.InvokeUnbox<Vector3>(camera, [arg]);
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(UnityScreen), $"WorldToScreenPoint: {ex.Message}");
            return false;
        }

        depth = raw.Z;
        if (raw.Z <= 0) return false; // 相机背后

        // Unity 屏幕坐标原点在左下、Y 向上；ImGui 原点在左上、Y 向下。
        // 先按相机像素高度翻转，再缩放到 ImGui 的显示尺寸（渲染缩放时两者可能不同）。
        var cam = CameraPixelSize(camera);
        var display = ImGuiNET.ImGui.GetIO().DisplaySize;
        var sx = display.X / cam.X;
        var sy = display.Y / cam.Y;

        screen = new Vector2(raw.X * sx, (cam.Y - raw.Y) * sy);
        return true;
    }

    /// <inheritdoc cref="TryWorldToScreen(Vector3, out Vector2, out float)"/>
    public static bool TryWorldToScreen(Vector3 world, out Vector2 screen)
        => TryWorldToScreen(world, out screen, out _);

    /// <summary>读取一个 Component / GameObject 的 Transform 世界坐标。</summary>
    public static bool TryGetWorldPosition(nint component, out Vector3 world)
    {
        world = default;
        if (component == 0 || !Resolve()) return false;
        if (_getTransform == null || _getPosition == null) return false;

        try
        {
            var transform = _getTransform.Invoke(component);
            if (transform == 0) return false;
            world = _getPosition.InvokeUnbox<Vector3>(transform);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(UnityScreen), $"Transform.position: {ex.Message}");
            return false;
        }
    }

    /// <summary>组件位置 → ImGui 屏幕坐标，一步到位。</summary>
    public static bool TryComponentToScreen(nint component, out Vector2 screen)
    {
        screen = default;
        return TryGetWorldPosition(component, out var world) &&
               TryWorldToScreen(world, out screen);
    }
}
