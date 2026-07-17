using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

public static unsafe class RuntimeBox
{
    public static nint Box<T>(T value) where T : unmanaged
    {
        var size = sizeof(T);
        if (size <= sizeof(nint))
        {
            return Unsafe.As<T, nint>(ref value);
        }

        var classPtr = GetClassPtr(typeof(T));
        if (classPtr == 0)
            throw new InvalidOperationException($"Runtime class not found for {typeof(T)}");

        nint objPtr;
        if (RuntimeManager.IsIl2Cpp)
        {
            objPtr = Il2CppFunctions.il2cpp_object_new(classPtr);
            if (objPtr == 0) return 0;
            var dataPtr = Il2CppFunctions.il2cpp_object_unbox(objPtr);
            Unsafe.CopyBlock((void*)dataPtr, Unsafe.AsPointer(ref value), (uint)size);
        }
        else if (RuntimeManager.IsMono)
        {
            var domain = MonoFunctions.MonoDomainGet();
            objPtr = MonoFunctions.MonoObjectNew(domain, classPtr);
            if (objPtr == 0) return 0;
            var dataPtr = MonoFunctions.MonoObjectUnbox(objPtr);
            Unsafe.CopyBlock((void*)dataPtr, Unsafe.AsPointer(ref value), (uint)size);
        }
        else
        {
            return 0;
        }

        return objPtr;
    }

    public static T Unbox<T>(nint ptr) where T : unmanaged
    {
        if (sizeof(T) <= sizeof(nint))
        {
            return Unsafe.As<nint, T>(ref ptr);
        }

        nint dataPtr;
        if (RuntimeManager.IsIl2Cpp)
            dataPtr = Il2CppFunctions.il2cpp_object_unbox(ptr);
        else if (RuntimeManager.IsMono)
            dataPtr = MonoFunctions.MonoObjectUnbox(ptr);
        else
            return default;

        var result = default(T);
        Unsafe.CopyBlock(Unsafe.AsPointer(ref result), (void*)dataPtr, (uint)sizeof(T));
        return result;
    }

    private static nint GetClassPtr(Type type)
    {
        var domain = RuntimeManager.GetDomain();
        if (domain == null) return 0;

        var assemblies = domain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            var name = asm.Name;
            if (name == null) continue;
            if (!name.StartsWith("UnityEngine") && !name.StartsWith("Unity")) continue;
            var klass = asm.GetClass(type.Namespace ?? "", type.Name);
            if (klass != null) return klass.Ptr;
        }

        return 0;
    }
}
