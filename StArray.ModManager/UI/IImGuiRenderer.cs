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
        if (OperatingSystem.IsAndroid())
            LoadAndroidFallbackFonts(io);
        // 注意：AddFontFromMemoryTTF 只存指针，Build() 时才真正读取数据
        try
        {
            io.Fonts.Build();
        }
        finally
        {
            // Build() 之后才能安全释放字体和 range 内存。
            FreeFontMemory();
        }
    }

    private static nint _fontPtr1, _fontPtr2, _glyphRangesPtr;
    private static readonly List<nint> _glyphRangePtrs = [];

    private static readonly (string[] Names, ushort[] Ranges)[] AndroidFallbackFonts =
    [
        // European languages and common mathematical/symbol blocks.
        (
            ["NotoSans-Regular.ttf", "NotoSansDisplay-Regular.ttf"],
            [
                0x0020, 0x024F, 0x0250, 0x02AF, 0x02B0, 0x02FF,
                0x0300, 0x036F, 0x0370, 0x03FF, 0x0400, 0x052F,
                0x1AB0, 0x1AFF, 0x1C80, 0x1C8F, 0x1CD0, 0x1CFF,
                0x1DC0, 0x1DFF, 0x1E00, 0x1EFF, 0x1F00, 0x1FFF,
                0x2000, 0x206F,
                0x2070, 0x209F, 0x20A0, 0x20CF, 0x2100, 0x214F,
                0x2150, 0x218F, 0x2190, 0x21FF, 0x2200, 0x22FF,
                0x2300, 0x23FF, 0x2460, 0x24FF, 0x2500, 0x25FF,
                0x2600, 0x26FF, 0x2700, 0x27BF, 0x27C0, 0x27FF,
                0x2900, 0x297F, 0x2980, 0x29FF, 0x2A00, 0x2AFF,
                0x2B00, 0x2BFF, 0x2C60, 0x2C7F, 0x2DE0, 0x2DFF,
                0xA640, 0xA69F, 0xA720, 0xA7FF, 0xAB30, 0xAB6F, 0
            ]
        ),
        // Symbols fonts cover arrows, technical symbols and BMP dingbats.
        (
            [
                "NotoSansSymbols2-Regular.ttf",
                "NotoSansSymbols2-Regular-Subsetted.ttf",
                "NotoSansSymbols-Regular.ttf",
                "NotoSansSymbols-Regular-Subsetted.ttf"
            ],
            [
                0x2000, 0x27FF, 0x2900, 0x2BFF, 0xFE00, 0xFE0F, 0
            ]
        ),
        (
            ["NotoSansMath-Regular.ttf"],
            [0x2000, 0x206F, 0x2190, 0x21FF, 0x2200, 0x22FF,
             0x27C0, 0x27EF, 0x2980, 0x29FF, 0x2A00, 0x2AFF, 0]
        ),
        // Arabic and Hebrew families are separate on Android.
        (
            [
                "NotoSansArabic-Regular.ttf",
                "NotoSansArabicUI-Regular.ttf",
                "NotoNaskhArabic-Regular.ttf"
            ],
            [
                0x0600, 0x06FF, 0x0700, 0x074F, 0x0750, 0x077F,
                0x0870, 0x089F, 0x08A0, 0x08FF, 0xFB50, 0xFDFF,
                0xFE70, 0xFEFF, 0
            ]
        ),
        (
            ["NotoSansHebrew-Regular.ttf", "NotoSansHebrewUI-Regular.ttf"],
            [0x0590, 0x05FF, 0xFB1D, 0xFB4F, 0]
        ),
        (
            ["NotoSansArmenian-Regular.ttf", "NotoSansArmenianUI-Regular.ttf"],
            [0x0530, 0x058F, 0xFB13, 0xFB17, 0]
        ),
        (
            ["NotoSansGeorgian-Regular.ttf", "NotoSansGeorgianUI-Regular.ttf"],
            [0x10A0, 0x10FF, 0x1C90, 0x1CBF, 0x2D00, 0x2D2F, 0]
        ),
        // Indic scripts.
        (
            ["NotoSansDevanagari-Regular.ttf", "NotoSansDevanagariUI-Regular.ttf"],
            [0x0900, 0x097F, 0x1CD0, 0x1CFF, 0xA8E0, 0xA8FF, 0]
        ),
        (
            ["NotoSansBengali-Regular.ttf", "NotoSansBengaliUI-Regular.ttf"],
            [0x0980, 0x09FF, 0]
        ),
        (
            ["NotoSansGurmukhi-Regular.ttf", "NotoSansGurmukhiUI-Regular.ttf"],
            [0x0A00, 0x0A7F, 0]
        ),
        (
            ["NotoSansGujarati-Regular.ttf", "NotoSansGujaratiUI-Regular.ttf"],
            [0x0A80, 0x0AFF, 0]
        ),
        (
            [
                "NotoSansOriya-Regular.ttf",
                "NotoSansOdia-Regular.ttf",
                "NotoSansOriyaUI-Regular.ttf",
                "NotoSansOdiaUI-Regular.ttf"
            ],
            [0x0B00, 0x0B7F, 0]
        ),
        (
            ["NotoSansTamil-Regular.ttf", "NotoSansTamilUI-Regular.ttf"],
            [0x0B80, 0x0BFF, 0]
        ),
        (
            ["NotoSansTelugu-Regular.ttf", "NotoSansTeluguUI-Regular.ttf"],
            [0x0C00, 0x0C7F, 0]
        ),
        (
            ["NotoSansKannada-Regular.ttf", "NotoSansKannadaUI-Regular.ttf"],
            [0x0C80, 0x0CFF, 0]
        ),
        (
            ["NotoSansMalayalam-Regular.ttf", "NotoSansMalayalamUI-Regular.ttf"],
            [0x0D00, 0x0D7F, 0]
        ),
        (
            ["NotoSansSinhala-Regular.ttf", "NotoSansSinhalaUI-Regular.ttf"],
            [0x0D80, 0x0DFF, 0]
        ),
        // Southeast Asian and other BMP scripts.
        (
            ["NotoSansThai-Regular.ttf", "NotoSansThaiLooped-Regular.ttf"],
            [0x0E00, 0x0E7F, 0]
        ),
        (
            ["NotoSansLao-Regular.ttf"],
            [0x0E80, 0x0EFF, 0]
        ),
        (
            ["NotoSansTibetan-Regular.ttf"],
            [0x0F00, 0x0FFF, 0]
        ),
        (
            ["NotoSansMyanmar-Regular.ttf"],
            [0x1000, 0x109F, 0xAA60, 0xAA7F, 0xA9E0, 0xA9FF, 0]
        ),
        (
            ["NotoSansEthiopic-Regular.ttf"],
            [0x1200, 0x137F, 0x1380, 0x139F, 0x2D80, 0x2DDF, 0xAB00, 0xAB2F, 0]
        ),
        (
            ["NotoSansMongolian-Regular.ttf"],
            [0x1800, 0x18AF, 0]
        ),
        (
            ["NotoSansKhmer-Regular.ttf"],
            [0x1780, 0x17FF, 0x19E0, 0x19FF, 0]
        ),
        (
            ["NotoSansCherokee-Regular.ttf"],
            [0x13A0, 0x13FF, 0xAB70, 0xABBF, 0]
        ),
        (
            ["NotoSansCanadianAboriginal-Regular.ttf"],
            [0x1400, 0x167F, 0]
        ),
        (
            ["NotoSansSyriac-Regular.ttf"],
            [0x0700, 0x074F, 0]
        ),
        (
            ["NotoSansSamaritan-Regular.ttf"],
            [0x0800, 0x083F, 0]
        ),
        (
            ["NotoSansMandaic-Regular.ttf"],
            [0x0840, 0x085F, 0]
        ),
        (
            ["NotoSansThaana-Regular.ttf"],
            [0x0780, 0x07BF, 0]
        ),
        (
            ["NotoSansNKo-Regular.ttf"],
            [0x07C0, 0x07FF, 0]
        ),
        (
            ["NotoSansTifinagh-Regular.ttf"],
            [0x2D30, 0x2D7F, 0]
        ),
        (
            ["NotoSansGlagolitic-Regular.ttf"],
            [0x2C00, 0x2C5F, 0]
        ),
        (
            ["NotoSansCoptic-Regular.ttf"],
            [0x2C80, 0x2CFF, 0]
        ),
        (
            ["NotoSansLisu-Regular.ttf"],
            [0xA4D0, 0xA4FF, 0]
        ),
        (
            ["NotoSansVai-Regular.ttf"],
            [0xA500, 0xA63F, 0]
        ),
        (
            ["NotoSansBamum-Regular.ttf"],
            [0xA6A0, 0xA6FF, 0]
        ),
        (
            ["NotoSansSaurashtra-Regular.ttf"],
            [0xA880, 0xA8DF, 0]
        ),
        (
            ["NotoSansKayahLi-Regular.ttf"],
            [0xA900, 0xA92F, 0]
        ),
        (
            ["NotoSansRejang-Regular.ttf"],
            [0xA930, 0xA95F, 0]
        ),
        (
            ["NotoSansJavanese-Regular.ttf"],
            [0xA980, 0xA9DF, 0]
        ),
        (
            ["NotoSansCham-Regular.ttf"],
            [0xAA00, 0xAA5F, 0]
        ),
        (
            ["NotoSansTaiViet-Regular.ttf"],
            [0xAA80, 0xAADF, 0]
        ),
        (
            ["NotoSansMeeteiMayek-Regular.ttf"],
            [0xABC0, 0xABFF, 0]
        ),
        (
            ["NotoSansOlChiki-Regular.ttf"],
            [0x1C50, 0x1C7F, 0]
        ),
        (
            ["NotoSansBuginese-Regular.ttf"],
            [0x1A00, 0x1A1F, 0]
        ),
        (
            ["NotoSansNewTaiLue-Regular.ttf"],
            [0x1980, 0x19DF, 0]
        ),
        (
            ["NotoSansTaiTham-Regular.ttf"],
            [0x1A20, 0x1AAF, 0]
        ),
        (
            ["NotoSansBalinese-Regular.ttf"],
            [0x1B00, 0x1B7F, 0]
        ),
        (
            ["NotoSansSundanese-Regular.ttf"],
            [0x1B80, 0x1BBF, 0]
        ),
        (
            ["NotoSansBatak-Regular.ttf"],
            [0x1BC0, 0x1BFF, 0]
        ),
        (
            ["NotoSansLepcha-Regular.ttf"],
            [0x1C00, 0x1C4F, 0]
        )
    ];

    private static void FreeFontMemory()
    {
        if (_fontPtr1 != 0) { Marshal.FreeHGlobal(_fontPtr1); _fontPtr1 = 0; }
        if (_fontPtr2 != 0) { Marshal.FreeHGlobal(_fontPtr2); _fontPtr2 = 0; }
        foreach (var ptr in _glyphRangePtrs)
            if (ptr != 0) Marshal.FreeHGlobal(ptr);
        _glyphRangePtrs.Clear();
        _glyphRangesPtr = 0;
    }

    private static unsafe nint CopyGlyphRanges(ushort[] glyphRanges)
    {
        var bytes = checked(glyphRanges.Length * sizeof(ushort));
        var ptr = Marshal.AllocHGlobal(bytes);
        try
        {
            fixed (ushort* ranges = glyphRanges)
            {
                Buffer.MemoryCopy(
                    ranges,
                    (void*)ptr,
                    bytes,
                    bytes);
            }
            _glyphRangePtrs.Add(ptr);
            return ptr;
        }
        catch
        {
            Marshal.FreeHGlobal(ptr);
            throw;
        }
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
            try
            {
                cfg->MergeMode = 1;
                cfg->FontDataOwnedByAtlas = 0; // 自己管理内存，Build() 后释放

                ushort[] glyphRanges =
                [
                    // Latin, Greek, Cyrillic, punctuation, math and symbols.
                    0x0020, 0x024F, 0x0250, 0x02FF,
                    0x0300, 0x036F, 0x0370, 0x03FF,
                    0x0400, 0x052F, 0x1100, 0x11FF,
                    0x1AB0, 0x1AFF, 0x1C80, 0x1C8F,
                    0x1CD0, 0x1CFF, 0x1DC0, 0x1DFF,
                    0x1E00, 0x1EFF, 0x1F00, 0x1FFF,
                    0x2C60, 0x2C7F,
                    0x2DE0, 0x2DFF, 0xA640, 0xA69F,
                    0xA720, 0xA7FF, 0xAB30, 0xAB6F,
                    0x2000, 0x206F, 0x2070, 0x209F,
                    0x20A0, 0x20CF, 0x2100, 0x214F,
                    0x2150, 0x218F, 0x2190, 0x21FF,
                    0x2200, 0x22FF, 0x2300, 0x23FF,
                    0x2460, 0x24FF, 0x2500, 0x25FF,
                    0x2600, 0x26FF, 0x2700, 0x27FF,
                    0x2900, 0x29FF, 0x2A00, 0x2AFF,
                    0x2B00, 0x2BFF,

                    // CJK punctuation, Japanese, Korean, Chinese and fullwidth forms.
                    0x2E80, 0x2FFF, 0x3000, 0x303F,
                    0x3040, 0x30FF, 0x3100, 0x312F,
                    0x3130, 0x318F, 0x3190, 0x31FF,
                    0x3200, 0x33FF, 0x3400, 0x4DBF,
                    0x4E00, 0x9FFF, 0xA960, 0xA97F,
                    0xAC00, 0xD7FF, 0xF900, 0xFAFF,
                    0xFB00, 0xFB4F, 0xFE10, 0xFE6F,
                    0xFF00, 0xFFEF,
                    0
                ];
                _glyphRangesPtr = CopyGlyphRanges(glyphRanges);
                io.Fonts.AddFontFromMemoryTTF(_fontPtr1, ttf.Length, 16f, cfg, _glyphRangesPtr);
            }
            finally
            {
                ImGuiNative.ImFontConfig_destroy(cfg);
            }
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
            try
            {
                cfg->FontDataOwnedByAtlas = 0; // 自己管理内存

                ushort[] iconRange = [0xe005, 0xf8ff, 0];
                var iconRangePtr = CopyGlyphRanges(iconRange);
                io.Fonts.AddFontFromMemoryTTF(_fontPtr2, ttf.Length, 16f, cfg, iconRangePtr);
            }
            finally
            {
                ImGuiNative.ImFontConfig_destroy(cfg);
            }
        }
        catch { /* 静默跳过 */ }
    }

    private static unsafe void LoadAndroidFallbackFonts(ImGuiIOPtr io)
    {
        var loaded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spec in AndroidFallbackFonts)
        {
            var path = FindAndroidFont(spec.Names);
            if (path == null || !loaded.Add(path))
                continue;

            try
            {
                var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
                try
                {
                    cfg->MergeMode = 1;
                    cfg->FontDataOwnedByAtlas = 1;
                    var rangePtr = CopyGlyphRanges(spec.Ranges);
                    io.Fonts.AddFontFromFileTTF(path, 16f, cfg, rangePtr);
                }
                finally
                {
                    ImGuiNative.ImFontConfig_destroy(cfg);
                }
            }
            catch
            {
                // Android vendors ship different subsets of the Noto family.
                // A missing or unsupported fallback must not abort ImGui init.
            }
        }
    }

    private static string? FindAndroidFont(string[] names)
    {
        string[] roots =
        [
            "/system/fonts",
            "/system_ext/fonts",
            "/product/fonts",
            "/vendor/fonts"
        ];

        foreach (var name in names)
        foreach (var root in roots)
        {
            var path = Path.Combine(root, name);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
