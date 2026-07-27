namespace StArray.ModManager.RuntimeAbstractions;

public static class RuntimeHelpers
{
    // ── Instance methods ──

    public static void InstanceVoid(nint ptr, string methodName, int paramCount, nint[]? args)
        => new RuntimeObject(ptr).InvokeVoid(methodName, paramCount, args);

    public static nint InstanceRet(nint ptr, string methodName, int paramCount, nint[]? args)
        => new RuntimeObject(ptr).Invoke(methodName, paramCount, args);

    /// <summary>
    /// For value-type returns. runtime_invoke hands back a *boxed* object, so the raw pointer
    /// has to be unboxed and dereferenced — truncating it would yield garbage.
    /// </summary>
    public static T InstanceRetUnbox<T>(nint ptr, string methodName, int paramCount, nint[]? args)
        where T : unmanaged
        => new RuntimeObject(ptr).InvokeUnbox<T>(methodName, paramCount, args);

    // ── Static methods ──

    private static IRuntimeMethod? ResolveStaticMethod(string asmName, string ns, string className,
        string methodName, int paramCount)
    {
        var domain = RuntimeManager.GetDomain();
        if (domain == null) return null;
        var asm = domain.OpenAssembly(asmName);
        if (asm == null) return null;
        var cls = asm.GetClass(ns, className);
        if (cls == null) return null;
        return cls.GetMethod(methodName, paramCount);
    }

    public static void StaticVoid(string asmName, string ns, string className,
        string methodName, int paramCount, nint[]? args)
        => ResolveStaticMethod(asmName, ns, className, methodName, paramCount)
            ?.InvokeStatic(args);

    public static nint StaticRet(string asmName, string ns, string className,
        string methodName, int paramCount, nint[]? args)
        => ResolveStaticMethod(asmName, ns, className, methodName, paramCount)
            ?.InvokeStatic(args) ?? 0;

    /// <summary>Value-type counterpart of <see cref="StaticRet"/> — see <see cref="InstanceRetUnbox{T}"/>.</summary>
    public static T StaticRetUnbox<T>(string asmName, string ns, string className,
        string methodName, int paramCount, nint[]? args) where T : unmanaged
        => ResolveStaticMethod(asmName, ns, className, methodName, paramCount) is { } m
            ? m.InvokeStaticUnbox<T>(args)
            : default;

    // ── Instance fields ──

    public static T GetField<T>(nint ptr, string fieldName) where T : unmanaged
        => new RuntimeObject(ptr).GetField<T>(fieldName);

    public static void SetField<T>(nint ptr, string fieldName, T value) where T : unmanaged
        => new RuntimeObject(ptr).SetField(fieldName, value);

    // ── Static fields ──

    private static IRuntimeField? ResolveStaticField(string asmName, string ns, string className,
        string fieldName)
    {
        var domain = RuntimeManager.GetDomain();
        if (domain == null) return null;
        var asm = domain.OpenAssembly(asmName);
        if (asm == null) return null;
        var cls = asm.GetClass(ns, className);
        if (cls == null) return null;
        return cls.GetField(fieldName);
    }

    public static T GetStaticField<T>(string asmName, string ns, string className,
        string fieldName) where T : unmanaged
        => ResolveStaticField(asmName, ns, className, fieldName) is { } f
            ? f.GetValue<T>(0)
            : default;

    public static void SetStaticField<T>(string asmName, string ns, string className,
        string fieldName, T value) where T : unmanaged
        => ResolveStaticField(asmName, ns, className, fieldName)?.SetValue(0, value);
}
