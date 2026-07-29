using System.Runtime.InteropServices;
using StArray.ModManager.Manager;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Windows.Native;

public class MinHook : IHook
{
    private const string Lib = NativeApi.LibraryName;

    private enum Status : int
    {
        Ok = 0,
        ErrorAlreadyInitialized = 1,
        ErrorNotInitialized = 2,
        ErrorAlreadyCreated = 3,
        ErrorNotCreated = 4,
        ErrorEnabled = 5,
        ErrorDisabled = 6,
        ErrorNotExecutable = 7,
        ErrorUnsupportedFunction = 8,
        ErrorMemoryAlloc = 9,
        ErrorMemoryProtect = 10,
        ErrorModuleNotFound = 11,
        ErrorFunctionNotFound = 12,
    }

    [DllImport(Lib, EntryPoint = "MH_Initialize")]
    private static extern Status _Initialize();

    [DllImport(Lib, EntryPoint = "MH_Uninitialize")]
    private static extern Status _Uninitialize();

    [DllImport(Lib, EntryPoint = "MH_CreateHook")]
    private static extern Status _CreateHook(nint target, nint detour, out nint original);

    [DllImport(Lib, EntryPoint = "MH_EnableHook")]
    private static extern Status _EnableHook(nint target);

    [DllImport(Lib, EntryPoint = "MH_DisableHook")]
    private static extern Status _DisableHook(nint target);

    [DllImport(Lib, EntryPoint = "MH_RemoveHook")]
    private static extern Status _RemoveHook(nint target);

    private bool _initialized;
    private readonly Dictionary<nint, nint> _hooks = new();

    private void EnsureInit()
    {
        if (_initialized)
            return;
        var s = _Initialize();
        Logger.Info("MinHook", $"MH_Initialize: {s} ({(int)s})");
        _initialized = true;
    }

    public nint Hook(nint target, nint detour)
    {
        EnsureInit();
        if (target == 0)
        {
            Logger.Warn("MinHook", "Hook target is 0, skipping");
            return nint.Zero;
        }
        if (_hooks.TryGetValue(target, out var existing))
            return existing;

        // 确保目标内存可读可写
        Win32Native.VirtualProtect(target, (UIntPtr)32, 0x40 /* PAGE_EXECUTE_READWRITE */, out _);

        Status s;
        nint original;
        try
        {
            s = _CreateHook(target, detour, out original);
        }
        catch (AccessViolationException ex)
        {
            Logger.Error("MinHook", $"MH_CreateHook access violation at target=0x{target:X}: {ex.Message}");
            return nint.Zero;
        }
        catch (Exception ex)
        {
            Logger.Error("MinHook", $"MH_CreateHook failed at target=0x{target:X}: {ex.GetType().Name}: {ex.Message}");
            return nint.Zero;
        }

        Logger.Info("MinHook", $"MH_CreateHook(target=0x{target:X}, detour=0x{detour:X}): {s} ({(int)s}), original=0x{original:X}");
        if (s != Status.Ok)
            return nint.Zero;
        _hooks[target] = original;
        _EnableHook(target);
        return original;
    }

    public bool Unhook(nint target)
    {
        var ok = _DisableHook(target) == Status.Ok;
        ok &= _RemoveHook(target) == Status.Ok;
        _hooks.Remove(target);
        return ok;
    }

    public nint GetFunctionRVA(string library, long rva)
    {
        var dllName = library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? library : library + ".dll";
        var mod = Win32Native.GetModuleHandleW(dllName);
        if (mod != nint.Zero)
            return mod + (nint)rva;

        var localPath = Path.Combine(AppContext.BaseDirectory, dllName);
        if (File.Exists(localPath))
        {
            NativeLibrary.Load(localPath);
            mod = Win32Native.GetModuleHandleW(dllName);
            if (mod != nint.Zero)
                return mod + (nint)rva;
        }

        var dllsPath = Path.Combine(AppContext.BaseDirectory, "dlls", dllName);
        if (File.Exists(dllsPath))
        {
            NativeLibrary.Load(dllsPath);
            mod = Win32Native.GetModuleHandleW(dllName);
            if (mod != nint.Zero)
                return mod + (nint)rva;
        }

        return nint.Zero;
    }

    public nint GetFunction(string library, string name)
    {
        var dllName = library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? library : library + ".dll";

        // 1) 从已加载的模块获取
        var mod = Win32Native.GetModuleHandleW(dllName);
        if (mod != nint.Zero)
        {
            var addr = Win32Native.GetProcAddress(mod, name);
            if (addr != nint.Zero) return addr;
        }

        // 2) 从输出目录加载
        var localPath = Path.Combine(AppContext.BaseDirectory, dllName);
        if (File.Exists(localPath))
        {
            mod = NativeLibrary.Load(localPath);
            if (mod != nint.Zero)
            {
                var addr = NativeLibrary.GetExport(mod, name);
                if (addr != nint.Zero) return addr;
            }
        }

        // 3) 从 dlls/ 加载
        var dllsPath = Path.Combine(AppContext.BaseDirectory, "dlls", dllName);
        if (File.Exists(dllsPath))
        {
            mod = NativeLibrary.Load(dllsPath);
            if (mod != nint.Zero)
                return NativeLibrary.GetExport(mod, name);
        }

        return nint.Zero;
    }
}
