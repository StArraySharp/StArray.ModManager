using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

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
        if (OperatingSystem.IsWindows())
        {
            if (GetModuleHandle("mono-2.0-bdwgc.dll") != IntPtr.Zero ||
                GetModuleHandle("mono.dll") != IntPtr.Zero)
                Backend = RuntimeBackend.Mono;
            else if (GetModuleHandle("GameAssembly.dll") != IntPtr.Zero ||
                     GetModuleHandle("unityplayer.dll") != IntPtr.Zero)
                Backend = RuntimeBackend.Il2Cpp;
        }
        else if (OperatingSystem.IsAndroid())
        {
            if (dlopen("libmono.so", RTLD_NOLOAD) != IntPtr.Zero ||
                dlopen("libmonobdwgc-2.0.so", RTLD_NOLOAD) != IntPtr.Zero)
                Backend = RuntimeBackend.Mono;
            else if (dlopen("libil2cpp.so", RTLD_NOLOAD) != IntPtr.Zero)
                Backend = RuntimeBackend.Il2Cpp;
        }
        else if (OperatingSystem.IsLinux())
        {
            if (dlopen("libmono-2.0.so.1", RTLD_NOLOAD) != IntPtr.Zero ||
                dlopen("libmono.so", RTLD_NOLOAD) != IntPtr.Zero)
                Backend = RuntimeBackend.Mono;
            else if (dlopen("libil2cpp.so", RTLD_NOLOAD) != IntPtr.Zero)
                Backend = RuntimeBackend.Il2Cpp;
        }
        else
        {
            Backend = RuntimeBackend.None;
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

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("libdl", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen(string filename, int flags);

    /// <summary>dlopen 标志：仅检查是否已加载，不实际加载</summary>
    private const int RTLD_NOLOAD = 0x0002;
}

/// <summary>运行时后端类型</summary>
public enum RuntimeBackend
{
    None,
    Il2Cpp,
    Mono,
}
