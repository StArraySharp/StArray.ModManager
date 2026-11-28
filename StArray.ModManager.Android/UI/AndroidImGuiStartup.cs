using ImGuiNET;
using StArray.ModManager.Manager;
using StArray.ModManager.UI;

namespace StArray.ModManager.Android.UI;

internal sealed unsafe class AndroidImGuiStartup
{
    private readonly Task<nint> _atlas = Task.Run(BuildAtlas);
    private nint _context;

    internal bool TryInitializeContext()
    {
        if (!_atlas.IsCompleted)
            return false;
        if (_context != 0)
        {
            ImGui.SetCurrentContext(_context);
            return true;
        }

        if (_atlas.IsCompletedSuccessfully)
        {
            _context = ImGui.CreateContext(new ImFontAtlasPtr((ImFontAtlas*)_atlas.Result));
        }
        else
        {
            Logger.Warn(nameof(AndroidImGuiStartup), $"Using the default font: {_atlas.Exception?.GetBaseException().Message}");
            _context = ImGui.CreateContext();
        }
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        if (_atlas.IsCompletedSuccessfully)
            ImGuiFonts.LogAtlas(io.Fonts);
        return true;
    }

    private static nint BuildAtlas()
    {
        ImFontAtlas* atlas = ImGuiNative.ImFontAtlas_ImFontAtlas();
        try
        {
            ImGuiFonts.Build(new ImFontAtlasPtr(atlas), prepareRgbaTexture: true);
            return (nint)atlas;
        }
        catch
        {
            ImGuiNative.ImFontAtlas_destroy(atlas);
            throw;
        }
    }
}
