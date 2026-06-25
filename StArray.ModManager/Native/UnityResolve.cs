using System.Runtime.InteropServices;

namespace StArray.ModManager.Native;

// ============================================================================
// UnityResolve — 实例化的 Unity IL2CPP/Mono 反射 API
// ============================================================================

/// <summary>
/// UnityResolve 反射引擎 — 遍历 Unity IL2CPP/Mono 托管类型。
/// 必须在 <c>Init()</c> 后使用。
/// </summary>
/// <example>
/// <code>
/// var resolve = new UnityResolve();
/// resolve.Init(monoHandle, UnityResolve.ResolveMode.Mono);
/// var asm = resolve.GetAssembly("UnityEngine.CoreModule.dll");
/// var cls = asm.GetClass("UnityEngine", "Time");
/// var method = cls.GetMethod("get_time", 0);
/// float time = method.InvokeUnbox&lt;float&gt;();
/// </code>
/// </example>
public class UnityResolve
{
    private const string Lib = "modmanager";

    // ========================================================================
    // ResolveMode
    // ========================================================================
    public enum ResolveMode { Mono = 0, Il2Cpp = 1 }

    private bool _initialized;

    // ========================================================================
    // Init / Lifecycle
    // ========================================================================

    /// <summary>初始化 UnityResolve 并遍历所有程序集。</summary>
    public void Init(nint hmodule, ResolveMode mode)
    {
        _NativeInit(hmodule, (int)mode);
        _initialized = true;
    }

    /// <summary>将当前线程附加到 Mono/Il2Cpp 域。</summary>
    public void ThreadAttach() => _NativeThreadAttach();

    /// <summary>将当前线程从 Mono/Il2Cpp 域分离。</summary>
    public void ThreadDetach() => _NativeThreadDetach();

    /// <summary>
    /// 导出所有程序集的反射信息到文件。
    /// 生成 path/dump.cs（类/方法/字段声明）和 path/struct.hpp（C++ 内存布局）。
    /// </summary>
    /// <param name="path">输出目录路径</param>
    public void DumpToFile(string path)
    {
        EnsureInit();
        _NativeDumpToFile(path);
    }

    // ========================================================================
    // Assembly access
    // ========================================================================

    /// <summary>按名称获取程序集。</summary>
    public Assembly? GetAssembly(string name)
    {
        EnsureInit();
        var ptr = _NativeGetAssembly(name);
        return ptr != IntPtr.Zero ? new Assembly(ptr) : null;
    }

    /// <summary>获取已遍历的程序集总数。</summary>
    public int AssemblyCount
    {
        get { EnsureInit(); return _NativeAssemblyCount(); }
    }

    /// <summary>按索引获取程序集。</summary>
    public Assembly? GetAssemblyAt(int index)
    {
        EnsureInit();
        var ptr = _NativeAssemblyAt(index);
        return ptr != IntPtr.Zero ? new Assembly(ptr) : null;
    }

    /// <summary>遍历所有程序集。</summary>
    public IEnumerable<Assembly> Assemblies
    {
        get
        {
            EnsureInit();
            int count = AssemblyCount;
            for (int i = 0; i < count; i++)
            {
                var a = GetAssemblyAt(i);
                if (a != null) yield return a;
            }
        }
    }

    // ========================================================================
    // Sub-class: Assembly
    // ========================================================================
    public class Assembly
    {
        internal readonly nint _ptr;

        internal Assembly(nint ptr) => _ptr = ptr;

        public nint NativePtr => _ptr;
        public bool IsValid => _ptr != IntPtr.Zero;

        /// <summary>获取程序集名称。e.g. "UnityEngine.CoreModule.dll"</summary>
        public string Name => ReadAnsiString(_NativeAssemblyGetName(_ptr));

        /// <summary>按命名空间和类名查找类。</summary>
        public Class? GetClass(string namespaze, string name)
        {
            var ptr = _NativeClassGet(_ptr, namespaze, name);
            return ptr != IntPtr.Zero ? new Class(ptr) : null;
        }

        /// <summary>程序集中的类数量。</summary>
        public int ClassCount => _NativeAssemblyGetClassCount(_ptr);

        /// <summary>按索引获取类。</summary>
        public Class? GetClassAt(int index)
        {
            var ptr = _NativeAssemblyGetClassAt(_ptr, index);
            return ptr != IntPtr.Zero ? new Class(ptr) : null;
        }

        /// <summary>遍历所有类。</summary>
        public IEnumerable<Class> Classes
        {
            get
            {
                int count = ClassCount;
                for (int i = 0; i < count; i++)
                {
                    var c = GetClassAt(i);
                    if (c != null) yield return c;
                }
            }
        }

