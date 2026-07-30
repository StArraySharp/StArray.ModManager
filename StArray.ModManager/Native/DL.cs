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

    [DllImport("dl", EntryPoint = "dl_iterate_phdr")]
    private static extern int dl_iterate_phdr(DlIteratePhdrCallback callback, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DlIteratePhdrCallback(IntPtr info, int size, IntPtr data);

    // ── Public API ──

    public static IntPtr Open(string fileName, RTLDFlags rtldFlags)
    {
        if (GetBaseAddress(fileName) != IntPtr.Zero) return GetBaseAddress(fileName);
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

    /// <summary>
    /// Iterates all loaded shared objects via <c>dl_iterate_phdr</c>.
    /// Returns the base address of the first library whose name ends with <paramref name="library"/>.
    /// Returns <see cref="IntPtr.Zero"/> if not found or not supported.
    /// </summary>
    public static IntPtr IteratePhdr(string library)
    {
        if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsLinux())
            return IntPtr.Zero;

        var libName = library.EndsWith(".so", StringComparison.Ordinal)
            ? library : library + ".so";

        IntPtr found = IntPtr.Zero;

        dl_iterate_phdr((IntPtr infoPtr, int size, IntPtr data) =>
        {
            var namePtr = Marshal.ReadIntPtr(infoPtr, IntPtr.Size);
            var name = Marshal.PtrToStringAnsi(namePtr);
            if (name != null && name.EndsWith(libName, StringComparison.Ordinal))
            {
                found = Marshal.ReadIntPtr(infoPtr);
                return 1;
            }
            return 0;
        }, IntPtr.Zero);

        return found;
    }

    /// <summary>
    /// Iterates all loaded shared objects and executes <paramref name="onMatch"/> for each match.
    /// <paramref name="onMatch"/> receives (baseAddr, phdrPtr, phnum).
    /// Returns true if any callback returned true (early stop).
    /// </summary>
    public static bool IteratePhdr(string library, Func<IntPtr, IntPtr, int, bool> onMatch)
    {
        if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsLinux())
            return false;

        var libName = library.EndsWith(".so", StringComparison.Ordinal)
            ? library : library + ".so";

        bool matched = false;

        dl_iterate_phdr((IntPtr infoPtr, int size, IntPtr data) =>
        {
            var namePtr = Marshal.ReadIntPtr(infoPtr, IntPtr.Size);
            var name = Marshal.PtrToStringAnsi(namePtr);
            if (name != null && name.EndsWith(libName, StringComparison.Ordinal))
            {
                var addr = Marshal.ReadIntPtr(infoPtr);
                var phdr = Marshal.ReadIntPtr(infoPtr, IntPtr.Size * 2);
                var phnum = Marshal.ReadInt16(infoPtr, IntPtr.Size * 2 + 8);
                if (onMatch(addr, phdr, phnum))
                {
                    matched = true;
                    return 1;
                }
            }
            return 0;
        }, IntPtr.Zero);

        return matched;
    }

    /// <summary>
    /// Finds the <c>p_vaddr</c> of the first <c>PT_LOAD</c> segment from program headers.
    /// Returns -1 if not found.
    /// </summary>
    public static long FindLoadVaddr(IntPtr phdr, int phnum)
    {
        // Elf64_Phdr: p_type(4) p_flags(4) p_offset(8) p_vaddr(8) p_paddr(8) p_filesz(8) p_memsz(8) p_align(8) = 56 bytes
        for (int i = 0; i < phnum; i++)
        {
            int type = Marshal.ReadInt32(phdr, i * 56);
            if (type == 1) // PT_LOAD
                return Marshal.ReadInt64(phdr, i * 56 + 16); // p_vaddr
        }
        return -1;
    }

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

            return IteratePhdr(library);
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
