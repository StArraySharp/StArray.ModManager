namespace StArray.ModManager.Runtime;

/// <summary>
/// Mod 插件接口 —— Mod DLL 实现此接口，元数据由属性声明
/// </summary>
public interface IModPlugin
{
    /// <summary>唯一标识</summary>
    string Id { get; }

    /// <summary>显示名称</summary>
    string Name { get; }

    /// <summary>版本号</summary>
    string Version { get; }

    /// <summary>作者</summary>
    string Author { get; }

    /// <summary>描述</summary>
    string Description { get; }

    /// <summary>加载优先级（数字越小越先）</summary>
    int LoadPriority { get; }

    /// <summary>依赖的其他 Mod ID</summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>Mod 加载时调用</summary>
    void OnLoad();

    /// <summary>Mod 卸载时调用</summary>
    void OnUnload();
}
