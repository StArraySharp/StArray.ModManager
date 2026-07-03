using System.Runtime.CompilerServices;
using ImGuiNET;
using System.Runtime.InteropServices;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.Core;
using StArray.ModManager.Hooks;
using StArray.ModManager.Runtime;

namespace ImGuiHook.DX12;

public partial class UnityEngineHook
{
    [Il2CppHook("UnityEngine.CoreModule", "UnityEngine.Object", "GetComponentInChildren")]
    public static nint GetComponentInChildren(nint ptr)
    {
        GetComponentInChildrenOriginal(ptr);
        return ptr;
    }
    
    //[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    [NativeHook("msvcrt.dll","printf")]
    public static nint printf(nint ptr)
    {
        printfOriginal(ptr);
        return ptr;
    }
}
