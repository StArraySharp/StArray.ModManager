using StArray.ModManager.Resources;
using System.Reflection;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Runtime;

/// <summary>Mod 管理器核心 / Mod loader — scan, load, enable/disable mods</summary>
public class ModLoader
{
    private readonly List<ModEntry> _mods = new();
    private readonly string _modsDirectory;

    /// <summary>已发现的 Mod 列表（只读）</summary>
    public IReadOnlyList<ModEntry> Mods => _mods.AsReadOnly();
    /// <summary>Mods 目录路径</summary>
    public string ModsDirectory
    {
        get => _modsDirectory;
        set => throw new NotSupportedException("ModsDirectory is set via constructor only");
    }

    /// <summary>Mod 状态变更事件</summary>
    public event Action<ModEntry>? OnModStateChanged;

    /// <summary>创建 ModLoader 并指定 Mods 目录</summary>
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
            Logger.Info(nameof(ModLoader), L10n.Get("Log_DirCreated", _modsDirectory));
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
                Logger.Info(nameof(ModLoader), L10n.Get("Log_ModFound", mod.Name, mod.Id));
            }
        }

        Logger.Info(nameof(ModLoader), L10n.Get("Log_ModCount", _mods.Count));
    }

    /// <summary>
    /// 找出程序集里的 <see cref="IModPlugin"/> 实现类型。
    /// 优先读 <see cref="ModEntryPointAttribute"/>：生成的存根程序集有成千上万个类型，
    /// 全量扫描既慢、又会因其中任何一个类型加载失败而整体抛出。
    /// 没有该标注时回退到扫描，并容忍部分类型加载失败。
    /// </summary>
    private static Type? ResolvePluginType(Assembly assembly)
    {
        try
        {
            if (assembly.GetCustomAttribute<ModEntryPointAttribute>()?.PluginType is { } declared &&
                IsPluginType(declared))
                return declared;
        }
        catch (Exception ex)
        {
            // 标注指向的类型解析不了就走扫描，不致命
            Logger.Warn(nameof(ModLoader), $"ModEntryPoint attribute unusable: {ex.Message}");
        }

        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        return types.FirstOrDefault(t => t != null && IsPluginType(t));
    }

    private static bool IsPluginType(Type t) =>
        typeof(IModPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract;

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
            // 字节加载：Assembly.LoadFrom(path) 会映射并锁定文件，导致后续重新生成
            // mod DLL 时 IOException（旧实例还占着句柄）。从内存加载则不锁文件。
            var assembly = Assembly.Load(File.ReadAllBytes(entryDll));

            var pluginType = ResolvePluginType(assembly);
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
                Dependencies = plugin.Dependencies.ToList(),
                FolderPath = folderPath,
                EntryPoint = entryDll,
            };
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ModLoader), L10n.Get("Log_ModAssemblyError", dirName, ex.Message));
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
            Logger.Info(nameof(ModLoader), $"{mod.Name} 已经加载");
            return true;
        }

        mod.LoadState = ModLoadState.Loading;
        mod.LoadError = null;
        OnModStateChanged?.Invoke(mod);

        try
        {
            // 依赖检查
            foreach (var depId in mod.Dependencies)
            {
                var dep = _mods.FirstOrDefault(m => m.Id == depId);
                if (dep == null)
                {
                    throw new Exception(L10n.Get("Log_MissingDep", depId));
                }
                if (dep.LoadState != ModLoadState.Loaded)
                {
                    Logger.Info(nameof(ModLoader), $"  load dep: {dep.Name}");
                    LoadMod(dep);
                }
            }

            // 加载入口程序集
            if (!string.IsNullOrEmpty(mod.EntryPoint) && File.Exists(mod.EntryPoint))
            {
                // 字节加载，避免锁定 DLL 文件（同 DiscoverMod 处的说明）
                var assembly = Assembly.Load(File.ReadAllBytes(mod.EntryPoint));

                var pluginType = ResolvePluginType(assembly);
                if (pluginType != null)
                {
                    var plugin = (IModPlugin)Activator.CreateInstance(pluginType)!;
                    mod.PluginInstance = plugin;
                    plugin.OnLoad();

                    if (plugin is IModSettings s)
                        ModManagerUI.LoadSettings(mod, s);
                }
            }

            mod.IsEnabled = true;
            mod.LoadState = ModLoadState.Loaded;
            Logger.Info(nameof(ModLoader), $"{mod.Name} 加载成功");
        }
        catch (Exception ex)
        {
            mod.LoadState = ModLoadState.Error;
            mod.LoadError = ex.Message;
            Logger.Error(nameof(ModLoader), $"{mod.Name} 加载失败: {ex}");
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
        Logger.Info(nameof(ModLoader), $"{mod.Name} 已卸载");
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
        Logger.Info(nameof(ModLoader), L10n.Get("Log_ModAdded", mod.Name));
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
            Logger.Info(nameof(ModLoader), L10n.Get("Log_ModRemoved", mod.Name));
        return removed;
    }

}
