using StArray.ModManager.Runtime;

namespace StArray.ModManager.Manager;

/// <summary>
/// Mod 条目数据模型
/// </summary>
public class ModEntry
{
    /// <summary>唯一标识</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Mod 名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>版本号</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Mod 所在文件夹路径</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>入口 DLL 或可执行文件路径</summary>
    public string? EntryPoint { get; set; }

    /// <summary>是否已启用</summary>
    public bool IsEnabled { get; set; }

    /// <summary>加载优先级（数字越小越先加载）</summary>
    public int LoadPriority { get; set; }

    /// <summary>依赖的其他 Mod ID 列表</summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>加载状态</summary>
    public ModLoadState LoadState { get; set; } = ModLoadState.NotLoaded;

    /// <summary>加载时的错误信息</summary>
    public string? LoadError { get; set; }

    /// <summary>已加载的插件实例（供 UI 调用 OnGui）</summary>
    public IModPlugin? PluginInstance { get; set; }

    public override string ToString() => $"{Name} v{Version}";
}

/// <summary>
/// Mod 加载状态
/// </summary>
public enum ModLoadState
{
    NotLoaded,
    Loading,
    Loaded,
    Error
}
