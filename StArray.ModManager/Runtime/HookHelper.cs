using System.Runtime.InteropServices;

namespace StArray.ModManager.Runtime;

/// <summary>
/// Hook 辅助类 —— 提供与平台无关的静态 Hook 操作入口，
/// 供 <c>HookGenerator</c> 生成的代码调用。
/// 平台初始化时需设置 <see cref="Instance"/>。
/// </summary>
public static class HookHelper
{
    private static readonly AsyncLocal<string?> CurrentOwner = new();

    /// <summary>当前平台的 Hook 实现（Windows = MinHook，Android = Dobby）</summary>
    public static IHook? Instance { get; set; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern nint GetProcAddress(nint hModule, string lpProcName);

    /// <summary>安装 Hook</summary>
    public static nint Hook(nint target, nint detour)
    {
        var provider = Instance;
        if (provider == null) return nint.Zero;
        return provider.Hook(target, detour, CurrentOwner.Value);
    }

    /// <summary>卸载 Hook</summary>
    public static bool Unhook(nint target)
    {
        var provider = Instance;
        // Without a provider we cannot know whether the native hook is still
        // live. Generated hooks must retain their callback in this case.
        if (provider == null) return false;
        if (!provider.SupportsRuntimeUnhook) return false;
        return provider.Unhook(target);
    }

    /// <summary>为当前 Mod 的 hook 安装设置诊断 owner。</summary>
    internal static IDisposable EnterOwnerScope(string owner)
    {
        var previous = CurrentOwner.Value;
        CurrentOwner.Value = owner;
        return new OwnerScope(previous);
    }

    /// <summary>获取库导出函数地址</summary>
    public static nint GetFunction(string library, string name)
    {
        if (Instance != null)
            return Instance.GetFunction(library, name);

        // Instance == null 时的降级路径
        return GetFunctionFallback(library, name);
    }

    /// <summary>获取库基址 + RVA 对应的绝对地址</summary>
    public static nint GetFunctionRVA(string library, long rva)
    {
        if (Instance != null)
        {
            var addr = Instance.GetFunctionRVA(library, rva);
            if (addr != nint.Zero) return addr;
        }

        return GetFunctionRVAFallback(library, rva);
    }

    /// <summary>降级路径（无 Instance 时使用）</summary>
    public static nint GetFunctionFallback(string library, string name)
    {
        // 1) GetModuleHandle 获取已加载的模块
        var dllName = library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? library : library + ".dll";
        var mod = GetModuleHandle(dllName);
        if (mod != nint.Zero)
        {
            var addr = GetProcAddress(mod, name);
            if (addr != nint.Zero) return addr;
        }

        // 2) 从 dlls/ 加载
        var dllsPath = Path.Combine(AppContext.BaseDirectory, "dlls", dllName);
        if (File.Exists(dllsPath) && NativeLibrary.TryLoad(dllsPath, out var lib))
            return NativeLibrary.GetExport(lib, name);

        // 3) 从输出目录根加载
        var rootPath = Path.Combine(AppContext.BaseDirectory, dllName);
        if (File.Exists(rootPath) && NativeLibrary.TryLoad(rootPath, out lib))
            return NativeLibrary.GetExport(lib, name);

        // 4) 回退标准 TryLoad
        if (NativeLibrary.TryLoad(library, out lib))
            return NativeLibrary.GetExport(lib, name);
        return nint.Zero;
    }

    /// <summary>降级 RVA 解析路径</summary>
    public static nint GetFunctionRVAFallback(string library, long rva)
    {
        var dllName = library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? library : library + ".dll";

        var mod = GetModuleHandle(dllName);
        if (mod != nint.Zero)
            return mod + (nint)rva;

        var localPath = Path.Combine(AppContext.BaseDirectory, dllName);
        if (File.Exists(localPath) && NativeLibrary.TryLoad(localPath, out _))
        {
            mod = GetModuleHandle(dllName);
            if (mod != nint.Zero)
                return mod + (nint)rva;
        }

        var dllsPath = Path.Combine(AppContext.BaseDirectory, "dlls", dllName);
        if (File.Exists(dllsPath) && NativeLibrary.TryLoad(dllsPath, out _))
        {
            mod = GetModuleHandle(dllName);
            if (mod != nint.Zero)
                return mod + (nint)rva;
        }

        if (NativeLibrary.TryLoad(library, out _))
        {
            mod = GetModuleHandle(dllName);
            if (mod != nint.Zero)
                return mod + (nint)rva;
        }

        return nint.Zero;
    }

    private sealed class OwnerScope(string? previous) : IDisposable
    {
        private readonly string? _previous = previous;
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                CurrentOwner.Value = _previous;
        }
    }
}
