namespace StArray.ModManager.Il2Cpp;

public sealed class Il2CppInvocationException : Exception
{
    public nint NativeException { get; }
    public string NativeStackTrace { get; }

    private Il2CppInvocationException(
        string message,
        nint nativeException,
        string nativeStackTrace)
        : base(message)
    {
        NativeException = nativeException;
        NativeStackTrace = nativeStackTrace;
    }

    internal static Il2CppInvocationException Create(nint nativeException, string context)
    {
        var api = Il2CppRuntimeApi.Current;
        var nativeMessage = TryFormat(
            () => api.FormatException(nativeException),
            $"native exception 0x{nativeException:X}");
        var nativeStackTrace = TryFormat(() => api.FormatStackTrace(nativeException), "");
        return new Il2CppInvocationException(
            $"{context}: {nativeMessage}",
            nativeException,
            nativeStackTrace);
    }

    public override string ToString()
    {
        var managed = base.ToString();
        return string.IsNullOrWhiteSpace(NativeStackTrace)
            ? managed
            : $"{managed}{Environment.NewLine}--- IL2CPP native stack ---{Environment.NewLine}{NativeStackTrace}";
    }

    private static string TryFormat(Func<string> formatter, string fallback)
    {
        try
        {
            var value = formatter();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch (Exception ex)
        {
            return string.IsNullOrEmpty(fallback)
                ? $"<format failed: {ex.GetType().Name}>"
                : $"{fallback} (format failed: {ex.GetType().Name})";
        }
    }
}
