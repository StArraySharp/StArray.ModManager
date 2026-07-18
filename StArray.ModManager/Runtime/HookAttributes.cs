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

    /// <summary>
    /// 标记一个方法为托管运行时（Mono / Il2Cpp）函数 Hook。
    /// 3 参数构造函数 Namespace 默认为 ""（Il2Cpp 风格）；
    /// 4 参数构造函数可指定 Namespace（Mono 风格）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnmanagedHookAttribute : Attribute
    {
        public string AssemblyName { get; }
        public string? Namespace { get; set; }
        public string ClassName { get; }
        public string MethodName { get; }
        public int ParameterCount { get; set; } = -1;
        public string[]? ParameterTypeNames { get; set; }
        public CallingConvention Convention { get; set; } = CallingConvention.Cdecl;

        public UnmanagedHookAttribute(string assembly, string className, string methodName)
        { AssemblyName = assembly; ClassName = className; MethodName = methodName; }

        public UnmanagedHookAttribute(string assembly, string ns, string className, string methodName)
        { AssemblyName = assembly; Namespace = ns; ClassName = className; MethodName = methodName; }
    }
}