        public static implicit operator nint(Assembly? a) => a?._ptr ?? IntPtr.Zero;
        public static explicit operator Assembly(nint ptr) => new(ptr);
    }

    // ========================================================================
    // Sub-class: Class
    // ========================================================================
    public class Class
    {
        internal readonly nint _ptr;

        internal Class(nint ptr) => _ptr = ptr;

        public nint NativePtr => _ptr;
        public bool IsValid => _ptr != IntPtr.Zero;

        /// <summary>类名。</summary>
        public string Name => ReadAnsiString(_NativeClassGetName(_ptr));

        /// <summary>命名空间。</summary>
        public string Namespace => ReadAnsiString(_NativeClassGetNamespace(_ptr));

        /// <summary>全限定名。</summary>
        public string FullName => string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;

        /// <summary>按名称查找方法（不限制参数个数）。</summary>
        public Method? GetMethod(string name)
        {
            var ptr = _NativeMethodGet(_ptr, name);
            return ptr != IntPtr.Zero ? new Method(ptr) : null;
        }

        /// <summary>按名称和参数数量查找方法。</summary>
        public Method? GetMethod(string name, int paramCount)
        {
            var ptr = _NativeMethodGetWithParams(_ptr, name, paramCount);
            return ptr != IntPtr.Zero ? new Method(ptr) : null;
        }

        /// <summary>按名称查找字段。</summary>
        public Field? GetField(string name)
        {
            var ptr = _NativeFieldGet(_ptr, name);
            return ptr != IntPtr.Zero ? new Field(ptr) : null;
        }

        /// <summary>类中的方法数量。</summary>
        public int MethodCount => _NativeClassGetMethodCount(_ptr);

        /// <summary>按索引获取方法。</summary>
        public Method? GetMethodAt(int index)
        {
            var ptr = _NativeClassGetMethodAt(_ptr, index);
            return ptr != IntPtr.Zero ? new Method(ptr) : null;
        }

        /// <summary>遍历所有方法。</summary>
        public IEnumerable<Method> Methods
        {
            get
            {
                int count = MethodCount;
                for (int i = 0; i < count; i++)
                {
                    var m = GetMethodAt(i);
                    if (m != null) yield return m;
                }
            }
        }

        /// <summary>类中的字段数量。</summary>
        public int FieldCount => _NativeClassGetFieldCount(_ptr);

        /// <summary>按索引获取字段。</summary>
        public Field? GetFieldAt(int index)
        {
            var ptr = _NativeClassGetFieldAt(_ptr, index);
            return ptr != IntPtr.Zero ? new Field(ptr) : null;
        }

        /// <summary>遍历所有字段。</summary>
        public IEnumerable<Field> Fields
        {
            get
            {
                int count = FieldCount;
                for (int i = 0; i < count; i++)
                {
                    var f = GetFieldAt(i);
                    if (f != null) yield return f;
                }
            }
        }

        public static implicit operator nint(Class? c) => c?._ptr ?? IntPtr.Zero;
        public static explicit operator Class(nint ptr) => new(ptr);
    }

    // ========================================================================
    // Sub-class: Method
    // ========================================================================
    public class Method
    {
        internal readonly nint _ptr;

        internal Method(nint ptr) => _ptr = ptr;

        public nint NativePtr => _ptr;
        public bool IsValid => _ptr != IntPtr.Zero;

        /// <summary>方法名。</summary>
        public string Name => ReadAnsiString(_NativeMethodGetName(_ptr));

        /// <summary>是否为静态方法。</summary>
        public bool IsStatic => _NativeMethodIsStatic(_ptr) != 0;

        /// <summary>参数个数。</summary>
        public int ParamCount => _NativeMethodGetParamCount(_ptr);

        /// <summary>按索引获取参数名。</summary>
        public string GetParamName(int index)
            => ReadAnsiString(_NativeMethodGetParamName(_ptr, index));

        /// <summary>返回类型名。</summary>
        public string ReturnTypeName => ReadAnsiString(_NativeMethodGetReturnTypeName(_ptr));

        /// <summary>获取原生函数指针（需先 Compile）。</summary>
        public nint FunctionPtr
        {
            get
            {
                Compile();
                return _NativeMethodGetFunction(_ptr);
            }
        }

        /// <summary>编译（Mono 下 JIT 编译 IL 为原生代码）。</summary>
        public void Compile() => _NativeMethodCompile(_ptr);

        /// <summary>
        /// 通过 runtime_invoke 调用实例方法。
        /// 返回托管对象指针，值类型需 Unbox。
        /// </summary>
        public nint Invoke(nint obj, nint[]? args = null)
            => _NativeMethodRuntimeInvoke(_ptr, obj, args, args?.Length ?? 0);

