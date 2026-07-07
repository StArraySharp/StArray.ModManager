using System.Runtime.InteropServices;

namespace StArray.ModManager.Windows.Native;

public static class NativeApi
{
    private const string LibraryName = "StArray.ModManager.Windows.Native";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ImGuiInitCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImGuiShutdownCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImGuiRenderCallback();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Init")]
    public static extern int Init(IntPtr init, IntPtr shutdown, IntPtr render);
}
