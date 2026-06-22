using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModLoader.Manager;
using StArray.ModLoader.PInvoke;
using StArray.ModLoader.Unity;

namespace StArray.ModLoader;

/// <summary>
/// 加载器入口 — 由 CoreCLR delegate 回调。
/// </summary>
public static class Mono
{
    private const string LogTag = "StArray.ModLoader.Managed";

    #region HookTest
    public static int HookTest()
    {
        Thread.Sleep(5000);
        UnityResolve resolve = new UnityResolve();
        resolve.InitIl2Cpp();
        AndroidLog.Error(LogTag, $"AssemblyCount:{resolve.Assemblies.Count()}");
        foreach (var assembly1 in resolve.Assemblies)
        {
            AndroidLog.Error("Assembly",assembly1.Name);
        }
        var assembly = resolve.GetAssembly("Assembly-CSharp.dll");
        AndroidLog.Error(LogTag, $"assembly is null: {assembly == null}");
        if (assembly == null) return -1;
        var klass = assembly.GetClass("", "NewBehaviourScript");
        AndroidLog.Error(LogTag, $"class is null: {klass == null}");
        if (klass == null) return -1;
        var method = klass.GetMethod("Button_OnClick");
        AndroidLog.Error(LogTag, $"method is null: {method == null} ptr:{method?.NativePtr:X}");
        if (method == null) return -1;
        Dobby.Hook(method.FunctionPtr, typeof(Mono).GetMethod(nameof(OnClick)), out var orig);
        return 0;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void OnClick(IntPtr instance)
    {
        AndroidLog.Error("DOTNET",$"0x{instance:X}: OnClick Called");
    }
    #endregion
    public static int Entry()
    {
        AndroidLog.Error(LogTag,UnitySurfaceHelper.GetUnityNativeWindow().ToString());
        var result = ImGuiRender.Install();
        return 0;
    }
}
