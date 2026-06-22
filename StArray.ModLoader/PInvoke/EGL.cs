using System.Runtime.InteropServices;

namespace StArray.ModLoader.PInvoke;

/// <summary>
/// EGL (Embedded-System Graphics Library) P/Invoke bindings
/// </summary>
public static class EGL
{
    private const string LibEGL = "libEGL.so";
    
    // EGL types
    public static readonly IntPtr EGL_NO_DISPLAY = IntPtr.Zero;
    public static readonly IntPtr EGL_NO_SURFACE = IntPtr.Zero;
    public static readonly IntPtr EGL_NO_CONTEXT = IntPtr.Zero;
    
    // EGL surface attributes
    public const int EGL_HEIGHT = 0x3056;
    public const int EGL_WIDTH = 0x3057;
    public const int EGL_RENDER_BUFFER = 0x3086;
    
    // EGL surface types
    public const int EGL_READ = 0x305A;
    public const int EGL_DRAW = 0x3059;
    
    /// <summary>
    /// Get the current EGL display
    /// </summary>
    [DllImport(LibEGL, EntryPoint = "eglGetCurrentDisplay")]
    public static extern IntPtr GetCurrentDisplay();
    
    /// <summary>
    /// Get the current EGL surface (read or draw)
    /// </summary>
    [DllImport(LibEGL, EntryPoint = "eglGetCurrentSurface")]
    public static extern IntPtr GetCurrentSurface(int readdraw);
    
    /// <summary>
    /// Query EGL surface attributes
    /// </summary>
    [DllImport(LibEGL, EntryPoint = "eglQuerySurface")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool QuerySurface(IntPtr dpy, IntPtr surface, int attribute, out int value);
    
    /// <summary>
    /// Swap front and back buffers
    /// </summary>
    [DllImport(LibEGL, EntryPoint = "eglSwapBuffers")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SwapBuffers(IntPtr dpy, IntPtr surface);
    
    /// <summary>
    /// Get the last EGL error
    /// </summary>
    [DllImport(LibEGL, EntryPoint = "eglGetError")]
    public static extern int GetError();
}
