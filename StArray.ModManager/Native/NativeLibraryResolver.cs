using System.Reflection;
using System.Runtime.InteropServices;

namespace StArray.ModManager.Native;

/// <summary>
/// 原生库解析核心：基于 <see cref="NativeLibrary.SetDllImportResolver"/> 的统一转发器。
/// </summary>
/// <remarks>
/// .NET 对每个程序集只允许注册一个 DllImportResolver（重复注册抛
/// <see cref="InvalidOperationException"/>）。本类把"每程序集一个"的名额收敛到这里，
/// 对外暴露事件：任何模块想参与库解析，只需 <c>ResolveRequested +=</c> 订阅，
/// 不再各自抢注册权。析构顺序无关，订阅/退订随时可做。
/// </remarks>
public static class NativeLibraryResolver
{
    /// <summary>库解析请求（libraryName, assembly）→ 返回库句柄；返回 0 表示"本订阅不处理"。</summary>
    public static event Func<string, Assembly, IntPtr>? ResolveRequested;

    private static readonly Lock _lock = new();
    private static readonly HashSet<Assembly> _registered = [];

    /// <summary>为指定程序集接入统一解析（幂等，可对任意多个程序集重复调用）。</summary>
    public static void Install(Assembly assembly)
    {
        lock (_lock)
        {
            if (!_registered.Add(assembly)) return;
            NativeLibrary.SetDllImportResolver(assembly, Resolve);
        }
    }

    /// <summary>为包含类型 <typeparamref name="T"/> 的程序集接入统一解析。</summary>
    public static void Install<T>() => Install(typeof(T).Assembly);

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        var handlers = ResolveRequested;
        if (handlers is null) return IntPtr.Zero;

        // 逐个询问订阅方；任何一个给出非零句柄即解析成功。
        // 事件委托的返回值不会自动串联（多播委托只保留最后一个返回值），
        // 所以必须手动 GetInvocationList 逐个调用。
        foreach (Func<string, Assembly, IntPtr> handler in handlers.GetInvocationList())
        {
            try
            {
                var handle = handler(libraryName, assembly);
                if (handle != IntPtr.Zero) return handle;
            }
            catch
            {
                // 单个订阅方出错不影响其他订阅方与默认解析流程
            }
        }

        return IntPtr.Zero; // 交还默认解析
    }
}
