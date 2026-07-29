using OpenTK;
using StArray.ModManager.Native;

namespace StArray.ModManager.Android.Native;

public class GLESBindingsContext : IBindingsContext
{
    private IntPtr _libHandle;
    
    public GLESBindingsContext()
    {
        _libHandle = DL.Open("libGLESv3.so", DL.RTLDFlags.RTLD_LAZY);
    }

    public IntPtr GetProcAddress(string procName)
    {
        return DL.Symbol(_libHandle, procName);
    }
}