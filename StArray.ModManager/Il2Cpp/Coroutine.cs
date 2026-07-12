namespace StArray.ModManager.Il2Cpp;

/// <summary>Unity MonoBehaviour 协程封装</summary>
public static class Coroutine
{
    private static Il2CppClass? s_monoClass;
    private static Il2CppMethod? s_startCoroutine;
    private static Il2CppMethod? s_stopCoroutine;

    private static void EnsureInit()
    {
        if (s_monoClass != null) return;
        var asm = Il2CppAssembly.Get("UnityEngine.CoreModule.dll");
        s_monoClass = asm?.GetClass("UnityEngine", "MonoBehaviour");
        s_startCoroutine = s_monoClass?.GetMethod("StartCoroutine", "System.Collections.IEnumerator");
        s_stopCoroutine = s_monoClass?.GetMethod("StopCoroutine", "System.Collections.IEnumerator");
    }

    /// <summary>
    /// 启动协程。需要传入一个 Unity MonoBehaviour 实例指针和实现了 IEnumerator 的托管对象。
    /// </summary>
    public static nint Start(nint monoBehaviour, nint iEnumerator)
    {
        EnsureInit();
        if (s_startCoroutine == null) return 0;
        return s_startCoroutine.Invoke(monoBehaviour, [iEnumerator]);
    }

    /// <summary>
    /// 停止协程。
    /// </summary>
    public static void Stop(nint monoBehaviour, nint coroutine)
    {
        EnsureInit();
        s_stopCoroutine?.Invoke(monoBehaviour, [coroutine]);
    }
}
