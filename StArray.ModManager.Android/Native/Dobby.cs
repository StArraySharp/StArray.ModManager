using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

/// <summary>
/// Dobby Hook P/Invoke 封装。
/// 对应 native 端的 <c>core/dobby_hook.cpp</c>，导出 C ABI 函数。
/// 所有方法通过 <c>[DllImport("modmanager")]</c> 调用 <c>libmodmanager.so</c>。
/// </summary>
public static class Dobby
{
    private const string Lib = "modmanager";
    private static readonly object HookLock = new();
    private static readonly Dictionary<HookKey, HookRecord> InstalledHooks = new();
    private static readonly Dictionary<nint, HookChain> HookChains = new();
    private static readonly Dictionary<nint, nint> DetourTargets = new();
    private static readonly Dictionary<nint, HookRecord> LatestHooks = new();

    private readonly record struct HookKey(nint Target, nint Detour);

    private sealed class HookChain(nint target)
    {
        public nint Target { get; } = target;
        public nint Head { get; set; }
        public List<HookRecord> Layers { get; } = new();
    }

    private sealed record HookRecord(
        nint Target,
        nint PatchPoint,
        nint Detour,
        nint Origin,
        string Owner);

    // ========================================================================
    // Native externs
    // ========================================================================

    /// <summary>安装 inline hook。</summary>
    /// <param name="address">目标函数地址</param>
    /// <param name="replace">替换函数地址（nint 指向 delegate 或函数指针）</param>
    /// <param name="origin">[out] 原函数指针（用于调用原逻辑）</param>
    /// <returns>0 = 成功，非 0 = 失败</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_hook", CallingConvention = CallingConvention.Cdecl)]
    private static extern int InstallNativeHook(nint address, nint replace, out nint origin);

    /// <summary>
    /// 安装 inline hook，并保持旧版 API 兼容。
    /// 同一目标的后续 detour 会安装到上一层 detour 上，形成托管侧维护的
    /// Hook 链，不依赖新版本 native HookBroker 导出。
    /// </summary>
    public static int Hook(nint address, nint replace, out nint origin)
        => Hook(address, replace, out origin, "Dobby.Hook");

    /// <summary>安装带 owner 诊断信息的 Hook。</summary>
    public static int Hook(nint address, nint replace, out nint origin, string? owner)
    {
        origin = nint.Zero;
        if (address == nint.Zero || replace == nint.Zero)
            return -1;
        if (address == replace)
            return -2;

        lock (HookLock)
        {
            var key = new HookKey(address, replace);
            if (InstalledHooks.TryGetValue(key, out var existing))
            {
                origin = existing.Origin;
                return 0;
            }

            // A detour already used as another chain's layer is a patched
            // entry point, not an independent target. Patching it as a new
            // root would make the two managed registries disagree.
            if (DetourTargets.ContainsKey(address))
                return -3;

            if (DetourTargets.TryGetValue(replace, out var boundTarget) &&
                boundTarget != address)
                return -3;

            HookChains.TryGetValue(address, out var chain);
            var patchPoint = chain?.Head ?? address;
            if (patchPoint == nint.Zero || patchPoint == replace)
                return -2;

            var result = InstallNativeHook(patchPoint, replace, out origin);
            if (result != 0)
            {
                origin = nint.Zero;
                return result;
            }

            // A successful native hook must always provide a continuation. Do
            // not publish an incomplete layer to the process-wide registry.
            if (origin == nint.Zero)
            {
                RemoveNativeHook(patchPoint);
                return -4;
            }

            chain ??= new HookChain(address);
            if (!HookChains.ContainsKey(address))
                HookChains.Add(address, chain);

            var record = new HookRecord(
                address,
                patchPoint,
                replace,
                origin,
                string.IsNullOrWhiteSpace(owner) ? "unknown" : owner);
            chain.Head = replace;
            chain.Layers.Add(record);
            InstalledHooks[key] = record;
            DetourTargets[replace] = address;
            LatestHooks[address] = record;
            return 0;
        }
    }

    /// <summary>获取目标地址最近安装的 Hook 层，供诊断使用。</summary>
    public static bool TryGetInstalledHook(
        nint address,
        out string owner,
        out nint detour,
        out nint origin)
    {
        lock (HookLock)
        {
            if (LatestHooks.TryGetValue(address, out var existing))
            {
                owner = existing.Owner;
                detour = existing.Detour;
                origin = existing.Origin;
                return true;
            }
        }

        owner = string.Empty;
        detour = nint.Zero;
        origin = nint.Zero;
        return false;
    }

    /// <summary>获取 C# Hook 链的层数。</summary>
    public static int GetLayerCount(nint address)
    {
        lock (HookLock)
            return HookChains.TryGetValue(address, out var chain)
                ? chain.Layers.Count
                : 0;
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
        var owner = replaceMethod.DeclaringType == null
            ? replaceMethod.Name
            : replaceMethod.DeclaringType.FullName + "." + replaceMethod.Name;
        return Hook(address, replacePtr, out origin, owner);
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
    [DllImport(Lib, EntryPoint = "modmanager_dobby_destroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern int RemoveNativeHook(nint address);

    /// <summary>
    /// 移除单层 Hook。包含多层的链不能安全地从中间拆除，因此保留到进程结束。
    /// </summary>
    public static int Destroy(nint address)
    {
        lock (HookLock)
        {
            if (HookChains.TryGetValue(address, out var chain) && chain.Layers.Count > 1)
                return -5;
            if (DetourTargets.ContainsKey(address))
                return -5;

            // Keep the native operation inside the same lock as registry
            // updates so a concurrent install cannot attach to a hook while
            // it is being restored.
            var result = RemoveNativeHook(address);
            if (result != 0)
                return result;

            if (!HookChains.Remove(address, out var removedChain))
                return result;

            foreach (var layer in removedChain.Layers)
            {
                InstalledHooks.Remove(new HookKey(layer.Target, layer.Detour));
                DetourTargets.Remove(layer.Detour);
            }
            LatestHooks.Remove(address);

            return result;
        }
    }

    /// <summary>按动态库名和符号名解析函数地址。</summary>
    /// <param name="imageName">动态库名，如 "libil2cpp.so"</param>
    /// <param name="symbolName">符号名</param>
    /// <returns>符号地址，失败返回 nint.Zero</returns>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_symbol_resolver")]
    public static extern nint _SymbolResolver(string imageName, string symbolName);

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
    
    public static IntPtr SymbolResolver(string imageName, string symbolName){
        var handle = _SymbolResolver(imageName, symbolName);
        Logger.Error(nameof(SymbolResolver), $"Resolving {imageName}, {symbolName} = {handle}");
        return handle;
    }

    /// <summary>获取 Dobby 版本字符串。</summary>
    public static string GetVersion()
    {
        var ptr = _GetVersionRaw();
        return Marshal.PtrToStringAnsi(ptr) ?? "unknown";
    }
}
