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

        try
        {
            var hasBaseFont = LoadEmbeddedFont(
                io,
                "StArray.ModManager.Resources.NotoSansCJK-Regular.otf",
                ref _cjkFontPtr,
                merge: false,
                GetAllUnicodeRanges());

            var latinFontIsBase = false;
            if (!hasBaseFont)
            {
                latinFontIsBase = LoadEmbeddedFont(
                    io,
                    "StArray.ModManager.Resources.NotoSans-Regular.ttf",
                    ref _latinFontPtr,
                    merge: false,
                    GetAllUnicodeRanges());
                hasBaseFont = latinFontIsBase;
            }

            if (!hasBaseFont)
            {
                io.Fonts.AddFontDefault();
                hasBaseFont = true;
            }

            if (!latinFontIsBase)
            {
                LoadEmbeddedFont(
                    io,
                    "StArray.ModManager.Resources.NotoSans-Regular.ttf",
                    ref _latinFontPtr,
                    merge: true,
                    GetAllUnicodeRanges());
            }
            LoadEmbeddedFont(
                io,
                "StArray.ModManager.Resources.NotoSansSymbols2-Regular.ttf",
                ref _symbolsFontPtr,
                merge: true,
                GetAllUnicodeRanges());
            LoadEmbeddedFont(
                io,
                "StArray.ModManager.Resources.OpenMoji-black-glyf.ttf",
                ref _emojiFontPtr,
                merge: true,
                GetAllUnicodeRanges());
            LoadEmbeddedFont(
                io,
                "StArray.ModManager.Resources.fa-solid-900.ttf",
                ref _iconFontPtr,
                merge: true,
                GetIconRanges());

            // AddFontFromMemoryTTF 只保存指针，Build() 时才真正读取数据。
            io.Fonts.Build();
        }
        finally
        {
            FreeFontMemory();
        }
    }

    private static nint _cjkFontPtr;
    private static nint _latinFontPtr;
    private static nint _symbolsFontPtr;
    private static nint _emojiFontPtr;
    private static nint _iconFontPtr;
    private static nint _allUnicodeRangesPtr;
    private static nint _iconRangesPtr;

    private static void FreeFontMemory()
    {
        Free(ref _cjkFontPtr);
        Free(ref _latinFontPtr);
        Free(ref _symbolsFontPtr);
        Free(ref _emojiFontPtr);
        Free(ref _iconFontPtr);
        Free(ref _allUnicodeRangesPtr);
        Free(ref _iconRangesPtr);
    }

    private static void Free(ref nint ptr)
    {
        if (ptr == 0) return;
        Marshal.FreeHGlobal(ptr);
        ptr = 0;
    }

    private static nint GetAllUnicodeRanges()
    {
        if (_allUnicodeRangesPtr != 0) return _allUnicodeRangesPtr;
        _allUnicodeRangesPtr = AllocateRanges(0x0001, 0xFFFF);
        return _allUnicodeRangesPtr;
    }

    private static nint GetIconRanges()
    {
        if (_iconRangesPtr != 0) return _iconRangesPtr;
        _iconRangesPtr = AllocateRanges(0xE000, 0xF8FF);
        return _iconRangesPtr;
    }

    private static nint AllocateRanges(ushort first, ushort last)
    {
        var ptr = Marshal.AllocHGlobal(sizeof(ushort) * 3);
        Marshal.WriteInt16(ptr, 0, (short)first);
        Marshal.WriteInt16(ptr, sizeof(ushort), (short)last);
        Marshal.WriteInt16(ptr, sizeof(ushort) * 2, 0);
        return ptr;
    }

    private static unsafe bool LoadEmbeddedFont(
        ImGuiIOPtr io,
        string resourceName,
        ref nint fontPtr,
        bool merge,
        nint glyphRanges)
    {
        try
        {
            var asm = typeof(IImGuiRenderer).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return false;

            var fontData = new byte[checked((int)stream.Length)];
            stream.ReadExactly(fontData);
            fontPtr = Marshal.AllocHGlobal(fontData.Length);
            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

            var cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            try
            {
                cfg->MergeMode = merge ? (byte)1 : (byte)0;
                cfg->FontDataOwnedByAtlas = 0;
                io.Fonts.AddFontFromMemoryTTF(
                    fontPtr,
                    fontData.Length,
                    16f,
                    cfg,
                    glyphRanges);
            }
            finally
            {
                ImGuiNative.ImFontConfig_destroy(cfg);
            }

            return true;
        }
        catch
        {
            Free(ref fontPtr);
            return false;
        }
    }

    /// <summary>
    /// 每帧 UI 构建回调（由渲染循环驱动）
    /// </summary>
    event Action OnRender;
}
