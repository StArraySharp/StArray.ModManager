using System.Runtime.InteropServices;
using System.Text.Json;
using StArray.ModManager.Android.Native;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;
using Xphorror.PcModCompat;

namespace StArray.ModManager.Android.UI;

public sealed class AndroidModManagerPlatformServices : IModManagerPlatformServices, IDisposable
{
    private readonly JavaClass _bootstrap;
    private readonly nint _requestImportModZip;
    private readonly nint _getLastImportStatusJson;
    private readonly nint _setModalInputCapture;
    private readonly nint _isModalInputCaptureActive;
    private readonly nint _consumeModalCloseRequest;
    private bool _disposed;

    public AndroidModManagerPlatformServices()
    {
        _bootstrap = new JavaClass("com.fizzd.connectedworlds.editorport.StArrayModManagerBootstrap");
        _requestImportModZip = _bootstrap.GetStaticMethodID("requestImportModZip", "()V");
        _getLastImportStatusJson = _bootstrap.GetStaticMethodID(
            "getLastImportStatusJson",
            "()Ljava/lang/String;");
        _setModalInputCapture = _bootstrap.GetStaticMethodID(
            "setModalInputCapture",
            "(Z)V");
        _isModalInputCaptureActive = _bootstrap.GetStaticMethodID(
            "isModalInputCaptureActive",
            "()I");
        _consumeModalCloseRequest = _bootstrap.GetStaticMethodID(
            "consumeModalCloseRequest",
            "()I");
    }

    public bool SupportsModZipImport => _requestImportModZip != 0 && _getLastImportStatusJson != 0;

    public bool IsOverlayVisible => modmanager_overlay_ui_is_visible() != 0;

    public bool IsModalInputCaptureActive
    {
        get
        {
            var nativeActive = modmanager_modal_input_is_active() != 0;
            return nativeActive ||
                   (_isModalInputCaptureActive != 0 &&
                    _bootstrap.CallStaticIntMethod0(_isModalInputCaptureActive) != 0);
        }
    }

    public void RequestModZipImport()
    {
        if (_requestImportModZip == 0 ||
            !ModManagerRuntimeAuthorization.IsAllowed)
            return;

        _bootstrap.CallStaticVoidMethod0(_requestImportModZip);
    }

    public ModImportStatus GetModZipImportStatus()
    {
        if (_getLastImportStatusJson == 0)
            return new ModImportStatus(0, ModImportState.Error, "Android import bridge is unavailable", null);

        var jsonRef = _bootstrap.CallStaticObjectMethod0(_getLastImportStatusJson);
        if (jsonRef == IntPtr.Zero)
            return new ModImportStatus(0, ModImportState.Idle, string.Empty, null);

        try
        {
            var json = JniHelperNative.GetString(jsonRef);
            if (string.IsNullOrWhiteSpace(json))
                return new ModImportStatus(0, ModImportState.Idle, string.Empty, null);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var serial = root.TryGetProperty("serial", out var serialElem) ? serialElem.GetInt32() : 0;
            var stateText = root.TryGetProperty("state", out var stateElem)
                ? stateElem.GetString()
                : "Idle";
            var message = root.TryGetProperty("message", out var messageElem)
                ? messageElem.GetString() ?? string.Empty
                : string.Empty;
            var path = root.TryGetProperty("path", out var pathElem)
                ? pathElem.GetString()
                : null;

            return new ModImportStatus(serial, ParseState(stateText), message, path);
        }
        catch (Exception ex)
        {
            Logger.Warn(nameof(AndroidModManagerPlatformServices), $"Import status parse failed: {ex.Message}");
            return new ModImportStatus(0, ModImportState.Error, ex.Message, null);
        }
        finally
        {
            JniHelperNative.DeleteLocalRef(jsonRef);
        }
    }

    public void BeginOverlayInputFrame()
        => modmanager_overlay_touch_begin_frame();

    public void AddOverlayInputRect(float x, float y, float width, float height)
        => modmanager_overlay_touch_add_rect(x, y, width, height);

    public void EndOverlayInputFrame()
        => modmanager_overlay_touch_commit_frame();

    public void SetOverlayVisible(bool visible)
    {
        if (!visible)
            modmanager_overlay_input_request_focus_release();
        modmanager_overlay_ui_set_visible(visible ? 1 : 0);
        ImGuiInputHandler.SetImeOwner(
            visible
                ? AndroidImeOwner.ModManager
                : IsModalInputCaptureActive
                    ? AndroidImeOwner.UnitySettings
                    : AndroidImeOwner.None);
    }

    public void SetModalInputCapture(bool active, bool blockUnityEventSystem)
    {
        PcCompatLegacyInputBridge.SetModalInputCapture(active);
        if (active)
            modmanager_modal_input_set_unity_event_system_blocked(
                blockUnityEventSystem ? 1 : 0);

        // async_input reads the native gate directly. Update it before the
        // Java mirror so a failed JNI call cannot leave gameplay input blocked.
        modmanager_modal_input_set_active(active ? 1 : 0);
        if (_setModalInputCapture != 0)
            _bootstrap.CallStaticVoidMethod1(_setModalInputCapture, active);

        if (!active)
            modmanager_modal_input_set_unity_event_system_blocked(0);

        ImGuiInputHandler.SetImeOwner(
            active
                ? AndroidImeOwner.UnitySettings
                : IsOverlayVisible
                    ? AndroidImeOwner.ModManager
                    : AndroidImeOwner.None);
    }

    public bool ConsumeModalCloseRequest()
        => _consumeModalCloseRequest != 0 &&
           _bootstrap.CallStaticIntMethod0(_consumeModalCloseRequest) != 0;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _bootstrap.Dispose();
    }

    private static ModImportState ParseState(string? state)
        => Enum.TryParse<ModImportState>(state, ignoreCase: true, out var result)
            ? result
            : ModImportState.Error;

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_begin_frame")]
    private static extern void modmanager_overlay_touch_begin_frame();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_add_rect")]
    private static extern void modmanager_overlay_touch_add_rect(float x, float y, float width, float height);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_commit_frame")]
    private static extern void modmanager_overlay_touch_commit_frame();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_is_visible")]
    private static extern int modmanager_overlay_ui_is_visible();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_set_visible")]
    private static extern void modmanager_overlay_ui_set_visible(int visible);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_input_request_focus_release")]
    private static extern void modmanager_overlay_input_request_focus_release();

    [DllImport(
        "starray_modmanager",
        EntryPoint = "modmanager_modal_input_set_unity_event_system_blocked")]
    private static extern void modmanager_modal_input_set_unity_event_system_blocked(int blocked);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_modal_input_is_active")]
    private static extern int modmanager_modal_input_is_active();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_modal_input_set_active")]
    private static extern void modmanager_modal_input_set_active(int active);
}
