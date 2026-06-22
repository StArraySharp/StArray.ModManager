using System.Runtime.InteropServices;

namespace StArray.ModLoader.PInvoke;

public class DL
{
    [DllImport("dl")]
    public static extern IntPtr dlopen(string fileName, Flags flags);
    
    [DllImport("dl")]
    public static extern IntPtr dlsym(IntPtr handle, string symbol);

    [Flags]
    public enum Flags : int
    {
        RTLD_LAZY = 1,
        RTLD_NOW = 2,
        RTLD_GLOBAL = 0x100,
        RTLD_LOCAL = 0x200
    }
}