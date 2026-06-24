using System.Reflection;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Runtime;

/// <summary>
/// Mod 管理器核心 —— 负责 Mod 的扫描、加载、启用/禁用
/// </summary>
public class ModLoader
{
    private readonly List<ModEntry> _mods = new();
    private readonly string _modsDirectory;

    public IReadOnlyList<ModEntry> Mods => _mods.AsReadOnly();

    public event Action<ModEntry>? OnModStateChanged;
    public event Action<string>? OnLogMessage;

    public ModLoader(string modsDirectory)
    {
        _modsDirectory = modsDirectory;
    }

    /// <summary>
    /// 扫描 mods 目录，发现所有 Mod
    /// </summary>
    public void ScanMods()
    {
        // 保存当前已加载 Mod 的状态（扫描后恢复）
        var loadedStates = _mods
            .Where(m => m.LoadState == ModLoadState.Loaded)
            .ToDictionary(m => m.Id, m => (m.PluginInstance, m.IsEnabled));

        _mods.Clear();

        if (!Directory.Exists(_modsDirectory))
        {
            Directory.CreateDirectory(_modsDirectory);
            Log($"已创建 Mods 目录: {_modsDirectory}");
            return;
        }

        foreach (var dir in Directory.GetDirectories(_modsDirectory))
        {
            var mod = DiscoverMod(dir);
            if (mod != null)
            {
                // 恢复之前已加载的状态
                if (loadedStates.TryGetValue(mod.Id, out var state))
                {
                    mod.PluginInstance = state.PluginInstance;
                    mod.IsEnabled = state.IsEnabled;
                    mod.LoadState = ModLoadState.Loaded;
                }
                _mods.Add(mod);
                Log($"发现 Mod: {mod.Name} ({mod.Id})");
            }
        }

        // 按加载优先级排序
        _mods.Sort((a, b) => a.LoadPriority.CompareTo(b.LoadPriority));
        Log($"共发现 {_mods.Count} 个 Mod");
    }

    /// <summary>
    /// 从文件夹发现 Mod 信息
    /// </summary>
    private ModEntry? DiscoverMod(string folderPath)
    {
        var dirName = Path.GetFileName(folderPath);

        var entryDll = Directory.GetFiles(folderPath, "*.dll")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == dirName)
            ?? Directory.GetFiles(folderPath, "*.dll")
                .FirstOrDefault(f => !Path.GetFileNameWithoutExtension(f).Equals("StArray.ModManager", StringComparison.OrdinalIgnoreCase));

        if (entryDll == null) return null;

        try
        {
            var assembly = Assembly.LoadFrom(entryDll);

            // 扫描实现 IModPlugin 的类型
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IModPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (pluginType == null) return null;

            // 实例化以读取元数据
            var plugin = (IModPlugin)Activator.CreateInstance(pluginType)!;

            return new ModEntry
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Version = plugin.Version,
                Author = plugin.Author,
                Description = plugin.Description,
                LoadPriority = plugin.LoadPriority,
                Dependencies = plugin.Dependencies.ToList(),
                FolderPath = folderPath,
                EntryPoint = entryDll,
            };
        }
        catch (Exception ex)
        {
            Log($"无法读取 Mod 程序集 {dirName}: {ex.Message}", isError: true);
            return null;
        }
    }

    /// <summary>
    /// 加载指定的 Mod
    /// </summary>
    public bool LoadMod(ModEntry mod)
    {
        if (mod.LoadState == ModLoadState.Loaded)
        {
            Log($"{mod.Name} 已经加载");
            return true;
        }

        mod.LoadState = ModLoadState.Loading;
        mod.LoadError = null;
        OnModStateChanged?.Invoke(mod);

        try
        {
            // 先加载依赖
            foreach (var depId in mod.Dependencies)
            {
                var dep = _mods.FirstOrDefault(m => m.Id == depId);
                if (dep == null)
                {
                    throw new Exception($"缺少依赖: {depId}");
                }
                if (dep.LoadState != ModLoadState.Loaded)
                {
                    Log($"  └─ 自动加载依赖: {dep.Name}");
                    LoadMod(dep);
                }
            }

            // 加载入口程序集
            if (!string.IsNullOrEmpty(mod.EntryPoint) && File.Exists(mod.EntryPoint))
            {
                var assembly = Assembly.LoadFrom(mod.EntryPoint);
                Log($"{mod.Name} 程序集已加载: {assembly.GetName().Name}");

                // 查找 IModPlugin 实现并执行
                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IModPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (pluginType != null)
                {
                    var plugin = (IModPlugin)Activator.CreateInstance(pluginType)!;
                    mod.PluginInstance = plugin;
                    plugin.OnLoad();
                    Log($"{mod.Name} 插件入口已执行");
                }
            }
            else
            {
                // 没有入口 DLL 也算加载成功（纯数据 Mod）
                Log($"{mod.Name} 无入口程序集，作为数据 Mod 加载");
            }

            mod.IsEnabled = true;
            mod.LoadState = ModLoadState.Loaded;
            Log($"{mod.Name} 加载成功");
        }
        catch (Exception ex)
        {
            mod.LoadState = ModLoadState.Error;
            mod.LoadError = ex.Message;
            Log($"{mod.Name} 加载失败: {ex.Message}", isError: true);
        }

        OnModStateChanged?.Invoke(mod);
        return mod.LoadState == ModLoadState.Loaded;
    }

    /// <summary>
    /// 卸载指定的 Mod
    /// </summary>
    public void UnloadMod(ModEntry mod)
    {
        if (mod.LoadState != ModLoadState.Loaded) return;

        mod.PluginInstance?.OnUnload();
        mod.PluginInstance = null;
        mod.IsEnabled = false;
        mod.LoadState = ModLoadState.NotLoaded;
        Log($"{mod.Name} 已卸载");
        OnModStateChanged?.Invoke(mod);
    }

    /// <summary>
    /// 切换 Mod 启用状态
    /// </summary>
    public void ToggleMod(ModEntry mod)
    {
        if (mod.LoadState == ModLoadState.Loaded)
            UnloadMod(mod);
        else
            LoadMod(mod);
    }

    /// <summary>
    /// 添加一个新的 Mod 条目（手动创建）
    /// </summary>
    public ModEntry AddMod(ModEntry mod)
    {
        _mods.Add(mod);
        Log($"已添加 Mod: {mod.Name}");
        return mod;
    }

    /// <summary>
    /// 移除 Mod 条目
    /// </summary>
    public bool RemoveMod(ModEntry mod)
    {
        if (mod.LoadState == ModLoadState.Loaded)
            UnloadMod(mod);

        var removed = _mods.Remove(mod);
        if (removed)
            Log($"已移除 Mod: {mod.Name}");
        return removed;
    }

    private void Log(string message, bool isError = false)
    {
        OnLogMessage?.Invoke(isError ? $"[ERROR] {message}" : $"[INFO] {message}");
    }
}
