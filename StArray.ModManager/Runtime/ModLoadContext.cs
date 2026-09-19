using System.Reflection;
using System.Runtime.Loader;

namespace StArray.ModManager.Runtime;

/// <summary>
/// 每个 Mod 独立的可卸载 AssemblyLoadContext。
/// </summary>
/// <remarks>
/// 两种加载方式并存：
/// <list type="bullet">
/// <item><see cref="LoadFromFilePath"/>（正式加载）：<see cref="Assembly.Location"/>
/// 为真实路径，mod 可定位同目录资源；文件被内存映射锁定，直到 Unload + GC 回收
/// （collectible 下锁是暂时的）</item>
/// <item><see cref="LoadMetadataFromPath"/>（探测）：流式加载不锁文件，Location 为空</item>
/// <item>依赖解析（<see cref="AssemblyLoadContext.Load"/>）走路径加载，
/// 与主程序集一致；优先 mod 目录，其次管理器目录，系统库交默认上下文</item>
/// <item><see cref="Unload"/> 后（所有实例与 Type 释放）程序集真正卸载、文件解锁</item>
/// </list>
/// </remarks>
public sealed class ModLoadContext : AssemblyLoadContext
{
    private readonly string _baseDir;
    private readonly string? _managerDir;

    /// <summary>宿主上下文（SMM 本体所在的 ALC）——其中的程序集在 mod 侧必须保持同一身份。</summary>
    private static readonly AssemblyLoadContext HostContext =
        AssemblyLoadContext.GetLoadContext(typeof(ModLoadContext).Assembly) ?? AssemblyLoadContext.Default;

    /// <summary>mod 依赖解析失败时触发（诊断用）。</summary>
    public event Action<ModLoadContext, AssemblyName>? OnResolveFailure;

    public ModLoadContext(string name, string baseDir, string? managerDir = null)
        : base(name, isCollectible: true)
    {
        _baseDir = baseDir;
        _managerDir = managerDir;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 1) 宿主已加载的同名程序集必须复用同一实例。
        //    管理器目录里同样有 StArray.ModManager.dll，若直接从这里加载一份副本，
        //    mod 看到的 IModPlugin / ModEntryPointAttribute 就变成另一份类型，
        //    IsPluginType 恒为 false → 一个 Mod 都发现不了（Android/Windows 皆然）。
        if (TryFindHostAssembly(assemblyName.Name, out var hostAssembly))
            return hostAssembly;

        // 2) 只处理本 mod 目录内的程序集；系统/框架程序集返回 null 交给默认上下文，
        // 保证 mscorlib/System.* 与 SMM 本体的类型一致性（IModPlugin 接口可赋值）。
        var local = ProbeDir(_baseDir, assemblyName);
        if (local != null) return local;

        // 管理器目录（SMM 自带依赖，如 ImGui.NET）——不走默认上下文，
        // 避免与已加载版本因版本号差异重复加载。
        if (_managerDir != null)
        {
            var mgr = ProbeDir(_managerDir, assemblyName);
            if (mgr != null) return mgr;
        }

        return null;
    }

    private Assembly? ProbeDir(string dir, AssemblyName name)
    {
        var path = Path.Combine(dir, name.Name + ".dll");
        if (!File.Exists(path)) return null;

        try
        {
            return LoadFromAssemblyPath(Path.GetFullPath(path));
        }
        catch
        {
            return null; // 加载失败交回解析链（Resolving/默认上下文）
        }
    }

    /// <summary>
    /// 在宿主上下文（及默认上下文）中查找已加载的同名程序集。
    /// 命中则返回该实例，<b>绝不再从磁盘加载第二份</b> —— 类型身份一致是 mod 能被识别的
    /// 前提（<c>IModPlugin</c> 与宿主必须是同一个 Type）。
    /// </summary>
    private static bool TryFindHostAssembly(string? simpleName, out Assembly? assembly)
    {
        assembly = null;
        if (string.IsNullOrEmpty(simpleName)) return false;

        if (TryFind(HostContext, simpleName, out assembly)) return true;
        return !ReferenceEquals(HostContext, AssemblyLoadContext.Default)
            && TryFind(AssemblyLoadContext.Default, simpleName, out assembly);
    }

    private static bool TryFind(AssemblyLoadContext context, string simpleName, out Assembly? assembly)
    {
        try
        {
            foreach (var loaded in context.Assemblies)
            {
                if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                {
                    assembly = loaded;
                    return true;
                }
            }
        }
        catch (InvalidOperationException)
        {
            // 上下文正在卸载，忽略
        }

        assembly = null;
        return false;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        // 原生库不经 ALC：交由 NativeLibraryResolver / OS 搜索路径处理
        return nint.Zero;
    }

    /// <summary>
    /// 从磁盘路径文件映射加载（<see cref="AssemblyLoadContext.LoadFromAssemblyPath"/>）：
    /// <see cref="Assembly.Location"/> 为真实路径，mod 可用它定位同目录资源。
    /// </summary>
    /// <remarks>
    /// 代价：文件被内存映射锁定，直到 <see cref="Unload"/> 且 ALC 被 GC 回收。
    /// collectible ALC 下锁是暂时的（对比默认上下文的永久锁）；重新读同一文件
    /// （卸载后重载）不受影响——只有覆盖写（如重新生成 DLL）需要先卸载并等待回收。
    /// </remarks>
    public Assembly LoadFromFilePath(string path)
        => LoadFromAssemblyPath(Path.GetFullPath(path));

    /// <summary>
    /// 从磁盘路径流式加载（不锁文件，<see cref="Assembly.Location"/> 为空）。
    /// 适用于只需读元数据的探测场景。
    /// </summary>
    public Assembly LoadMetadataFromPath(string path)
    {
        using var fs = File.OpenRead(path);
        return LoadFromStream(fs);
    }

    /// <summary>显式暴露依赖解析失败（挂到日志）。</summary>
    internal void RaiseResolveFailure(AssemblyName name) => OnResolveFailure?.Invoke(this, name);

    /// <summary>
    /// 触发卸载并保留弱引用追踪：调用方应同步放弃所有强引用（PluginInstance、
    /// LoadContext、事件订阅、缓存 Type），否则 collectible ALC 永不回收。
    /// </summary>
    public WeakReference UnloadAndTrack() => new(this, trackResurrection: true);
}
