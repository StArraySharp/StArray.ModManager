using System.Runtime.InteropServices;

namespace StArray.ModManager.PInvoke;

public class Misc
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptChar(uint codepoint);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptCharCallback")]
    public static extern void SetOnAcceptCharCallback(OnAcceptChar onAcceptChar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptKey(int keyCode);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptKeyCallback")]
    public static extern void SetOnAcceptKeyCallback(OnAcceptKey onAcceptKey);
}