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

    private static nint _fontPtr1, _fontPtr2, _glyphRangesPtr;

    private static void FreeFontMemory()
    {
        if (_fontPtr1 != 0) { Marshal.FreeHGlobal(_fontPtr1); _fontPtr1 = 0; }
        if (_fontPtr2 != 0) { Marshal.FreeHGlobal(_fontPtr2); _fontPtr2 = 0; }
        if (_glyphRangesPtr != 0) { Marshal.FreeHGlobal(_glyphRangesPtr); _glyphRangesPtr = 0; }
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

            // Merge the CJK font into the icon font. The range is kept in unmanaged
            // memory until Build() because ImGui reads it while building the atlas.
            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            cfg->MergeMode = 1;
            cfg->FontDataOwnedByAtlas = 0; // 自己管理内存，Build() 后释放

            ushort[] glyphRanges =
            [
                0x0020, 0x00FF, // Basic Latin and Latin-1 Supplement
                0x1100, 0x11FF, // Hangul Jamo
                0x2000, 0x206F, // General Punctuation
                0x2E80, 0x2FFF, // CJK radicals and ideographs
                0x3000, 0x30FF, // CJK punctuation, Hiragana, Katakana
                0x3100, 0x31FF, // Bopomofo and Hangul compatibility Jamo
                0x3200, 0x33FF, // Enclosed CJK and compatibility characters
                0x3400, 0x4DBF, // CJK Unified Ideographs Extension A
                0x4E00, 0x9FFF, // CJK Unified Ideographs
                0xA960, 0xA97F, // Hangul Jamo Extended-A
                0xAC00, 0xD7A3, // Hangul syllables
                0xD7B0, 0xD7FF, // Hangul Jamo Extended-B
                0xF900, 0xFAFF, // CJK compatibility ideographs
                0xFE10, 0xFE6F, // CJK compatibility forms
                0xFF00, 0xFFEF, // Halfwidth and Fullwidth Forms
                0
            ];
            var rangeBytes = checked(glyphRanges.Length * sizeof(ushort));
            _glyphRangesPtr = Marshal.AllocHGlobal(rangeBytes);
            fixed (ushort* ranges = glyphRanges)
            {
                Buffer.MemoryCopy(
                    ranges,
                    (void*)_glyphRangesPtr,
                    rangeBytes,
                    rangeBytes);
            }

            io.Fonts.AddFontFromMemoryTTF(_fontPtr1, ttf.Length, 16f, cfg, _glyphRangesPtr);
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
