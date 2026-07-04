using System.Runtime.InteropServices;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Windows.Native;

public class MinHook : IHook
{
    private const string Lib = "MinHook.x64";

    private enum Status : int
    {
        Ok = 0,
        Error = -1,
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

    // Win32
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern nint GetProcAddress(nint hModule, string lpProcName);

    private bool _initialized;

    private void EnsureInit()
    {
        if (_initialized)
            return;
        _Initialize();
        _initialized = true;
    }

    public nint Hook(nint target, nint detour)
    {
        EnsureInit();
        if (_CreateHook(target, detour, out var original) != Status.Ok)
            return nint.Zero;
        _EnableHook(target);
        return original;
    }

    public bool Unhook(nint target)
    {
        var ok = _DisableHook(target) == Status.Ok;
        ok &= _RemoveHook(target) == Status.Ok;
        return ok;
    }

    public nint GetFunction(string library, string name)
    {
        var dllName = library.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? library : library + ".dll";

        // 1) 从已加载的模块获取
        var mod = GetModuleHandle(dllName);
        if (mod != nint.Zero)
        {
            var addr = GetProcAddress(mod, name);
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
