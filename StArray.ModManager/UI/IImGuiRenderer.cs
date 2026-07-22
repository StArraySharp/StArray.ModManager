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
        ImGui.SetCurrentContext(ImGui.CreateContext());
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        LoadIconFont(io);
        LoadEmbeddedFont(io);
        // 注意：AddFontFromMemoryTTF 只存指针，Build() 时才真正读取数据
        io.Fonts.Build();
        // Build() 之后才能安全释放字体内存
        FreeFontMemory();
    }

    private static nint _fontPtr1, _fontPtr2;

    private static void FreeFontMemory()
    {
        if (_fontPtr1 != 0) { Marshal.FreeHGlobal(_fontPtr1); _fontPtr1 = 0; }
        if (_fontPtr2 != 0) { Marshal.FreeHGlobal(_fontPtr2); _fontPtr2 = 0; }
    }

    private static unsafe void LoadEmbeddedFont(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(IImGuiRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(
                "StArray.ModManager.Resources.NotoSansCJK-Regular.otf");
            if (stream == null) return;

            var ttf = new byte[stream.Length];
            stream.ReadExactly(ttf);
            _fontPtr1 = Marshal.AllocHGlobal(ttf.Length);
            Marshal.Copy(ttf, 0, _fontPtr1, ttf.Length);

            // MergeMode: 中文字形合并到图标基础字体
            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg->MergeMode = 1;
            cfg->FontDataOwnedByAtlas = 0; // 自己管理内存，Build() 后释放

            var glyphRanges = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();
            io.Fonts.AddFontFromMemoryTTF(_fontPtr1, ttf.Length, 16f, cfg, glyphRanges);
            ImGuiNative.ImFontConfig_destroy(cfg);
        }
        catch { /* 静默跳过 */ }
    }

    private static unsafe void LoadIconFont(ImGuiIOPtr io)
    {
        try
        {
            var asm = typeof(IImGuiRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(
                "StArray.ModManager.Resources.fa-solid-900.ttf");
            if (stream == null) return;

            var ttf = new byte[stream.Length];
            stream.ReadExactly(ttf);
            _fontPtr2 = Marshal.AllocHGlobal(ttf.Length);
            Marshal.Copy(ttf, 0, _fontPtr2, ttf.Length);

            // 基础字体：FontAwesome 7 图标
            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg->FontDataOwnedByAtlas = 0; // 自己管理内存

            ushort[] iconRange = [0xe005, 0xf8ff, 0];
            fixed (ushort* r = iconRange)
                io.Fonts.AddFontFromMemoryTTF(_fontPtr2, ttf.Length, 16f, cfg, (IntPtr)r);
            ImGuiNative.ImFontConfig_destroy(cfg);
        }
        catch { /* 静默跳过 */ }
    }

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
