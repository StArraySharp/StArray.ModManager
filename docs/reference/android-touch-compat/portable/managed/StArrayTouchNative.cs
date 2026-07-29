using System.Runtime.InteropServices;

namespace StArray.ModManager.Android.Touch;

internal static class StArrayTouchNative
{
    [DllImport("starray_modmanager", EntryPoint = "modmanager_imgui_drain_forwarded_motion_events")]
    internal static extern int DrainForwardedMotionEvents();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_begin_frame")]
    internal static extern void BeginOverlayInputFrame();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_add_rect")]
    internal static extern void AddOverlayInputRect(float x, float y, float width, float height);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_touch_commit_frame")]
    internal static extern void EndOverlayInputFrame();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_is_visible")]
    [return: MarshalAs(UnmanagedType.I4)]
    internal static extern int IsOverlayVisible();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_ui_set_visible")]
    internal static extern void SetOverlayVisible(int visible);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_overlay_input_request_focus_release")]
    internal static extern void RequestOverlayFocusRelease();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_modal_input_is_active")]
    internal static extern int IsModalInputCaptureActive();

    [DllImport("starray_modmanager", EntryPoint = "modmanager_modal_input_set_active")]
    internal static extern void SetModalInputCapture(int active);

    [DllImport("starray_modmanager", EntryPoint = "modmanager_modal_input_take_close_request")]
    internal static extern int ConsumeModalCloseRequest();
}
