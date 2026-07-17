using StArray.ModManager.RuntimeAbstractions;

namespace StArray.ModManager.Il2Cpp;

public unsafe class Il2CppDomain : IAppDomain
{
    public nint Ptr { get; }
    public bool IsValid => Ptr != 0;

    public Il2CppDomain(nint ptr) => Ptr = ptr;

    public static event Action<string, nint>? AssemblyLoad;

    private static void OnAssemblyLoad(string name, nint asm) => AssemblyLoad?.Invoke(name, asm);

    public static Il2CppDomain? Current
    {
        get
        {
            var ptr = Il2CppFunctions.il2cpp_domain_get();
            return ptr != 0 ? new Il2CppDomain(ptr) : null;
        }
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
        Il2CppFunctions.il2cpp_thread_attach(Ptr);
    }

    public static void ThreadDetach()
    {
        var thread = Il2CppFunctions.il2cpp_thread_current();
        if (thread != 0)
            Il2CppFunctions.il2cpp_thread_detach(thread);
    }
}
