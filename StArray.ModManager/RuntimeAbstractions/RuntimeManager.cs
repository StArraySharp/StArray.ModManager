using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;
using StArray.ModManager.Native;

namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>游戏运行时后端（Mono / Il2Cpp）自动检测与统一入口</summary>
public static class RuntimeManager
{
    /// <summary>检测到的后端类型</summary>
    public static RuntimeBackend Backend { get; private set; } = RuntimeBackend.None;

    /// <summary>是否已成功检测</summary>
    public static bool IsAvailable => Backend != RuntimeBackend.None;

    /// <summary>是否 Mono 后端</summary>
    public static bool IsMono => Backend == RuntimeBackend.Mono;

    /// <summary>是否 Il2Cpp 后端</summary>
    public static bool IsIl2Cpp => Backend == RuntimeBackend.Il2Cpp;

    /// <summary>自动检测当前进程加载了哪个运行时 DLL</summary>
    public static RuntimeBackend Detect()
    {
        Backend = RuntimeBackend.None;
        if (OperatingSystem.IsWindows())
        {
            // 优先探测进程内已加载的 mono 运行时（覆盖 mono 宿主进程，
            // 如 MSYS2 mono 的 libmonosgen-2.0.dll、游戏嵌入的 mono-2.0-bdwgc.dll）。
            var monoLib = ProbeLoadedMonoLibrary();
            if (monoLib != null)
            {
                Backend = RuntimeBackend.Mono;
                MonoFunctions.SetMonoLibraryPath(monoLib);
            }
            else if (File.Exists(Path.Combine(AppContext.BaseDirectory,"..","..","GameAssembly.dll")))
            {
                Backend = RuntimeBackend.Il2Cpp;
            }
            else if (File.Exists(Path.Combine(AppContext.BaseDirectory,"..","..","MonoBleedingEdge","EmbedRuntime","mono-2.0-bdwgc.dll")))
            {
                Backend = RuntimeBackend.Mono;
                MonoFunctions.SetMonoLibraryPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "MonoBleedingEdge", "EmbedRuntime", "mono-2.0-bdwgc.dll"));
            }
        }
        else if (OperatingSystem.IsAndroid())
        {
            if (IsUnixLibraryLoaded("libmono.so") ||
                IsUnixLibraryLoaded("libmonobdwgc-2.0.so"))
                Backend = RuntimeBackend.Mono;
            else if (IsUnixLibraryLoaded("libil2cpp.so"))
                Backend = RuntimeBackend.Il2Cpp;
        }
        else if (OperatingSystem.IsLinux())
        {
            if (IsUnixLibraryLoaded("libmono-2.0.so.1") ||
                IsUnixLibraryLoaded("libmono.so"))
                Backend = RuntimeBackend.Mono;
            else if (IsUnixLibraryLoaded("libil2cpp.so"))
                Backend = RuntimeBackend.Il2Cpp;
        }
        return Backend;
    }

    /// <summary>手动指定后端（用于非 Unity 或不明确环境）</summary>
    public static void SetBackend(RuntimeBackend backend) => Backend = backend;

    /// <summary>获取当前后端的 AppDomain</summary>
    public static IAppDomain? GetDomain()
    {
        if (IsIl2Cpp)
        {
            var domain = Il2CppDomain.Current;
            return domain;
        }
        if (IsMono)
        {
            var domain = MonoDomain.Current;
            return domain;
        }
        return null;
    }

    /// <summary>
    /// 从运行时对象取得其真实类型。
    /// </summary>
    /// <remarks>
    /// 泛型容器只有在对象实例创建后才能拿到具体方法表。调用方可缓存返回类型的
    /// Add/Clear 等方法，避免每次事件通过字符串重新解析。
    /// </remarks>
    public static IRuntimeClass? GetObjectClass(nint objectPtr)
    {
        if (objectPtr == 0)
            return null;

        if (IsIl2Cpp)
        {
            nint klass = Il2CppFunctions.il2cpp_object_get_class(objectPtr);
            return klass == 0 ? null : new Il2CppClass(klass);
        }

        if (IsMono)
        {
            nint klass = MonoFunctions.MonoObjectGetClass(objectPtr);
            return klass == 0 ? null : new MonoClass(klass);
        }

        return null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetModuleFileNameW(IntPtr hModule,
        [Out] System.Text.StringBuilder lpFilename, int nSize);

    /// <summary>进程内已加载模块的候选 mono 库名（基名匹配，不含路径）。</summary>
    private static readonly string[] WindowsMonoCandidates =
    [
        "mono-2.0-bdwgc.dll",   // Unity 嵌入式（BDWGC）
        "mono-2.0-sgen.dll",    // Unity 嵌入式（SGen）
        "monosgen-2.0.dll",     // MSYS2 / 独立 mono
        "libmonosgen-2.0.dll",  // MSYS2 带前缀变体
    ];

    /// <summary>
    /// 探测进程内已加载的 mono 运行时，返回其完整路径；未加载返回 null。
    /// GetModuleHandle 对未加载模块仅返回零，不加载新库、无副作用。
    /// </summary>
    private static string? ProbeLoadedMonoLibrary()
    {
        foreach (var candidate in WindowsMonoCandidates)
        {
            var h = GetModuleHandle(candidate);
            if (h == IntPtr.Zero) continue;

            var sb = new System.Text.StringBuilder(1024);
            return GetModuleFileNameW(h, sb, sb.Capacity) > 0 ? sb.ToString() : candidate;
        }
        return null;
    }

    // libdl 相关调用统一走 Native.DL（RuntimeManager 不再自带 dlopen/dlclose P/Invoke）
    internal const int RtldNow = 0x0002;
    internal const int RtldNoLoad = 0x0004;

    private static bool IsUnixLibraryLoaded(string filename)
        => ProbeUnixLibrary(
            filename,
            (name, flags) => DL.Open(
                name, (DL.RTLDFlags)(flags)), // ProbeUnixLibrary 只传 NOW|NOLOAD，直接透传
            handle => _ = DL.Close(handle));

    internal static bool ProbeUnixLibrary(
        string filename,
        Func<string, int, nint> open,
        Action<nint> close)
    {
        var handle = open(filename, RtldNow | RtldNoLoad);
        if (handle == 0) return false;
        try
        {
            return true;
        }
        finally
        {
            close(handle);
        }
    }
}

/// <summary>运行时后端类型</summary>
public enum RuntimeBackend
{
    None,
    Il2Cpp,
    Mono,
}
