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
            .ToDictionary(m => m.Id, m => (m.PluginInstance, m.IsEnabled, m.LoadContext));

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
                    mod.LoadContext = state.LoadContext;
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
            // 探测性元数据读取走临时 ALC，读完即弃：既不锁文件，也不把类型
            // 留在默认上下文（否则后续正式加载会出现重复类型/版本冲突）。
            var probe = new ModLoadContext($"Probe:{dirName}", folderPath, _managerDir);
            try
            {
                // 探测只读元数据：流式加载即可（不锁文件；Location 无人消费）
                var assembly = probe.LoadMetadataFromPath(entryDll);

                var pluginType = ResolvePluginType(assembly);
                if (pluginType == null)
                {
                    // 静默跳过会让“共发现 0 个 Mod”难以定位，这里留下原因
                    Logger.Warn(nameof(ModLoader),
                        $"No IModPlugin type in {Path.GetFileName(entryDll)} ({dirName}); skipped");
                    return null;
                }

                // 实例化以读取元数据（Dependencies 允许实现返回 null —— 接口不变，这里宽容处理）
                var plugin = (IModPlugin)Activator.CreateInstance(pluginType)!;

                return new ModEntry
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Version = plugin.Version,
                    Author = plugin.Author,
                    Description = plugin.Description,
                    Dependencies = plugin.Dependencies?.ToList() ?? new List<string>(),
                    FolderPath = folderPath,
                    EntryPoint = entryDll,
                };
            }
            finally
            {
                probe.Unload();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(nameof(ModLoader), L10n.Get("Log_ModAssemblyError", dirName, ex.Message));
            return null;
        }
    }

    /// <summary>
    /// 加载指定的 Mod：从磁盘字节读入独立 ALC（不锁文件、可卸载）。
    /// 依赖的 mod 由各自 ALC 承载，卸载时按反向依赖级联。
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
            // 依赖检查：依赖缺失报错；未加载则先递归加载（各自的 ALC）
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
                    if (!LoadMod(dep))
                        throw new Exception($"dependency failed to load: {dep.Name}");
                }
            }

            // 加载入口程序集到本 mod 的可卸载 ALC
            if (!string.IsNullOrEmpty(mod.EntryPoint) && File.Exists(mod.EntryPoint))
            {
                var alc = new ModLoadContext(
                    $"Mod:{mod.Id}",
                    Path.GetDirectoryName(mod.EntryPoint)!,
                    _managerDir);

                var assembly = alc.LoadFromFilePath(mod.EntryPoint);
                var pluginType = ResolvePluginType(assembly);
                if (pluginType != null)
                {
                    var plugin = (IModPlugin)Activator.CreateInstance(pluginType)!;
                    mod.PluginInstance = plugin;
                    mod.LoadContext = alc;
                    plugin.OnLoad();

                    if (plugin is IModSettings s)
                        ModManagerUI.LoadSettings(mod, s);
                }
                else
                {
                    // 无插件类型的程序集不保留 ALC
                    alc.Unload();
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
    /// 卸载指定的 Mod，并级联卸载所有（直接/间接）依赖它的 mod。
    /// </summary>
    public void UnloadMod(ModEntry mod)
    {
        if (mod.LoadState != ModLoadState.Loaded) return;

        // 反向依赖闭包：a 依赖 b 时，卸 b 必须先卸 a。
        var dependents = _mods
            .Where(m => m.LoadState == ModLoadState.Loaded && DependsOn(m, mod.Id))
            .ToList();
        foreach (var dependent in dependents)
        {
            Logger.Info(nameof(ModLoader), $"  cascade unload: {dependent.Name} (depends on {mod.Name})");
            UnloadMod(dependent);
        }

        mod.PluginInstance?.OnUnload();
        mod.PluginInstance = null;
        mod.LoadContext?.UnloadAndTrack(); // ALC 异步回收；调用方已无强引用即可
        mod.LoadContext = null;
        mod.IsEnabled = false;
        mod.LoadState = ModLoadState.NotLoaded;
        Logger.Info(nameof(ModLoader), $"{mod.Name} 已卸载");
        OnModStateChanged?.Invoke(mod);
    }

    /// <summary>m 的依赖闭包（直接+间接）里是否含 targetId。</summary>
    private bool DependsOn(ModEntry m, string targetId)
    {
        if (m.Dependencies.Contains(targetId)) return true;
        return m.Dependencies
            .Select(id => _mods.FirstOrDefault(x => x.Id == id))
            .Where(d => d != null)
            .Any(d => DependsOn(d!, targetId));
    }

    /// <summary>管理器目录（SMM 依赖兜底解析），由构造传入。</summary>
    private readonly string? _managerDir;

    /// <summary>创建 ModLoader 并指定 Mods 目录</summary>
    public ModLoader(string modsDirectory, string? managerDir = null)
    {
        _modsDirectory = modsDirectory;
        _managerDir = managerDir;
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
