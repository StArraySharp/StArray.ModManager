using System.Text.Json;

namespace StArray.ModManager.Manager;

/// <summary>
/// Mod 管理器全局配置 —— UI 设置 + 各 Mod 启用状态
/// </summary>
public class ModManagerConfig
{
    /// <summary>Mods 目录路径</summary>
    public string ModsDirectory { get; set; } = string.Empty;

    /// <summary>界面缩放 (FontGlobalScale)</summary>
    public float UiScale { get; set; } = 2f;

    /// <summary>滑动条抓取宽度</summary>
    public float GrabMinSize { get; set; } = 10f;

    /// <summary>滚动条宽度</summary>
    public float ScrollbarSize { get; set; } = 16f;

    /// <summary>Mod ID → 是否启用</summary>
    public Dictionary<string, bool> ModEnabled { get; set; } = new();

    private const string FileName = "modmanager_config.json";

    /// <summary>保存到指定目录（源生成器）</summary>
    public void Save(string directory)
    {
        var path = Path.Combine(directory, FileName);
        var json = JsonSerializer.Serialize(this, ModManagerJsonContext.Default.ModManagerConfig);
        File.WriteAllText(path, json);
    }

    /// <summary>从指定目录加载（源生成器），失败返回默认</summary>
    public static ModManagerConfig Load(string directory)
    {
        var path = Path.Combine(directory, FileName);
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize(json, ModManagerJsonContext.Default.ModManagerConfig)
                    ?? new ModManagerConfig();
            }
        }
        catch { /* ignore corrupt config */ }
        return new ModManagerConfig();
    }
}
