using System.Text;

namespace StArray.ModManager.Il2Cpp;

internal interface IIl2CppRuntimeApi
{
    nint DomainGet();
    nint ThreadCurrent();
    nint ThreadAttach(nint domain);
    void ThreadDetach(nint thread);
    nint ObjectUnbox(nint obj);
    int FieldGetFlags(nint field);
    bool IsReferenceField(nint field);
    T GetFieldValue<T>(nint obj, nint field, bool isStatic) where T : unmanaged;
    void SetFieldValue<T>(nint obj, nint field, bool isStatic, T value) where T : unmanaged;
    void SetObjectFieldValue(nint obj, nint field, nint value);
    nint RuntimeInvoke(nint method, nint obj, nint[]? args, out nint exception);
    string FormatException(nint exception);
    string FormatStackTrace(nint exception);
}

internal static class Il2CppRuntimeApi
{
    private static IIl2CppRuntimeApi _current = new NativeIl2CppRuntimeApi();

    internal static IIl2CppRuntimeApi Current => Volatile.Read(ref _current);

    internal static IDisposable OverrideForTesting(IIl2CppRuntimeApi replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var previous = Interlocked.Exchange(ref _current, replacement);
        Il2CppDomain.ResetCachedState();
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(IIl2CppRuntimeApi previous) : IDisposable
    {
        private IIl2CppRuntimeApi? _previous = previous;

        public void Dispose()
        {
            var value = Interlocked.Exchange(ref _previous, null);
            if (value == null) return;
            Interlocked.Exchange(ref _current, value);
            Il2CppDomain.ResetCachedState();
        }
    }
}

internal sealed unsafe class NativeIl2CppRuntimeApi : IIl2CppRuntimeApi
{
    private const int FormatBufferSize = 4096;

    private enum Il2CppTypeCode
    {
        String = 0x0e,
        Class = 0x12,
        TypeParameter = 0x13,
        Array = 0x14,
        GenericInstance = 0x15,
        Object = 0x1c,
        SzArray = 0x1d,
        MethodTypeParameter = 0x1e,
    }

    public nint DomainGet() => Il2CppFunctions.il2cpp_domain_get();
    public nint ThreadCurrent() => Il2CppFunctions.il2cpp_thread_current();
    public nint ThreadAttach(nint domain) => Il2CppFunctions.il2cpp_thread_attach(domain);
    public void ThreadDetach(nint thread) => Il2CppFunctions.il2cpp_thread_detach(thread);
    public nint ObjectUnbox(nint obj) => Il2CppFunctions.il2cpp_object_unbox(obj);
    public int FieldGetFlags(nint field) => Il2CppFunctions.il2cpp_field_get_flags(field);

    public bool IsReferenceField(nint field)
    {
        var type = Il2CppFunctions.il2cpp_field_get_type(field);
        if (type == 0) return false;

        var typeCode = (Il2CppTypeCode)Il2CppFunctions.il2cpp_type_get_type(type);
        if (IsDirectReferenceType((int)typeCode)) return true;
        return typeCode switch
        {
            Il2CppTypeCode.TypeParameter or
            Il2CppTypeCode.GenericInstance or
            Il2CppTypeCode.MethodTypeParameter => IsReferenceClass(type),
            _ => false,
        };
    }

    internal static bool IsDirectReferenceType(int typeCode)
        => (Il2CppTypeCode)typeCode is
            Il2CppTypeCode.String or
            Il2CppTypeCode.Class or
            Il2CppTypeCode.Array or
            Il2CppTypeCode.Object or
            Il2CppTypeCode.SzArray;

    private static bool IsReferenceClass(nint type)
    {
        var klass = Il2CppFunctions.il2cpp_class_from_type(type);
        return klass != 0 && !Il2CppFunctions.il2cpp_class_is_valuetype(klass);
    }

    public T GetFieldValue<T>(nint obj, nint field, bool isStatic) where T : unmanaged
    {
        T value = default;
        if (isStatic)
            Il2CppFunctions.il2cpp_field_static_get_value(field, &value);
        else
            Il2CppFunctions.il2cpp_field_get_value(obj, field, &value);
        return value;
    }

    public void SetFieldValue<T>(nint obj, nint field, bool isStatic, T value) where T : unmanaged
    {
        if (isStatic)
            Il2CppFunctions.il2cpp_field_static_set_value(field, &value);
        else
            Il2CppFunctions.il2cpp_field_set_value(obj, field, &value);
    }

    public void SetObjectFieldValue(nint obj, nint field, nint value)
        => Il2CppFunctions.il2cpp_field_set_value_object(obj, field, value);

    public nint RuntimeInvoke(nint method, nint obj, nint[]? args, out nint exception)
    {
        exception = 0;
        fixed (nint* p = args)
            return Il2CppFunctions.il2cpp_runtime_invoke(method, obj, (void**)p, ref exception);
    }

    public string FormatException(nint exception)
        => Format(exception, stackTrace: false);

    public string FormatStackTrace(nint exception)
        => Format(exception, stackTrace: true);

    private static string Format(nint exception, bool stackTrace)
    {
        Span<byte> buffer = stackalloc byte[FormatBufferSize];
        buffer.Clear();
        fixed (byte* ptr = buffer)
        {
            if (stackTrace)
                Il2CppFunctions.il2cpp_format_stack_trace(exception, ptr, buffer.Length);
            else
                Il2CppFunctions.il2cpp_format_exception(exception, ptr, buffer.Length);
        }

        var length = buffer.IndexOf((byte)0);
        if (length < 0) length = buffer.Length;
        return Encoding.UTF8.GetString(buffer[..length]).Trim();
    }
}
