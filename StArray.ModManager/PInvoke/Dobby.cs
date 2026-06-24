using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StArray.ModManager.PInvoke;

/// <summary>
/// Dobby Hook P/Invoke 封装。
/// 对应 native 端的 <c>core/dobby_hook.cpp</c>，导出 C ABI 函数。
/// 所有方法通过 <c>[DllImport("modmanager")]</c> 调用 <c>libmodmanager.so</c>。
/// </summary>
public static class Dobby
{
    private const string Lib = "modmanager";

    // ========================================================================
    // Native externs
    // ========================================================================

    /// <summary>安装 inline hook。</summary>
    /// <param name="address">目标函数地址</param>
    /// <param name="replace">替换函数地址（nint 指向 delegate 或函数指针）</param>
    /// <param name="origin">[out] 原函数指针（用于调用原逻辑）</param>
    /// <returns>0 = 成功，非 0 = 失败</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_hook")]
    public static extern int Hook(nint address, nint replace, out nint origin);

    /// <summary>
    /// 安装 inline hook — 支持直接传入 UnityResolve.MethodInfo。
    /// 方法会被先 JIT 编译，再取其函数指针作为 replace 调用 Hook。
    /// </summary>
    /// <param name="address">目标函数地址</param>
    /// <param name="replaceMethod">替换方法（UnityResolve.MethodInfo，需为静态方法）</param>
    /// <param name="origin">[out] 原函数指针</param>
    /// <returns>0 = 成功，非 0 = 失败</returns>
    public static int Hook(nint address, UnityResolve.Method replaceMethod, out nint origin)
    {
        if (replaceMethod == null || !replaceMethod.IsValid)
        {
            origin = IntPtr.Zero;
            return -1;
        }
        replaceMethod.Compile();
        return Hook(address, replaceMethod.FunctionPtr, out origin);
    }

    /// <summary>
    /// 安装 inline hook — 传入 C# Reflection MethodInfo。
    /// 方法会被 PrepareMethod 强制 JIT 编译，再取其函数指针作为 replace。
    /// </summary>
    /// <param name="address">目标函数地址</param>
    /// <param name="replaceMethod">C# MethodInfo（需为静态方法）</param>
    /// <param name="origin">[out] 原函数指针</param>
    /// <returns>0 = 成功，非 0 = 失败</returns>
    public static int Hook(nint address, MethodInfo replaceMethod, out nint origin)
    {
        if (replaceMethod == null)
        {
            origin = IntPtr.Zero;
            return -1;
        }
        // 强制 JIT 编译该方法
        RuntimeHelpers.PrepareMethod(replaceMethod.MethodHandle);
        nint replacePtr = replaceMethod.MethodHandle.GetFunctionPointer();
        return Hook(address, replacePtr, out origin);
    }

    /// <summary>安装动态指令插桩。</summary>
    /// <param name="address">目标函数地址</param>
    /// <param name="preHandler">前置回调函数指针（dobby_instrument_callback_t 签名）</param>
    /// <returns>0 = 成功</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_instrument")]
    public static extern int Instrument(nint address, nint preHandler);

    /// <summary>移除 hook 并恢复原函数。</summary>
    /// <param name="address">被 hook 的函数地址</param>
    /// <returns>0 = 成功</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_destroy")]
    public static extern int Destroy(nint address);

    /// <summary>按动态库名和符号名解析函数地址。</summary>
    /// <param name="imageName">动态库名，如 "libil2cpp.so"</param>
    /// <param name="symbolName">符号名</param>
    /// <returns>符号地址，失败返回 nint.Zero</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_symbol_resolver")]
    public static extern nint SymbolResolver(string imageName, string symbolName);

    /// <summary>内存代码补丁。</summary>
    /// <param name="address">目标地址</param>
    /// <param name="buffer">补丁数据</param>
    /// <param name="bufferSize">补丁数据大小</param>
    /// <returns>0 = 成功</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_code_patch")]
    public static extern int CodePatch(nint address, byte[] buffer, uint bufferSize);

    /// <summary>获取 Dobby 版本字符串。</summary>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_get_version")]
    private static extern nint _GetVersionRaw();

    /// <summary>获取 Dobby 版本字符串。</summary>
    public static string GetVersion()
    {
        var ptr = _GetVersionRaw();
        return Marshal.PtrToStringAnsi(ptr) ?? "unknown";
    }
}
