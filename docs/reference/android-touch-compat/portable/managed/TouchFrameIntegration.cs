using ImGuiNET;

namespace StArray.ModManager.Android.Touch;

internal static class TouchFrameIntegration
{
    internal static void BeginRenderFrame(int width, int height)
    {
        // Run the renderer backend NewFrame calls first so DisplaySize is current.
        // Drain must still happen before ImGui.NewFrame consumes queued input.
        StArrayTouchNative.DrainForwardedMotionEvents();
        ImGui.NewFrame();
    }

    internal static void BeginOverlayInputFrame()
        => StArrayTouchNative.BeginOverlayInputFrame();

    internal static void AddOverlayInputRect(float x, float y, float width, float height)
        => StArrayTouchNative.AddOverlayInputRect(x, y, width, height);

    internal static void EndOverlayInputFrame()
        => StArrayTouchNative.EndOverlayInputFrame();

    internal static void SetOverlayVisible(bool visible)
    {
        if (!visible)
            StArrayTouchNative.RequestOverlayFocusRelease();
        StArrayTouchNative.SetOverlayVisible(visible ? 1 : 0);
    }
}
