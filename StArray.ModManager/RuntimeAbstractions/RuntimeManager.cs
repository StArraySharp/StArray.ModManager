using System.Runtime.InteropServices;

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
        if (GetModuleHandle("mono-2.0-bdwgc.dll") != IntPtr.Zero ||
            GetModuleHandle("mono.dll") != IntPtr.Zero)
            Backend = RuntimeBackend.Mono;
        else if (GetModuleHandle("GameAssembly.dll") != IntPtr.Zero ||
                 GetModuleHandle("unityplayer.dll") != IntPtr.Zero)
            Backend = RuntimeBackend.Il2Cpp;
        else
            Backend = RuntimeBackend.None;
        return Backend;
    }

    /// <summary>手动指定后端（用于非 Unity 或不明确环境）</summary>
    public static void SetBackend(RuntimeBackend backend) => Backend = backend;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}

/// <summary>运行时后端类型</summary>
public enum RuntimeBackend
{
    None,
    Il2Cpp,
    Mono,
}
