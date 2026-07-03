using System.Runtime.InteropServices;

namespace StArray.ModManager.Il2Cpp;

public unsafe class Il2CppDomain
{
    public nint Ptr { get; }
    public bool IsValid => Ptr != 0;

    public Il2CppDomain(nint ptr) => Ptr = ptr;

    public static Il2CppDomain? Current
    {
        get
        {
            var ptr = Il2CppFunctions.il2cpp_domain_get();
            return ptr != 0 ? new Il2CppDomain(ptr) : null;
        }
    }

    public List<Il2CppAssembly> GetAssemblies()
    {
        var list = new List<Il2CppAssembly>();
        uint size = 0;
        var ptr = Il2CppFunctions.il2cpp_domain_get_assemblies(Ptr, ref size);
        if (ptr == null) return list;
        for (uint i = 0; i < size; i++)
            list.Add(new Il2CppAssembly(ptr[i]));
        return list;
    }

    public Il2CppAssembly? OpenAssembly(string name)
    {
        var nameObj = Il2CppFunctions.il2cpp_string_new(name);
        var asm = Il2CppFunctions.il2cpp_domain_assembly_open(Ptr, nameObj);
        return asm != 0 ? new Il2CppAssembly(asm) : null;
    }

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
