using System.Runtime.InteropServices;

namespace StArray.ModManager.Windows.Native;

/// <summary>
/// Centralized Win32 P/Invoke declarations (kernel32, user32, d3d11).
/// </summary>
public static class Win32Native
{
    // ── kernel32.dll ──────────────────────────────────────────

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern nint GetModuleHandleW(string? lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    public static extern nint GetProcAddress(nint hModule, string lpProcName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern bool WriteConsoleW(nint hConsole, string text, uint len, out uint written, nint reserved);

    [DllImport("kernel32.dll")]
    public static extern nint GetStdHandle(int nStdHandle);

    // ── user32.dll ────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern ushort RegisterClassExW(ref WNDCLASSEX wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint CreateWindowExW(uint ex, string cls, string name, uint style,
        int x, int y, int w, int h, nint p, nint m, nint i, nint p2);

    [DllImport("user32.dll")]
    public static extern nint DefWindowProcW(nint h, uint m, nint w, nint l);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowLongPtrW(nint h, int n, nint d);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint CallWindowProcW(nint p, nint h, uint m, nint w, nint l);

    [DllImport("user32.dll")]
    public static extern bool DestroyWindow(nint h);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool UnregisterClassW(string cls, nint inst);

    [DllImport("user32.dll")]
    public static extern nint WindowFromDC(nint hdc);

    // ── d3d11.dll ─────────────────────────────────────────────

    [DllImport("d3d11.dll")]
    public static extern int D3D11CreateDeviceAndSwapChain(
        nint a, uint dt, nint sw, uint f,
        nint fl, uint l, uint sv,
        ref DXGISwapChainDesc d, out nint sc, out nint dev, out nint ctx);

    // ── Structs ───────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public nint hInstance, hIcon, hCursor, hbrBackground;
        public nint lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    /// <summary>Minimal blittable DXGI_SWAP_CHAIN_DESC — field layout must match native exactly.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DXGISwapChainDesc
    {
        public int Width, Height;
        public int RefreshNum, RefreshDen;
        public int Format;
        public int ScanlineOrder;
        public int Scaling;
        public int SampleCount, SampleQuality;
        public uint BufferUsage;
        public int BufferCount;
        public nint OutputWindow;
    }
}
