using System.Collections.Concurrent;
using StArray.ModManager.Manager;
using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Mono;

public unsafe class MonoDomain : IAppDomain
{
    private static readonly ConcurrentDictionary<string, List<Action>> _pendingActions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<Action> _deferredActions = new();
    private _MonoThread* _thread; // Changed from _MonoThread* _thread;

    public nint Ptr { get; }
    public bool IsValid => Ptr != 0;

    public MonoDomain(nint ptr) => Ptr = ptr;

    /// <summary>Assembly loaded callback. Args: assembly name (e.g. "Assembly-CSharp.dll"), assembly ptr.</summary>
    public static event Action<string, nint>? AssemblyLoad;

    internal static void OnAssemblyLoad(string name, nint asm) => AssemblyLoad?.Invoke(name, asm);

    static MonoDomain()
    {
        MonoFunctions.InstallAssemblyLoadHook();
        AssemblyLoad += (name, asm) =>
        {
            Logger.Debug("Mono", $"Assembly loaded: {name}");
        };
    }

    public static MonoDomain? Current
    {
        get
        {
            var ptr = MonoFunctions.MonoGetRootDomain();
            return ptr != 0 ? new MonoDomain(ptr) : null;
        }
    }

    public IRuntimeAssembly? OpenAssembly(string name)
    {
        // 1. 查找已加载的程序集
        var searchName = Path.GetFileNameWithoutExtension(name);
        var existing = GetAssemblies().FirstOrDefault(a =>
            string.Equals(Path.GetFileNameWithoutExtension(a.Name), searchName, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return existing;

        // 2. 从本地文件直接加载
        var status = MonoImageOpenStatus.MONO_IMAGE_OK;
        var asm = MonoFunctions.MonoAssemblyOpen(name, out status);
        if (asm != 0 && status == MonoImageOpenStatus.MONO_IMAGE_OK)
        {
            OnAssemblyLoad(name, asm);
            return new MonoAssembly(asm);
        }

        // 3. 尝试按名称加载（不带 .dll）
        var loaded = MonoFunctions.MonoAssemblyLoadWithPartialName(searchName, out status);
        if (loaded != 0 && status == MonoImageOpenStatus.MONO_IMAGE_OK)
        {
            OnAssemblyLoad(name, loaded);
            return new MonoAssembly(loaded);
        }

        return null;
    }

    public IReadOnlyList<IRuntimeAssembly> GetAssemblies()
    {
        var list = new List<IRuntimeAssembly>();
        MonoFunctions.MonoAssemblyForeach(asm =>
        {
            list.Add(new MonoAssembly(asm));
        });
        return list;
    }

    public nint NewString(string str) => MonoFunctions.MonoStringNew(Ptr, str);

    public nint NewArray(nint elementClass, int length) => MonoFunctions.MonoArrayNew(Ptr, elementClass, (nuint)length);

    public void ThreadAttach()
    {
        if (_thread != null) return;
        _thread = Methods.mono_thread_attach((_MonoDomain*)Ptr);
    }
    public void ThreadDetach()
    {
        Methods.mono_thread_detach(_thread);
        _thread = null;
    }
}
