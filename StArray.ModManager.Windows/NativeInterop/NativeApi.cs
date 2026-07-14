using System.Runtime.InteropServices;

namespace StArray.ModManager.Windows.Native;

public static class NativeApi
{
    public const string LibraryName = "StArray.ModManager.Windows.Native";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ImGuiInitCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImGuiShutdownCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImGuiRenderCallback();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Init")]
    public static extern int Init(IntPtr init, IntPtr shutdown, IntPtr render);

    /// <summary>Detect available graphics backends. Returns bitmask: 1=D3D12, 2=D3D11, 4=D3D9, 8=GL, 16=VK.</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetAvailableBackends")]
    public static extern int GetAvailableBackends();

    /// <summary>Select which backend to hook. 0=D3D12, 1=D3D11, 2=D3D9, 3=GL, 4=VK. Call before Init().</summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetBackend")]
    public static extern int SetBackend(int backend);
}
