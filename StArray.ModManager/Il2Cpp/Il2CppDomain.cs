using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Il2Cpp;

public unsafe class Il2CppDomain : IAppDomain
{
    private static readonly object CurrentLock = new();
    private static Il2CppDomain? _current;

    [ThreadStatic] private static nint _attachedThread;
    [ThreadStatic] private static int _attachmentDepth;
    [ThreadStatic] private static bool _ownsAttachment;
    [ThreadStatic] private static IIl2CppRuntimeApi? _attachmentApi;

    public nint Ptr { get; }
    public bool IsValid => Ptr != 0;

    public Il2CppDomain(nint ptr) => Ptr = ptr;

    public static event Action<string, nint>? AssemblyLoad;

    private static void OnAssemblyLoad(string name, nint asm) => AssemblyLoad?.Invoke(name, asm);

    public static Il2CppDomain? Current
    {
        get
        {
            var current = Volatile.Read(ref _current);
            if (current != null) return current;

            lock (CurrentLock)
            {
                if (_current != null) return _current;
                var ptr = Il2CppRuntimeApi.Current.DomainGet();
                if (ptr == 0) return null;
                _current = new Il2CppDomain(ptr);
                return _current;
            }
        }
    }

    internal static void ResetCachedState()
    {
        lock (CurrentLock)
            _current = null;
        _attachedThread = 0;
        _attachmentDepth = 0;
        _ownsAttachment = false;
        _attachmentApi = null;
    }

    public IReadOnlyList<IRuntimeAssembly> GetAssemblies()
    {
        uint size = 0;
        var ptr = Il2CppFunctions.il2cpp_domain_get_assemblies(Ptr, ref size);
        var list = new List<IRuntimeAssembly>((int)size);
        if (ptr == null) return list;
        for (uint i = 0; i < size; i++)
            list.Add(new Il2CppAssembly(ptr[i]));
        return list;
    }

    public IRuntimeAssembly? OpenAssembly(string name)
    {
        var asm = Il2CppFunctions.il2cpp_domain_assembly_open(Ptr, name);
        if (asm != 0)
        {
            OnAssemblyLoad(name, asm);
            return new Il2CppAssembly(asm);
        }
        return null;
    }

    public IRuntimeAssembly? WaitForAssembly(string name, int timeoutMs = 5000)
    {
        var asm = OpenAssembly(name);
        if (asm != null) return asm;

        int step = 100;
        int maxSteps = timeoutMs / step;
        for (int i = 0; i < maxSteps; i++)
        {
            Thread.Sleep(step);
            asm = OpenAssembly(name);
            if (asm != null) return asm;
        }
        return null;
    }

    public nint NewString(string str) => Il2CppFunctions.il2cpp_string_new(str);

    public nint NewArray(nint elementClass, int length) => Il2CppFunctions.il2cpp_array_new(elementClass, (ulong)length);

    public void ThreadAttach()
    {
        if (_attachmentDepth > 0)
        {
            _attachmentDepth++;
            return;
        }

        var api = Il2CppRuntimeApi.Current;
        var currentThread = api.ThreadCurrent();
        if (currentThread != 0)
        {
            _attachedThread = currentThread;
            _attachmentDepth = 1;
            _ownsAttachment = false;
            _attachmentApi = api;
            return;
        }

        var attachedThread = api.ThreadAttach(Ptr);
        if (attachedThread == 0)
            throw new InvalidOperationException("IL2CPP rejected thread attachment.");

        _attachedThread = attachedThread;
        _attachmentDepth = 1;
        _ownsAttachment = true;
        _attachmentApi = api;
    }

    public void ThreadDetach()
    {
        if (_attachmentDepth == 0) return;
        if (--_attachmentDepth > 0) return;

        var thread = _attachedThread;
        var ownsAttachment = _ownsAttachment;
        var api = _attachmentApi;
        _attachedThread = 0;
        _ownsAttachment = false;
        _attachmentApi = null;

        if (ownsAttachment && thread != 0)
            api!.ThreadDetach(thread);
    }
}