        /// <summary>调用静态方法（obj = IntPtr.Zero）。</summary>
        public nint InvokeStatic(nint[]? args = null)
            => Invoke(IntPtr.Zero, args);

        /// <summary>调用静态方法并 Unbox 返回值。</summary>
        public unsafe T InvokeStaticUnbox<T>(nint[]? args = null) where T : unmanaged
        {
            nint ret = InvokeStatic(args);
            if (ret == IntPtr.Zero) return default;
            nint unboxed = _NativeObjectUnbox(ret);
            if (unboxed == IntPtr.Zero) return default;
            return *(T*)unboxed;
        }

        /// <summary>调用实例方法并 Unbox 返回值。</summary>
        public unsafe T InvokeUnbox<T>(nint obj, nint[]? args = null) where T : unmanaged
        {
            nint ret = Invoke(obj, args);
            if (ret == IntPtr.Zero) return default;
            nint unboxed = _NativeObjectUnbox(ret);
            if (unboxed == IntPtr.Zero) return default;
            return *(T*)unboxed;
        }

        private static readonly List<Delegate> _hooks = new();

        /// <summary>安装 inline hook / Install inline hook. Returns true on success (0).</summary>
        public bool Hook<T>(T replace, out T original) where T : Delegate
        {
            var result = Dobby.Hook(FunctionPtr, Marshal.GetFunctionPointerForDelegate(replace), out var origin);
            original = Marshal.GetDelegateForFunctionPointer<T>(origin);
            if (result == 0) { _hooks.Add(replace); _hooks.Add(original); }
            return result == 0;
        }

        /// <summary>移除 hook / Remove hook. Returns true on success (0).</summary>
        public bool Unhook()
        {
            return Dobby.Destroy(FunctionPtr) == 0;
        }

        /// <summary>内存代码补丁 / Apply code patch at function address.</summary>
        public bool Patch(byte[] code)
        {
            return Dobby.CodePatch(FunctionPtr, code, (uint)code.Length) == 0;
        }

        public static implicit operator nint(Method? m) => m?._ptr ?? IntPtr.Zero;
        public static explicit operator Method(nint ptr) => new(ptr);
    }

    // ========================================================================
    // Sub-class: Field
    // ========================================================================
    public class Field
    {
        internal readonly nint _ptr;

        internal Field(nint ptr) => _ptr = ptr;

        public nint NativePtr => _ptr;
        public bool IsValid => _ptr != IntPtr.Zero;

        /// <summary>字段名。</summary>
        public string Name => ReadAnsiString(_NativeFieldGetName(_ptr));

        /// <summary>字段偏移量（-1 = 静态字段）。</summary>
        public int Offset => _NativeFieldGetOffset(_ptr);

        /// <summary>是否为静态字段。</summary>
        public bool IsStatic => _NativeFieldIsStatic(_ptr) != 0;

        /// <summary>字段类型名。</summary>
        public string TypeName => ReadAnsiString(_NativeFieldGetTypeName(_ptr));

        /// <summary>从实例对象读取字段值（按偏移量，返回指针）。</summary>
        public nint GetValuePtr(nint obj)
        {
            nint addr = obj + Offset;
            unsafe { return *(nint*)addr; }
        }

        /// <summary>向实例对象写入字段值（按偏移量，指针值）。</summary>
        public void SetValuePtr(nint obj, nint value)
        {
            nint addr = obj + Offset;
            unsafe { *(nint*)addr = value; }
        }

        /// <summary>读取泛型值类型字段。</summary>
        public unsafe T GetValue<T>(nint obj) where T : unmanaged
        {
            nint addr = obj + Offset;
            return *(T*)addr;
        }

        /// <summary>写入泛型值类型字段。</summary>
        public unsafe void SetValue<T>(nint obj, T value) where T : unmanaged
        {
            nint addr = obj + Offset;
            *(T*)addr = value;
        }

        /// <summary>获取静态字段值（泛型值类型）。</summary>
        public unsafe T GetStaticValue<T>() where T : unmanaged
        {
            T value = default;
            _NativeFieldGetStaticValue(_ptr, (nint)(&value));
            return value;
        }

        /// <summary>设置静态字段值（泛型值类型）。</summary>
        public unsafe void SetStaticValue<T>(T value) where T : unmanaged
        {
            _NativeFieldSetStaticValue(_ptr, (nint)(&value));
        }

        /// <summary>获取静态字段值到指定缓冲区。</summary>
        public void GetStaticValue(nint outPtr) => _NativeFieldGetStaticValue(_ptr, outPtr);

        /// <summary>设置静态字段值从指定缓冲区。</summary>
        public void SetStaticValue(nint valuePtr) => _NativeFieldSetStaticValue(_ptr, valuePtr);

