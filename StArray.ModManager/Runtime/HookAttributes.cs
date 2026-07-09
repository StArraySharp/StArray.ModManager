using System.Runtime.InteropServices;

namespace StArray.ModManager.Hooks
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeHookAttribute : Attribute
    {
        public string Library { get; }
        public string Symbol { get; }
        public ulong Address { get; }
        public CallingConvention Convention { get; set; }
        public bool Enabled { get; set; } = true;
        public NativeHookAttribute(string library, string symbol)
        { Library = library; Symbol = symbol; Convention = CallingConvention.StdCall; }
        public NativeHookAttribute(ulong address)
        { Address = address; Convention = CallingConvention.StdCall; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class Il2CppHookAttribute : Attribute
    {
        public string AssemblyName { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public int ParameterCount { get; set; } = -1;
        public string[] ParameterTypeNames { get; set; }
        public Il2CppHookAttribute(string asm, string cls, string method)
        { AssemblyName = asm; ClassName = cls; MethodName = method; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MonoHookAttribute : Attribute
    {
        public string AssemblyName { get; }
        public string Namespace { get; }
        public string ClassName { get; }
        public string MethodName { get; }
        public int ParameterCount { get; set; } = -1;
        public string[] ParameterTypeNames { get; set; }
        public MonoHookAttribute(string asm, string ns, string cls, string method)
        { AssemblyName = asm; Namespace = ns; ClassName = cls; MethodName = method; }
    }
}