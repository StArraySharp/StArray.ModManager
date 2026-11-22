namespace StArray.ModManager.UI;

/// <summary>Compatibility entry point for mods compiled against the original touch scroll API.</summary>
public static class ImGuiTouchScrollWhitelist
{
    /// <summary>Touch scrolling now discovers ImGui windows automatically.</summary>
    public static void RegisterCurrentWindowIfScrollable() { }
}