        public static implicit operator nint(Field? f) => f?._ptr ?? IntPtr.Zero;
        public static explicit operator Field(nint ptr) => new(ptr);
    }

    // ========================================================================
    // Internal helpers
    // ========================================================================
    private void EnsureInit()
    {
        if (!_initialized)
            throw new InvalidOperationException("UnityResolve not initialized. Call Init() first.");
    }

    private static string ReadAnsiString(nint ptr)
    {
        if (ptr == IntPtr.Zero) return string.Empty;
        return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
    }

    // ========================================================================
    // Native P/Invoke — all private static
    // ========================================================================

    [DllImport(Lib, EntryPoint = "modmanager_resolve_init")]
    private static extern int _NativeInit(nint hmodule, int mode);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_init_il2cpp")]
    private static extern int _NativeInitIl2Cpp();

    /// <summary>
    /// 初始化 UnityResolve（Il2Cpp 模式，自动获取 libil2cpp.so 句柄）。
    /// 内部使用 dlopen+RTLD_NOLOAD，不会重新加载库。
    /// </summary>
    public void InitIl2Cpp()
    {
        _NativeInitIl2Cpp();
        _initialized = true;
    }

    [DllImport(Lib, EntryPoint = "modmanager_resolve_thread_attach")]
    private static extern void _NativeThreadAttach();

    [DllImport(Lib, EntryPoint = "modmanager_resolve_thread_detach")]
    private static extern void _NativeThreadDetach();

    [DllImport(Lib, EntryPoint = "modmanager_resolve_dump_to_file")]
    private static extern void _NativeDumpToFile(string path);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_get_assembly")]
    private static extern nint _NativeGetAssembly(string name);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_assembly_get_name")]
    private static extern nint _NativeAssemblyGetName(nint assembly);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_assembly_count")]
    private static extern int _NativeAssemblyCount();

    [DllImport(Lib, EntryPoint = "modmanager_resolve_assembly_at")]
    private static extern nint _NativeAssemblyAt(int index);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get")]
    private static extern nint _NativeClassGet(nint assembly, string namespaze, string name);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_name")]
    private static extern nint _NativeClassGetName(nint klass);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_namespace")]
    private static extern nint _NativeClassGetNamespace(nint klass);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get")]
    private static extern nint _NativeMethodGet(nint klass, string name);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_with_params")]
    private static extern nint _NativeMethodGetWithParams(nint klass, string name, int paramCount);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_name")]
    private static extern nint _NativeMethodGetName(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_compile")]
    private static extern void _NativeMethodCompile(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_function")]
    private static extern nint _NativeMethodGetFunction(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_is_static")]
    private static extern int _NativeMethodIsStatic(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_runtime_invoke")]
    private static extern nint _NativeMethodRuntimeInvoke(nint method, nint obj, nint[]? args, int argCount);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_object_unbox")]
    private static extern nint _NativeObjectUnbox(nint obj);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get")]
    private static extern nint _NativeFieldGet(nint klass, string name);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get_name")]
    private static extern nint _NativeFieldGetName(nint field);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get_offset")]
    private static extern int _NativeFieldGetOffset(nint field);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_is_static")]
    private static extern int _NativeFieldIsStatic(nint field);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get_value")]
    private static extern nint _NativeFieldGetValue(nint obj, int offset);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_set_value")]
    private static extern void _NativeFieldSetValue(nint obj, int offset, nint value);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_set_static_value")]
    private static extern void _NativeFieldSetStaticValue(nint field, nint valuePtr);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get_static_value")]
    private static extern void _NativeFieldGetStaticValue(nint field, nint outPtr);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_return_type_name")]
    private static extern nint _NativeMethodGetReturnTypeName(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_param_count")]
    private static extern int _NativeMethodGetParamCount(nint method);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_method_get_param_name")]
    private static extern nint _NativeMethodGetParamName(nint method, int index);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_method_count")]
    private static extern int _NativeClassGetMethodCount(nint klass);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_method_at")]
    private static extern nint _NativeClassGetMethodAt(nint klass, int index);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_field_count")]
    private static extern int _NativeClassGetFieldCount(nint klass);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_class_get_field_at")]
    private static extern nint _NativeClassGetFieldAt(nint klass, int index);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_assembly_get_class_count")]
    private static extern int _NativeAssemblyGetClassCount(nint assembly);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_assembly_get_class_at")]
    private static extern nint _NativeAssemblyGetClassAt(nint assembly, int index);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_field_get_type_name")]
    private static extern nint _NativeFieldGetTypeName(nint field);

    [DllImport(Lib, EntryPoint = "modmanager_resolve_invoke_static")]
    private static extern nint _NativeInvokeStatic(
        string assemblyName, string namespaze, string className,
        string methodName, nint[]? args, int argCount);
}
