using System.Runtime.InteropServices;

namespace StArray.ModManager.Android.Native;

public static class JniHelperNative
{
    private const string LibModManager = "modmanager";

    [DllImport(LibModManager, EntryPoint = "jnihelper_get_jvm")]
    public static extern IntPtr GetJavaVM();

    [DllImport(LibModManager, EntryPoint = "jnihelper_get_env")]
    public static extern IntPtr GetJNIEnv();

    [DllImport(LibModManager, EntryPoint = "jnihelper_get_string")]
    private static extern IntPtr GetStringInternal(IntPtr jstr);

    public static string? GetString(IntPtr jstr)
    {
        if (jstr == IntPtr.Zero)
            return null;

        IntPtr ptr = GetStringInternal(jstr);
        if (ptr == IntPtr.Zero)
            return null;

        try
        {
            return Marshal.PtrToStringUTF8(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [DllImport(LibModManager, EntryPoint = "jnihelper_get_unity_native_window")]
    public static extern IntPtr GetUnityNativeWindow();
}
