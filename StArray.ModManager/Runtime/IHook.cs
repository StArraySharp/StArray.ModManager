namespace StArray.ModManager.Runtime;

/// <summary>
/// 原生方法 Hook 抽象 —— 支持安装/卸载 inline hook 及获取库导出函数
/// </summary>
public interface IHook
{
    /// <summary>安装 hook，将 target 重定向到 detour，返回原始函数指针</summary>
    nint Hook(nint target, nint detour);

    /// <summary>卸载 target 上的 hook</summary>
    bool Unhook(nint target);

        /// <summary>从指定库获取导出函数地址</summary>
    nint GetFunction(string library, string name);

    /// <summary>从指定库计算 RVA（相对虚拟地址）对应的绝对地址</summary>
    nint GetFunctionRVA(string library, long rva)
    {
        return nint.Zero;
    }
}
