using OpenTK;

namespace StArray.ModManager.Android.Native;

public class GLESBindingsContext : IBindingsContext
{
    private IntPtr _libHandle;
    
    public GLESBindingsContext()
    {
        _libHandle = DL.dlopen("libGLESv3.so", DL.Flags.RTLD_LAZY);
    }

    public IntPtr GetProcAddress(string procName)
    {
        return DL.dlsym(_libHandle, procName);
    }
}