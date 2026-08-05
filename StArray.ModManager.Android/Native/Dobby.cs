using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StArray.ModManager.Android.Native;

/// <summary>
/// Dobby P/Invoke wrapper backed by the process-wide native HookBroker.
/// The broker appends different detours to the same target and returns the
/// continuation for each layer.
/// </summary>
public static class Dobby
{
    private const string Lib = "modmanager";
    private static readonly object HookLock = new();
    private static readonly Dictionary<HookKey, HookRecord> InstalledHooks = new();
    private static readonly Dictionary<nint, HookRecord> LatestHooks = new();

    private readonly record struct HookKey(nint Target, nint Detour);
    private sealed record HookRecord(nint Target, nint Detour, nint Origin, string Owner);

    [DllImport(Lib, EntryPoint = "modmanager_hook_broker_install", CallingConvention = CallingConvention.Cdecl)]
    private static extern int InstallBrokerHook(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string owner,
        nint address,
        nint replace,
        out nint origin);

    /// <summary>安装 inline hook，并返回当前 layer 的 continuation。</summary>
    public static int Hook(nint address, nint replace, out nint origin)
        => Hook(address, replace, out origin, "Dobby.Hook");

    /// <summary>
    /// 安装 inline hook，并记录安装方。
    /// 同一地址的不同 detour 会由 native HookBroker 追加为新 layer；同一
    /// detour 的重复注册复用原 continuation。
    /// </summary>
    public static int Hook(nint address, nint replace, out nint origin, string owner)
    {
        origin = nint.Zero;
        if (address == nint.Zero || replace == nint.Zero)
            return -1;

        lock (HookLock)
        {
            var key = new HookKey(address, replace);
            if (InstalledHooks.TryGetValue(key, out var existing))
            {
                origin = existing.Origin;
                return 0;
            }

            var normalizedOwner = string.IsNullOrWhiteSpace(owner) ? "unknown" : owner;
            var result = InstallBrokerHook(normalizedOwner, address, replace, out origin);
            if (result == 0 && origin != nint.Zero)
            {
                var record = new HookRecord(
                    address,
                    replace,
                    origin,
                    normalizedOwner);
                InstalledHooks[key] = record;
                LatestHooks[address] = record;
            }

            return result;
        }
    }

    /// <summary>获取目标地址最近安装的 layer，供诊断使用。</summary>
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

    /// <summary>安装 inline hook，目标替换方法来自托管 MethodInfo。</summary>
    public static int Hook(nint address, MethodInfo replaceMethod, out nint origin)
    {
        if (replaceMethod == null)
        {
            origin = nint.Zero;
            return -1;
        }

        RuntimeHelpers.PrepareMethod(replaceMethod.MethodHandle);
        var replacePtr = replaceMethod.MethodHandle.GetFunctionPointer();
        var owner = replaceMethod.DeclaringType == null
            ? replaceMethod.Name
            : replaceMethod.DeclaringType.FullName + "." + replaceMethod.Name;
        return Hook(address, replacePtr, out origin, owner);
    }

    [DllImport(Lib, EntryPoint = "modmanager_dobby_instrument")]
    public static extern int Instrument(nint address, nint preHandler);

    /// <summary>
    /// 尝试移除 hook。HookBroker 链一旦建立便保持到进程结束，因此该调用
    /// 对 broker hook 返回失败，不会破坏其他 Mod 的 continuation。
    /// </summary>
    [DllImport(Lib, EntryPoint = "modmanager_dobby_destroy")]
    private static extern int RemoveHook(nint address);

    public static int Destroy(nint address)
    {
        var result = RemoveHook(address);
        if (result == 0)
        {
            lock (HookLock)
            {
                foreach (var key in InstalledHooks.Keys
                             .Where(key => key.Target == address)
                             .ToArray())
                    InstalledHooks.Remove(key);
                LatestHooks.Remove(address);
            }
        }

        return result;
    }

    [DllImport(Lib, EntryPoint = "modmanager_dobby_symbol_resolver")]
    public static extern nint SymbolResolver(string imageName, string symbolName);

    // Preserve the old helper name used by existing Android code.
    public static nint _SymbolResolver(string imageName, string symbolName) =>
        SymbolResolver(imageName, symbolName);

    [DllImport(Lib, EntryPoint = "modmanager_dobby_code_patch")]
    public static extern int CodePatch(nint address, byte[] buffer, uint bufferSize);

    [DllImport(Lib, EntryPoint = "modmanager_dobby_get_version")]
    private static extern nint GetVersionRaw();

    public static string GetVersion()
    {
        var ptr = GetVersionRaw();
        return Marshal.PtrToStringAnsi(ptr) ?? "unknown";
    }
}
