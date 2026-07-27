using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StArray.ModManager.Mono;

/// <summary>
/// Mono Runtime P/Invoke 封装 —— 将 Methods 类的 unsafe 指针 API
/// 包装为与 Il2CppFunctions 一致的 nint/IntPtr 风格。
/// </summary>
public unsafe class MonoFunctions
{
    // ====================================================================
    //  JIT / Domain
    // ====================================================================

    public static nint MonoJitInit(string file)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(file + "\0"))
            return (nint)Methods.mono_jit_init((sbyte*)p);
    }

    public static nint MonoJitInitVersion(string name, string version)
    {
        fixed (byte* pn = Encoding.UTF8.GetBytes(name + "\0"))
        fixed (byte* pv = Encoding.UTF8.GetBytes(version + "\0"))
            return (nint)Methods.mono_jit_init_version((sbyte*)pn, (sbyte*)pv);
    }

    public static void MonoJitCleanup(nint domain)
        => Methods.mono_jit_cleanup((_MonoDomain*)domain);

    public static void MonoJitParseOptions(int argc, nint argv)
        => Methods.mono_jit_parse_options(argc, (sbyte**)argv);

    public static nint MonoGetRootDomain()
        => (nint)Methods.mono_get_root_domain();

    public static nint MonoDomainGet()
        => (nint)Methods.mono_domain_get();

    public static nint MonoDomainSet(nint domain, bool force)
        => (nint)Methods.mono_domain_set((_MonoDomain*)domain, force ? 1 : 0);

    public static nint MonoDomainAssemblyOpen(nint domain, string name)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
            return (nint)Methods.mono_domain_assembly_open((_MonoDomain*)domain, (sbyte*)p);
    }

    public static void MonoDomainUnload(nint domain)
        => Methods.mono_domain_unload((_MonoDomain*)domain);

    // ====================================================================
    //  Assembly / Image
    // ====================================================================

    public static nint MonoAssemblyOpen(string filename, out MonoImageOpenStatus status)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(filename + "\0"))
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_assembly_open((sbyte*)p, ps);
        }
    }

    public static nint MonoAssemblyLoad(nint aname, string basedir, out MonoImageOpenStatus status)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(basedir + "\0"))
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_assembly_load((_MonoAssemblyName*)aname, (sbyte*)p, ps);
        }
    }

    public static nint MonoAssemblyLoadFrom(nint image, string fname, out MonoImageOpenStatus status)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(fname + "\0"))
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_assembly_load_from((_MonoImage*)image, (sbyte*)p, ps);
        }
    }

    public static nint MonoAssemblyLoadWithPartialName(string name, out MonoImageOpenStatus status)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_assembly_load_with_partial_name((sbyte*)p, ps);
        }
    }

    public static nint MonoAssemblyGetImage(nint assembly)
        => (nint)Methods.mono_assembly_get_image((_MonoAssembly*)assembly);

    public static nint MonoAssemblyGetName(nint assembly)
        => (nint)Methods.mono_assembly_get_name((_MonoAssembly*)assembly);

    // ====================================================================
    //  Image
    // ====================================================================

    public static nint MonoImageOpen(string fname, out MonoImageOpenStatus status)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(fname + "\0"))
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_image_open((sbyte*)p, ps);
        }
    }

    public static nint MonoImageOpenFromData(byte[] data, bool needCopy, out MonoImageOpenStatus status)
    {
        fixed (byte* p = data)
        {
            fixed (MonoImageOpenStatus* ps = &status)
                return (nint)Methods.mono_image_open_from_data((sbyte*)p, (uint)data.Length, needCopy ? 1 : 0, ps);
        }
    }

    public static string? MonoImageGetName(nint image)
    {
        var p = Methods.mono_image_get_name((_MonoImage*)image);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static string? MonoImageGetFilename(nint image)
    {
        var p = Methods.mono_image_get_filename((_MonoImage*)image);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static nint MonoImageGetAssembly(nint image)
        => (nint)Methods.mono_image_get_assembly((_MonoImage*)image);

    public static nint MonoImageLoaded(string name)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
            return (nint)Methods.mono_image_loaded((sbyte*)p);
    }

    public static void MonoImageClose(nint image)
        => Methods.mono_image_close((_MonoImage*)image);

    public static nint MonoGetCorlib()
        => (nint)Methods.mono_get_corlib();

    // ====================================================================
    //  Class
    // ====================================================================

    public static nint MonoClassFromName(nint image, string namespaze, string name)
    {
        fixed (byte* pns = Encoding.UTF8.GetBytes(namespaze + "\0"))
        fixed (byte* pn = Encoding.UTF8.GetBytes(name + "\0"))
            return (nint)Methods.mono_class_from_name((_MonoImage*)image, (sbyte*)pns, (sbyte*)pn);
    }

    public static string? MonoClassGetName(nint klass)
    {
        var p = Methods.mono_class_get_name((_MonoClass*)klass);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static string? MonoClassGetNamespace(nint klass)
    {
        var p = Methods.mono_class_get_namespace((_MonoClass*)klass);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static nint MonoClassGetImage(nint klass)
        => (nint)Methods.mono_class_get_image((_MonoClass*)klass);

    public static nint MonoClassGetType(nint klass)
        => (nint)Methods.mono_class_get_type((_MonoClass*)klass);

    public static nint MonoClassGetParent(nint klass)
        => (nint)Methods.mono_class_get_parent((_MonoClass*)klass);

    public static nint MonoClassGetFields(nint klass, ref nint iter)
    {
        fixed (nint* p = &iter)
            return (nint)Methods.mono_class_get_fields((_MonoClass*)klass, (void**)p);
    }

    public static nint MonoClassGetMethods(nint klass, ref nint iter)
    {
        fixed (nint* p = &iter)
            return (nint)Methods.mono_class_get_methods((_MonoClass*)klass, (void**)p);
    }

    public static bool MonoClassIsValuetype(nint klass)
        => Methods.mono_class_is_valuetype((_MonoClass*)klass) != 0;

    public static uint MonoClassGetFlags(nint klass)
        => Methods.mono_class_get_flags((_MonoClass*)klass);

    public static bool MonoClassIsEnum(nint klass)
        => Methods.mono_class_is_enum((_MonoClass*)klass) != 0;

    public static nint MonoClassGetElementClass(nint klass)
        => (nint)Methods.mono_class_get_element_class((_MonoClass*)klass);

    public static nint MonoClassGetNestingType(nint klass)
        => (nint)Methods.mono_class_get_nesting_type((_MonoClass*)klass);

    public static int MonoClassNumFields(nint klass)
        => Methods.mono_class_num_fields((_MonoClass*)klass);

    public static int MonoClassNumMethods(nint klass)
        => Methods.mono_class_num_methods((_MonoClass*)klass);

    public static int MonoClassInstanceSize(nint klass)
        => Methods.mono_class_instance_size((_MonoClass*)klass);

    public static int MonoClassValueSize(nint klass, out uint align)
    {
        fixed (uint* p = &align)
            return Methods.mono_class_value_size((_MonoClass*)klass, p);
    }

    public static nint MonoClassVtable(nint domain, nint klass)
        => (nint)Methods.mono_class_vtable((_MonoDomain*)domain, (_MonoClass*)klass);

    // ====================================================================
    //  Method
    // ====================================================================

    public static nint MonoClassGetMethodFromName(nint klass, string name, int paramCount)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
            return (nint)Methods.mono_class_get_method_from_name((_MonoClass*)klass, (sbyte*)p, paramCount);
    }

    public static string? MonoMethodGetName(nint method)
    {
        var p = Methods.mono_method_get_name((_MonoMethod*)method);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static nint MonoMethodGetClass(nint method)
        => (nint)Methods.mono_method_get_class((_MonoMethod*)method);

    public static nint MonoMethodSignature(nint method)
        => (nint)Methods.mono_method_signature((_MonoMethod*)method);

    public static uint MonoMethodGetParamToken(nint method, int idx)
        => Methods.mono_method_get_param_token((_MonoMethod*)method, idx);

    public static uint MonoMethodGetFlags(nint method)
        => Methods.mono_method_get_flags((_MonoMethod*)method, null);

    public static nint MonoCompileMethod(nint method)
        => (nint)Methods.mono_compile_method((_MonoMethod*)method);

    public static int MonoClassInit(nint klass)
        => Methods.mono_class_init((_MonoClass*)klass);

    // ====================================================================
    //  Method Signature
    // ====================================================================

    public static uint MonoSignatureGetParamCount(nint sig)
        => Methods.mono_signature_get_param_count((_MonoMethodSignature*)sig);

    public static nint MonoSignatureGetReturnType(nint sig)
        => (nint)Methods.mono_signature_get_return_type((_MonoMethodSignature*)sig);

    public static nint MonoSignatureGetParams(nint sig, ref void* iter)
    {
        fixed (void** p = &iter)
            return (nint)Methods.mono_signature_get_params((_MonoMethodSignature*)sig, p);
    }

    // ====================================================================
    //  Type
    // ====================================================================

    public static string? MonoTypeGetName(nint type)
    {
        var p = Methods.mono_type_get_name((_MonoType*)type);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static string? MonoTypeGetNameFull(nint type, MonoTypeNameFormat format)
    {
        var p = Methods.mono_type_get_name_full((_MonoType*)type, format);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static nint MonoTypeGetClass(nint type)
        => (nint)Methods.mono_type_get_class((_MonoType*)type);

    public static int MonoTypeGetType(nint type)
        => Methods.mono_type_get_type((_MonoType*)type);

    public static nint MonoTypeGetUnderlyingType(nint type)
        => (nint)Methods.mono_type_get_underlying_type((_MonoType*)type);

    public static bool MonoTypeIsByref(nint type)
        => Methods.mono_type_is_byref((_MonoType*)type) != 0;

    // ====================================================================
    //  Object
    // ====================================================================

    public static nint MonoObjectNew(nint domain, nint klass)
        => (nint)Methods.mono_object_new((_MonoDomain*)domain, (_MonoClass*)klass);

    public static nint MonoObjectNewSpecific(nint vtable)
        => (nint)Methods.mono_object_new_specific((MonoVTable*)vtable);

    public static nint MonoObjectUnbox(nint obj)
    {
        try
        {
            return (nint)Methods.mono_object_unbox((_MonoObject*)obj);
        }
        catch (Exception)
        {
            return obj;
        }
    }

    public static nint MonoObjectGetClass(nint obj)
        => (nint)Methods.mono_object_get_class((_MonoObject*)obj);

    public static nint MonoValueBox(nint domain, nint klass, nint value)
        => (nint)Methods.mono_value_box((_MonoDomain*)domain, (_MonoClass*)klass, (void*)value);

    // ====================================================================
    //  String
    // ====================================================================

    public static nint MonoStringNew(nint domain, string text)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(text + "\0"))
            return (nint)Methods.mono_string_new((_MonoDomain*)domain, (sbyte*)p);
    }

    public static int MonoStringLength(nint str)
        => Methods.mono_string_length((_MonoString*)str);

    public static char* MonoStringChars(nint str)
        => (char*)Methods.mono_string_chars((_MonoString*)str);

    public static string? MonoStringToUTF8(nint str)
    {
        var p = Methods.mono_string_to_utf8((_MonoString*)str);
        if (p == null) return null;
        var result = Marshal.PtrToStringUTF8((nint)p);
        Methods.mono_free(p);
        return result;
    }

    // ====================================================================
    //  Array
    // ====================================================================

    public static nint MonoArrayNew(nint domain, nint eclass, nuint n)
        => (nint)Methods.mono_array_new((_MonoDomain*)domain, (_MonoClass*)eclass, n);

    public static nuint MonoArrayLength(nint arr)
        => Methods.mono_array_length((_MonoArray*)arr);

    public static nint MonoArrayAddrWithSize(nint arr, int elementSize, nuint index)
        => (nint)Methods.mono_array_addr_with_size((_MonoArray*)arr, elementSize, index);

    public static nint MonoArrayClassGet(nint elementClass, uint rank)
        => (nint)Methods.mono_array_class_get((_MonoClass*)elementClass, rank);

    public static int MonoArrayElementSize(nint ac)
        => Methods.mono_array_element_size((_MonoClass*)ac);

    // ====================================================================
    //  Field
    // ====================================================================

    public static nint MonoClassGetFieldFromName(nint klass, string name)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
            return (nint)Methods.mono_class_get_field_from_name((_MonoClass*)klass, (sbyte*)p);
    }

    public static string? MonoFieldGetName(nint field)
    {
        var p = Methods.mono_field_get_name((_MonoClassField*)field);
        return p != null ? Marshal.PtrToStringUTF8((nint)p) : null;
    }

    public static nint MonoFieldGetType(nint field)
        => (nint)Methods.mono_field_get_type((_MonoClassField*)field);

    public static nint MonoFieldGetParent(nint field)
        => (nint)Methods.mono_field_get_parent((_MonoClassField*)field);

    public static uint MonoFieldGetOffset(nint field)
        => Methods.mono_field_get_offset((_MonoClassField*)field);

    public static uint MonoFieldGetFlags(nint field)
        => Methods.mono_field_get_flags((_MonoClassField*)field);

    public static void MonoFieldGetValue(nint obj, nint field, nint value)
        => Methods.mono_field_get_value((_MonoObject*)obj, (_MonoClassField*)field, (void*)value);

    public static void MonoFieldStaticGetValue(nint vtable, nint field, nint value)
        => Methods.mono_field_static_get_value((MonoVTable*)vtable, (_MonoClassField*)field, (void*)value);

    public static void MonoFieldSetValue(nint obj, nint field, nint value)
        => Methods.mono_field_set_value((_MonoObject*)obj, (_MonoClassField*)field, (void*)value);

    public static void MonoFieldStaticSetValue(nint vtable, nint field, nint value)
        => Methods.mono_field_static_set_value((MonoVTable*)vtable, (_MonoClassField*)field, (void*)value);

    // ====================================================================
    //  Runtime Invoke
    // ====================================================================

    public static nint MonoRuntimeInvoke(nint method, nint obj, nint[]? args, out nint exc)
    {
        fixed (nint* p = args)
        {
            fixed (nint* pe = &exc)
            {
                exc = 0;
                return (nint)Methods.mono_runtime_invoke((_MonoMethod*)method, (void*)obj, (void**)p, (_MonoObject**)pe);
            }
        }
    }

    public static void MonoRuntimeObjectInit(nint obj)
        => Methods.mono_runtime_object_init((_MonoObject*)obj);

    // ====================================================================
    //  GC / GCHandle
    // ====================================================================

    public static uint MonoGCHandleNew(nint obj, bool pinned)
        => Methods.mono_gchandle_new((_MonoObject*)obj, pinned ? 1 : 0);

    public static nint MonoGCHandleGetTarget(uint gchandle)
        => (nint)Methods.mono_gchandle_get_target(gchandle);

    public static void MonoGCHandleFree(uint gchandle)
        => Methods.mono_gchandle_free(gchandle);

    // ====================================================================
    //  Config / Misc
    // ====================================================================

    public static void MonoConfigParse(string filename)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(filename + "\0"))
            Methods.mono_config_parse((sbyte*)p);
    }

    public static void MonoConfigParseMemory(string buffer)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(buffer + "\0"))
            Methods.mono_config_parse_memory((sbyte*)p);
    }

    public static void MonoSetAssembliesPath(string path)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(path + "\0"))
            Methods.mono_set_assemblies_path((sbyte*)p);
    }

    public static void MonoFree(nint ptr)
        => Methods.mono_free((void*)ptr);

    public static void MonoRaiseException(nint ex)
        => Methods.mono_raise_exception((_MonoException*)ex);

    public static void MonoAddInternalCall(string name, nint method)
    {
        fixed (byte* p = Encoding.UTF8.GetBytes(name + "\0"))
            Methods.mono_add_internal_call((sbyte*)p, (void*)method);
    }

    // ====================================================================
    //  Assembly Load Hook
    // ====================================================================

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void AssemblyLoadHookCallback(_MonoAssembly* assembly, void* userData)
    {
        var image = MonoAssemblyGetImage((nint)assembly);
        var name = MonoImageGetName(image);
        if (name != null)
        {
            MonoDomain.OnAssemblyLoad(name, (nint)assembly);
        }
    }

    public static void InstallAssemblyLoadHook()
    {
        Methods.mono_install_assembly_load_hook(&AssemblyLoadHookCallback, null);
    }

    // ====================================================================
    //  Assembly Enumeration
    // ====================================================================

    public static void MonoAssemblyForeach(Action<nint> callback)
    {
        var handle = GCHandle.Alloc(callback);
        try
        {
            Methods.mono_assembly_foreach(&AssemblyForeachCallback, (void*)GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void AssemblyForeachCallback(void* assembly, void* userData)
    {
        var handle = GCHandle.FromIntPtr((nint)userData);
        if (handle.IsAllocated && handle.Target is Action<nint> callback)
            callback((nint)assembly);
    }

    // ====================================================================
    //  Security
    // ====================================================================

    public static void MonoSecurityEnableCoreCLR()
        => Methods.mono_security_enable_core_clr();
}
