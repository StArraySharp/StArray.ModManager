using System.Runtime.InteropServices;

namespace StArray.ModManager;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class NativeImportAttribute : Attribute
{
    public string? Library { get; }
    public string? EntryPoint { get; init; }
    public CallingConvention Convention { get; init; } = CallingConvention.Cdecl;
    public CharSet CharSet { get; init; } = CharSet.Ansi;

    public NativeImportAttribute()
    {
    }

    public NativeImportAttribute(string library)
    {
        Library = library;
    }
}
