using System.Globalization;
using System.Runtime.InteropServices;

namespace StArray.ModManager.Native;

public static class DL
{
    [DllImport("dl", EntryPoint = "dlopen")]
    private static extern IntPtr dl_open(string fileName, RTLDFlags rtldFlags);

    [DllImport("dl", EntryPoint = "dlsym")]
    private static extern IntPtr dl_sym(IntPtr handle, string symbol);

    [DllImport("dl", EntryPoint = "dlclose")]
    private static extern int dl_close(IntPtr handle);

    [DllImport("dl", EntryPoint = "dlerror")]
    private static extern IntPtr dl_error();

    [DllImport("dl", EntryPoint = "dladdr")]
    private static extern int dl_addr(IntPtr addr, ref DlInfo info);

    // ── Public API ──

    public static IntPtr Open(string fileName, RTLDFlags rtldFlags)
    {
        if (GetBaseAddress(fileName) != IntPtr.Zero) return IntPtr.Zero;
        return dl_open(fileName, rtldFlags);
    }

    public static IntPtr Symbol(IntPtr handle, string symbol) =>
        dl_sym(handle, symbol);

    public static int Close(IntPtr handle) =>
        dl_close(handle);

    public static IntPtr Error() =>
        dl_error();

    public static int Addr(IntPtr addr, ref DlInfo info) =>
        dl_addr(addr, ref info);

    public static IntPtr GetBaseAddress(string library)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsLinux())
        {
            var libName = library.EndsWith(".so", StringComparison.Ordinal)
                ? library : library + ".so";

            foreach (var line in File.ReadLines("/proc/self/maps"))
            {
                if (!line.EndsWith(libName, StringComparison.Ordinal))
                    continue;

                var dash = line.IndexOf('-');
                if (dash < 0) continue;

                var addrStr = line.AsSpan(0, dash);
                if (long.TryParse(addrStr, NumberStyles.HexNumber, null, out var addr))
                    return (IntPtr)addr;
            }
        }

        return IntPtr.Zero;
    }

    [Flags]
    public enum RTLDFlags
    {
        RTLD_LOCAL = 0,
        RTLD_LAZY = 1,
        RTLD_NOW = 2,
        RTLD_NOLOAD = 4,
        RTLD_GLOBAL = 0x100
    }

    public struct DlInfo
    {
        public IntPtr dli_fname;
        public IntPtr dli_fbase;
        public IntPtr dli_sname;
        public IntPtr dli_saddr;
    }
}
