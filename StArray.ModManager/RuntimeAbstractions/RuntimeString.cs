using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

public readonly unsafe struct RuntimeString
{
    public nint Ptr { get; }

    public RuntimeString(nint ptr) => Ptr = ptr;
    public RuntimeString(RuntimeObject obj) => Ptr = obj.Ptr;
    public bool IsValid => Ptr != 0;

    public int Length
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_string_length(Ptr);
            if (RuntimeManager.IsMono) return MonoFunctions.MonoStringLength(Ptr);
            return 0;
        }
    }

    public char* Chars
    {
        get
        {
            if (RuntimeManager.IsIl2Cpp) return Il2CppFunctions.il2cpp_string_chars(Ptr);
            if (RuntimeManager.IsMono) return MonoFunctions.MonoStringChars(Ptr);
            return null;
        }
    }

    public override string ToString()
    {
        var len = Length;
        if (len <= 0) return "";
        return Marshal.PtrToStringUni((nint)Chars, len) ?? "";
    }

    public static RuntimeString New(string str)
    {
        var domain = RuntimeManager.GetDomain();
        return domain != null ? New(domain, str) : default;
    }

    public static RuntimeString New(IAppDomain domain, string str)
    {
        var ptr = domain.NewString(str);
        return ptr != 0 ? new RuntimeString(ptr) : default;
    }

    public static implicit operator string(RuntimeString s) => s.ToString();
    public static implicit operator RuntimeString(RuntimeObject obj) => new(obj.Ptr);
    public static implicit operator RuntimeObject(RuntimeString s) => new(s.Ptr);
}
