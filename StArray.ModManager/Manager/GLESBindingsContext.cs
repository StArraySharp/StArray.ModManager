using System.Runtime.InteropServices;
using OpenTK;

namespace StArray.ModManager.Manager;

public class GLESBindingsContext : IBindingsContext
{
    [DllImport("dl")]
    private static extern IntPtr dlopen(string fileName, int flags);
    
    [DllImport("dl")]
    private static extern IntPtr dlsym(IntPtr handle, string symbol);
    
    private IntPtr _libHandle;
    
    public GLESBindingsContext()
    {
        _libHandle = dlopen("libGLESv3.so", 1);
    }

    public IntPtr GetProcAddress(string procName)
    {
        return dlsym(_libHandle, procName);
    }
}