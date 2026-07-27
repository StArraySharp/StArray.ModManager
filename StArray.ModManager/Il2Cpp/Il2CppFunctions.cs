using System.Runtime.InteropServices;

namespace StArray.ModManager.Il2Cpp;

public unsafe class Il2CppFunctions
{

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_init(IntPtr domain_name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_init_utf16(IntPtr domain_name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_shutdown();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_config_dir(IntPtr config_path);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_data_dir(IntPtr data_path);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_temp_dir(IntPtr temp_path);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_commandline_arguments(int argc, IntPtr argv, IntPtr basedir);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_commandline_arguments_utf16(int argc, IntPtr argv, IntPtr basedir);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_config_utf16(IntPtr executablePath);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_config(IntPtr executablePath);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_memory_callbacks(IntPtr callbacks);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_get_corlib();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_add_internal_call(IntPtr name, IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_alloc(uint size);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_free(IntPtr ptr);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_array_class_get(IntPtr element_class, uint rank);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_array_length(IntPtr array);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_array_get_byte_length(IntPtr array);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_array_new(IntPtr elementTypeInfo, ulong length);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_array_new_specific(IntPtr arrayTypeInfo, ulong length);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_array_new_full(IntPtr array_class, ref ulong lengths, ref ulong lower_bounds);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_bounded_array_class_get(IntPtr element_class, uint rank,
        [MarshalAs(UnmanagedType.I1)] bool bounded);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_array_element_size(IntPtr array_class);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_assembly_get_image(IntPtr assembly);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_enum_basetype(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_generic(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_inflated(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_assignable_from(IntPtr klass, IntPtr oklass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_subclass_of(IntPtr klass, IntPtr klassc,
        [MarshalAs(UnmanagedType.I1)] bool check_interfaces);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_has_parent(IntPtr klass, IntPtr klassc);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_from_il2cpp_type(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_from_name(IntPtr image,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string namespaze,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_from_system_type(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_element_class(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_events(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_fields(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_nested_types(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_interfaces(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_properties(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_property_from_name(IntPtr klass, IntPtr name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_field_from_name(IntPtr klass,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_methods(IntPtr klass, ref IntPtr iter);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_method_from_name(IntPtr klass,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int argsCount);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_class_get_name(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_class_get_namespace(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_parent(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_declaring_type(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_class_instance_size(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_class_num_fields(IntPtr enumKlass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_valuetype(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_class_value_size(IntPtr klass, ref uint align);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_blittable(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_class_get_flags(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_abstract(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_interface(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_class_array_element_size(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_from_type(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_type(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_class_get_type_token(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_has_attribute(IntPtr klass, IntPtr attr_class);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_has_references(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_class_is_enum(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_class_get_image(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_class_get_assemblyname(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_class_get_rank(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_class_get_bitmap_size(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_class_get_bitmap(IntPtr klass, ref uint bitmap);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_stats_dump_to_file(IntPtr path);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_domain_get();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_domain_assembly_open(IntPtr domain, string name);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr* il2cpp_domain_get_assemblies(IntPtr domain, ref uint size);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr
        il2cpp_exception_from_name_msg(IntPtr image, IntPtr name_space, IntPtr name, IntPtr msg);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_get_exception_argument_null(IntPtr arg);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_format_exception(IntPtr ex, void* message, int message_size);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_format_stack_trace(IntPtr ex, void* output, int output_size);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_unhandled_exception(IntPtr ex);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_field_get_flags(IntPtr field);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_field_get_name(IntPtr field);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_field_get_parent(IntPtr field);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_field_get_offset(IntPtr field);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_field_get_type(IntPtr field);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_field_get_value(IntPtr obj, IntPtr field, void* value);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_field_get_value_object(IntPtr field, IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_field_has_attribute(IntPtr field, IntPtr attr_class);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_field_set_value(IntPtr obj, IntPtr field, void* value);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_field_static_get_value(IntPtr field, void* value);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_field_static_set_value(IntPtr field, void* value);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_field_set_value_object(IntPtr instance, IntPtr field, IntPtr value);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_gc_collect(int maxGenerations);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_gc_collect_a_little();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_gc_disable();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_gc_enable();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_gc_is_disabled();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern long il2cpp_gc_get_used_size();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern long il2cpp_gc_get_heap_size();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_gc_wbarrier_set_field(IntPtr obj, IntPtr targetAddress, IntPtr gcObj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_gchandle_new(IntPtr obj, [MarshalAs(UnmanagedType.I1)] bool pinned);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_gchandle_new_weakref(IntPtr obj,
        [MarshalAs(UnmanagedType.I1)] bool track_resurrection);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_gchandle_get_target(nint gchandle);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_gchandle_free(nint gchandle);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_unity_liveness_calculation_begin(IntPtr filter, int max_object_count,
        IntPtr callback, IntPtr userdata, IntPtr onWorldStarted, IntPtr onWorldStopped);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_unity_liveness_calculation_end(IntPtr state);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_unity_liveness_calculation_from_root(IntPtr root, IntPtr state);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_unity_liveness_calculation_from_statics(IntPtr state);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_method_get_return_type(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_method_get_declaring_type(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_method_get_name(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr il2cpp_method_get_from_reflection(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_method_get_object(IntPtr method, IntPtr refClass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_method_is_generic(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_method_is_inflated(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_method_is_instance(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_method_get_param_count(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_method_get_param(IntPtr method, uint index);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_method_get_class(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_method_has_attribute(IntPtr method, IntPtr attr_class);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_method_get_flags(IntPtr method, ref uint iflags);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_method_get_token(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_method_get_param_name(IntPtr method, uint index);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install(IntPtr prof, IntPtr shutdown_callback);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install_enter_leave(IntPtr enter, IntPtr fleave);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install_allocation(IntPtr callback);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install_gc(IntPtr callback, IntPtr heap_resize_callback);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install_fileio(IntPtr callback);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_profiler_install_thread(IntPtr start, IntPtr end);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_property_get_flags(IntPtr prop);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_property_get_get_method(IntPtr prop);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_property_get_set_method(IntPtr prop);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_property_get_name(IntPtr prop);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_property_get_parent(IntPtr prop);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_object_get_class(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_object_get_size(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_object_get_virtual_method(IntPtr obj, IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_object_new(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_object_unbox(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_value_box(IntPtr klass, IntPtr data);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_monitor_enter(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_monitor_try_enter(IntPtr obj, uint timeout);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_monitor_exit(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_monitor_pulse(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_monitor_pulse_all(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_monitor_wait(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_monitor_try_wait(IntPtr obj, uint timeout);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_runtime_invoke(IntPtr method, IntPtr obj, void** param, ref IntPtr exc);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_runtime_invoke_convert_args(IntPtr method, IntPtr obj, void** param,
        int paramCount, ref IntPtr exc);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_runtime_class_init(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_runtime_object_init(IntPtr obj);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_runtime_object_init_exception(IntPtr obj, ref IntPtr exc);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_string_length(IntPtr str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern char* il2cpp_string_chars(IntPtr str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_new(string str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_new_len(string str, uint length);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_new_utf16(char* text, int len);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_new_wrapper(string str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_intern(string str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_string_is_interned(string str);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_thread_current();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_thread_attach(IntPtr domain);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_thread_detach(IntPtr thread);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void** il2cpp_thread_get_all_attached_threads(ref uint size);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_is_vm_thread(IntPtr thread);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_current_thread_walk_frame_stack(IntPtr func, IntPtr user_data);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_thread_walk_frame_stack(IntPtr thread, IntPtr func, IntPtr user_data);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_current_thread_get_top_frame(IntPtr frame);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_thread_get_top_frame(IntPtr thread, IntPtr frame);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_current_thread_get_frame_at(int offset, IntPtr frame);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_thread_get_frame_at(IntPtr thread, int offset, IntPtr frame);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_current_thread_get_stack_depth();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_thread_get_stack_depth(IntPtr thread);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_type_get_object(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int il2cpp_type_get_type(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_type_get_class_or_element_class(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_type_get_name(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_type_is_byref(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_type_get_attrs(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_type_equals(IntPtr type, IntPtr otherType);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_type_get_assembly_qualified_name(IntPtr type);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_image_get_assembly(IntPtr image);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_image_get_name(IntPtr image);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern nint il2cpp_image_get_filename(IntPtr image);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_image_get_entry_point(IntPtr image);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern uint il2cpp_image_get_class_count(IntPtr image);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_image_get_class(IntPtr image, uint index);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_capture_memory_snapshot();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_free_captured_memory_snapshot(IntPtr snapshot);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_set_find_plugin_callback(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_register_log_callback(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_debugger_set_agent_options(IntPtr options);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_is_debugger_attached();

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_unity_install_unitytls_interface(void* unitytlsInterfaceStruct);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_custom_attrs_from_class(IntPtr klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_custom_attrs_from_method(IntPtr method);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_custom_attrs_get_attr(IntPtr ainfo, IntPtr attr_klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool il2cpp_custom_attrs_has_attr(IntPtr ainfo, IntPtr attr_klass);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr il2cpp_custom_attrs_construct(IntPtr cinfo);

    [DllImport("IL2CPP_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void il2cpp_custom_attrs_free(IntPtr ainfo);

    // ── 库解析 ──
    //
    // DllImport 的库名必须是编译期常量，而 il2cpp 宿主库的路径要到运行时才知道，
    // 所以上面统一写成占位符，由 DllImportResolver 转成真实路径。

    private const string LibraryPlaceholder = "IL2CPP_LIBRARY_NAME";

    private static readonly object _resolverLock = new();
    private static string? _libraryPath;
    private static IntPtr _libraryHandle;
    private static bool _resolverRegistered;

    /// <summary>
    /// 指定 il2cpp 宿主库路径。可安全地重复调用：
    /// <see cref="NativeLibrary.SetDllImportResolver"/> 对同一程序集只允许注册一次
    /// （再次注册会抛 <see cref="InvalidOperationException"/>），因此这里只注册一次，
    /// 由解析器读取最新路径。传入新路径会让下次解析重新加载。
    /// </summary>
    public static void SetIl2CppLibraryPath(string path)
    {
        lock (_resolverLock)
        {
            if (!string.Equals(_libraryPath, path, StringComparison.Ordinal))
            {
                _libraryPath = path;
                _libraryHandle = IntPtr.Zero;
            }

            if (_resolverRegistered) return;
            NativeLibrary.SetDllImportResolver(typeof(Il2CppFunctions).Assembly, ResolveLibrary);
            _resolverRegistered = true;
        }
    }

    private static IntPtr ResolveLibrary(string libraryName, System.Reflection.Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        // 其他库名交还默认解析流程，别影响同程序集里的其它 P/Invoke
        if (libraryName != LibraryPlaceholder) return IntPtr.Zero;

        lock (_resolverLock)
        {
            if (_libraryHandle != IntPtr.Zero) return _libraryHandle;
            if (string.IsNullOrEmpty(_libraryPath)) return IntPtr.Zero;

            // 用 TryLoad：从解析器里抛出的异常会直接冒到调用点，盖住真正的原因。
            // 返回 0 让运行时报标准的 DllNotFoundException（消息里带着占位符名，便于定位）。
            if (!NativeLibrary.TryLoad(_libraryPath, out _libraryHandle))
                _libraryHandle = IntPtr.Zero;

            return _libraryHandle;
        }
    }
}