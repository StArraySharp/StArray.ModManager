using System.Runtime.InteropServices;
using StArray.ModManager.Runtime;

namespace StArray.ModManager.Native;

public class DobbyHook : IHook
{
    public nint Hook(nint target, nint detour)
    {
        if (Dobby.Hook(target, detour, out var origin) != 0)
            return nint.Zero;
        return origin;
    }

    public bool Unhook(nint target)
    {
        return Dobby.Destroy(target) == 0;
    }

    public nint GetFunction(string library, string name)
    {
        return Dobby.SymbolResolver(library, name);
    }
}
