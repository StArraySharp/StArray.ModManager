using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace StArray.ModManager.UI;

/// <summary>
/// ImGui 渲染器接口 —— 抽象渲染管线，允许替换不同的渲染后端
/// </summary>
public interface IImGuiRenderer
{
    /// <summary>
    /// 是否已完成初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 安装 Hook 并准备渲染管线
    /// </summary>
    bool Install();

    /// <summary>
    /// 初始化 ImGui 上下文 + 加载嵌入式字体 (文泉驿正黑 + FontAwesome 7 图标)
    /// </summary>
    void InitImGui()
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        LoadIconFont(io);
        LoadEmbeddedFont(io);
        io.Fonts.Build();
    }

    private static unsafe void LoadEmbeddedFont(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(IImGuiRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(
                "StArray.ModManager.Resources.msyh.ttf");
            if (stream == null) return;

            var ttf = new byte[stream.Length];
            stream.ReadExactly(ttf);
            var ptr = Marshal.AllocHGlobal(ttf.Length);
            Marshal.Copy(ttf, 0, ptr, ttf.Length);

            // MergeMode: 中文字形合并到图标基础字体
            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg->MergeMode = 1;

            var glyphRanges = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();
            io.Fonts.AddFontFromMemoryTTF(ptr, ttf.Length, 16f, cfg, glyphRanges);
            Marshal.FreeHGlobal(ptr);
        }
        catch { /* 静默跳过 */ }
    }

    private static unsafe void LoadIconFont(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(IImGuiRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(
                "StArray.ModManager.Resources.Font Awesome 7 Free-Solid-900.otf");
            if (stream == null) return;

            var ttf = new byte[stream.Length];
            stream.ReadExactly(ttf);
            var ptr = Marshal.AllocHGlobal(ttf.Length);
            Marshal.Copy(ttf, 0, ptr, ttf.Length);

            // 基础字体：FontAwesome 7 图标
            ushort[] iconRange = [0xe005, 0xf8ff, 0];
            fixed (ushort* r = iconRange)
                io.Fonts.AddFontFromMemoryTTF(ptr, ttf.Length, 16f, null, (IntPtr)r);

            Marshal.FreeHGlobal(ptr);
        }
        catch { /* 静默跳过 */ }
    }

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
