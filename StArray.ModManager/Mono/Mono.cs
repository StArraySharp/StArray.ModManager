using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StArray.ModManager.Mono
{
    public enum MonoAotMode
    {
        MONO_AOT_MODE_NONE,
        MONO_AOT_MODE_NORMAL,
        MONO_AOT_MODE_HYBRID,
        MONO_AOT_MODE_FULL,
        MONO_AOT_MODE_LLVMONLY,
        MONO_AOT_MODE_INTERP,
        MONO_AOT_MODE_INTERP_LLVMONLY,
        MONO_AOT_MODE_LLVMONLY_INTERP,
        MONO_AOT_MODE_LAST = 1000,
    }

    public enum MonoBreakPolicy
    {
        MONO_BREAK_POLICY_ALWAYS,
        MONO_BREAK_POLICY_NEVER,
        MONO_BREAK_POLICY_ON_DBG,
    }

    public partial struct _MonoAppDomain
    {
    }

    public unsafe partial struct MonoBundledAssembly
    {
        [NativeTypeName("const char *")]
        public sbyte* name;

        [NativeTypeName("const unsigned char *")]
        public byte* data;

        [NativeTypeName("unsigned int")]
        public uint size;
    }

    public enum MonoTypeEnum
    {
        MONO_TYPE_END = 0x00,
        MONO_TYPE_VOID = 0x01,
        MONO_TYPE_BOOLEAN = 0x02,
        MONO_TYPE_CHAR = 0x03,
        MONO_TYPE_I1 = 0x04,
        MONO_TYPE_U1 = 0x05,
        MONO_TYPE_I2 = 0x06,
        MONO_TYPE_U2 = 0x07,
        MONO_TYPE_I4 = 0x08,
        MONO_TYPE_U4 = 0x09,
        MONO_TYPE_I8 = 0x0a,
        MONO_TYPE_U8 = 0x0b,
        MONO_TYPE_R4 = 0x0c,
        MONO_TYPE_R8 = 0x0d,
        MONO_TYPE_STRING = 0x0e,
        MONO_TYPE_PTR = 0x0f,
        MONO_TYPE_BYREF = 0x10,
        MONO_TYPE_VALUETYPE = 0x11,
        MONO_TYPE_CLASS = 0x12,
        MONO_TYPE_VAR = 0x13,
        MONO_TYPE_ARRAY = 0x14,
        MONO_TYPE_GENERICINST = 0x15,
        MONO_TYPE_TYPEDBYREF = 0x16,
        MONO_TYPE_I = 0x18,
        MONO_TYPE_U = 0x19,
        MONO_TYPE_FNPTR = 0x1b,
        MONO_TYPE_OBJECT = 0x1c,
        MONO_TYPE_SZARRAY = 0x1d,
        MONO_TYPE_MVAR = 0x1e,
        MONO_TYPE_CMOD_REQD = 0x1f,
        MONO_TYPE_CMOD_OPT = 0x20,
        MONO_TYPE_INTERNAL = 0x21,
        MONO_TYPE_MODIFIER = 0x40,
        MONO_TYPE_SENTINEL = 0x41,
        MONO_TYPE_PINNED = 0x45,
        MONO_TYPE_ENUM = 0x55,
    }

    public enum MonoMetaTableEnum
    {
        MONO_TABLE_MODULE,
        MONO_TABLE_TYPEREF,
        MONO_TABLE_TYPEDEF,
        MONO_TABLE_FIELD_POINTER,
        MONO_TABLE_FIELD,
        MONO_TABLE_METHOD_POINTER,
        MONO_TABLE_METHOD,
        MONO_TABLE_PARAM_POINTER,
        MONO_TABLE_PARAM,
        MONO_TABLE_INTERFACEIMPL,
        MONO_TABLE_MEMBERREF,
        MONO_TABLE_CONSTANT,
        MONO_TABLE_CUSTOMATTRIBUTE,
        MONO_TABLE_FIELDMARSHAL,
        MONO_TABLE_DECLSECURITY,
        MONO_TABLE_CLASSLAYOUT,
        MONO_TABLE_FIELDLAYOUT,
        MONO_TABLE_STANDALONESIG,
        MONO_TABLE_EVENTMAP,
        MONO_TABLE_EVENT_POINTER,
        MONO_TABLE_EVENT,
        MONO_TABLE_PROPERTYMAP,
        MONO_TABLE_PROPERTY_POINTER,
        MONO_TABLE_PROPERTY,
        MONO_TABLE_METHODSEMANTICS,
        MONO_TABLE_METHODIMPL,
        MONO_TABLE_MODULEREF,
        MONO_TABLE_TYPESPEC,
        MONO_TABLE_IMPLMAP,
        MONO_TABLE_FIELDRVA,
        MONO_TABLE_UNUSED6,
        MONO_TABLE_UNUSED7,
        MONO_TABLE_ASSEMBLY,
        MONO_TABLE_ASSEMBLYPROCESSOR,
        MONO_TABLE_ASSEMBLYOS,
        MONO_TABLE_ASSEMBLYREF,
        MONO_TABLE_ASSEMBLYREFPROCESSOR,
        MONO_TABLE_ASSEMBLYREFOS,
        MONO_TABLE_FILE,
        MONO_TABLE_EXPORTEDTYPE,
        MONO_TABLE_MANIFESTRESOURCE,
        MONO_TABLE_NESTEDCLASS,
        MONO_TABLE_GENERICPARAM,
        MONO_TABLE_METHODSPEC,
        MONO_TABLE_GENERICPARAMCONSTRAINT,
        MONO_TABLE_UNUSED8,
        MONO_TABLE_UNUSED9,
        MONO_TABLE_UNUSED10,
        MONO_TABLE_DOCUMENT,
        MONO_TABLE_METHODBODY,
        MONO_TABLE_LOCALSCOPE,
        MONO_TABLE_LOCALVARIABLE,
        MONO_TABLE_LOCALCONSTANT,
        MONO_TABLE_IMPORTSCOPE,
        MONO_TABLE_STATEMACHINEMETHOD,
        MONO_TABLE_CUSTOMDEBUGINFORMATION,
    }

    public partial struct MonoVTable
    {
    }

    public partial struct _MonoClassField
    {
    }

    public partial struct _MonoProperty
    {
    }

    public partial struct _MonoEvent
    {
    }

    public enum MonoTypeNameFormat
    {
        MONO_TYPE_NAME_FORMAT_IL,
        MONO_TYPE_NAME_FORMAT_REFLECTION,
        MONO_TYPE_NAME_FORMAT_FULL_NAME,
        MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED,
    }

    public unsafe partial struct MonoDisHelper
    {
        [NativeTypeName("const char *")]
        public sbyte* newline;

        [NativeTypeName("const char *")]
        public sbyte* label_format;

        [NativeTypeName("const char *")]
        public sbyte* label_target;

        [NativeTypeName("MonoDisIndenter")]
        public delegate* unmanaged[Cdecl]<MonoDisHelper*, _MonoMethod*, uint, sbyte*> indenter;

        [NativeTypeName("MonoDisTokener")]
        public delegate* unmanaged[Cdecl]<MonoDisHelper*, _MonoMethod*, uint, sbyte*> tokener;

        public void* user_data;
    }

    public partial struct MonoMethodDesc
    {
    }

    public partial struct MonoSymbolFileLineNumberEntry
    {
    }

    public partial struct MonoSymbolFileDynamicTable
    {
    }

    public partial struct MonoSymbolFileOffsetTable
    {
        [NativeTypeName("uint32_t")]
        public uint _total_file_size;

        [NativeTypeName("uint32_t")]
        public uint _data_section_offset;

        [NativeTypeName("uint32_t")]
        public uint _data_section_size;

        [NativeTypeName("uint32_t")]
        public uint _compile_unit_count;

        [NativeTypeName("uint32_t")]
        public uint _compile_unit_table_offset;

        [NativeTypeName("uint32_t")]
        public uint _compile_unit_table_size;

        [NativeTypeName("uint32_t")]
        public uint _source_count;

        [NativeTypeName("uint32_t")]
        public uint _source_table_offset;

        [NativeTypeName("uint32_t")]
        public uint _source_table_size;

        [NativeTypeName("uint32_t")]
        public uint _method_count;

        [NativeTypeName("uint32_t")]
        public uint _method_table_offset;

        [NativeTypeName("uint32_t")]
        public uint _method_table_size;

        [NativeTypeName("uint32_t")]
        public uint _type_count;

        [NativeTypeName("uint32_t")]
        public uint _anonymous_scope_count;

        [NativeTypeName("uint32_t")]
        public uint _anonymous_scope_table_offset;

        [NativeTypeName("uint32_t")]
        public uint _anonymous_scope_table_size;

        [NativeTypeName("uint32_t")]
        public uint _line_number_table_line_base;

        [NativeTypeName("uint32_t")]
        public uint _line_number_table_line_range;

        [NativeTypeName("uint32_t")]
        public uint _line_number_table_opcode_base;

        [NativeTypeName("uint32_t")]
        public uint _is_aspx_source;
    }

    public partial struct MonoSymbolFileSourceEntry
    {
        [NativeTypeName("uint32_t")]
        public uint _index;

        [NativeTypeName("uint32_t")]
        public uint _data_offset;
    }

    public partial struct MonoSymbolFileMethodEntry
    {
        [NativeTypeName("uint32_t")]
        public uint _token;

        [NativeTypeName("uint32_t")]
        public uint _data_offset;

        [NativeTypeName("uint32_t")]
        public uint _line_number_table;
    }

    public unsafe partial struct MonoSymbolFileMethodAddress
    {
        [NativeTypeName("uint32_t")]
        public uint size;

        [NativeTypeName("const uint8_t *")]
        public byte* start_address;

        [NativeTypeName("const uint8_t *")]
        public byte* end_address;

        [NativeTypeName("const uint8_t *")]
        public byte* method_start_address;

        [NativeTypeName("const uint8_t *")]
        public byte* method_end_address;

        [NativeTypeName("const uint8_t *")]
        public byte* wrapper_address;

        [NativeTypeName("uint32_t")]
        public uint has_this;

        [NativeTypeName("uint32_t")]
        public uint num_params;

        [NativeTypeName("uint32_t")]
        public uint variable_table_offset;

        [NativeTypeName("uint32_t")]
        public uint type_table_offset;

        [NativeTypeName("uint32_t")]
        public uint num_line_numbers;

        [NativeTypeName("uint32_t")]
        public uint line_number_offset;

        [NativeTypeName("uint8_t[0]")]
        public _data_e__FixedBuffer data;

        public partial struct _data_e__FixedBuffer
        {
            public byte e0;

            [UnscopedRef]
            public ref byte this[int index]
            {
                get
                {
                    return ref Unsafe.Add(ref e0, index);
                }
            }

            [UnscopedRef]
            public Span<byte> AsSpan(int length) => MemoryMarshal.CreateSpan(ref e0, length);
        }
    }

    public partial struct _MonoAssembly
    {
    }

    public partial struct _MonoAssemblyName
    {
    }

    public partial struct _MonoTableInfo
    {
    }

    public enum MonoImageOpenStatus
    {
        MONO_IMAGE_OK,
        MONO_IMAGE_ERROR_ERRNO,
        MONO_IMAGE_MISSING_ASSEMBLYREF,
        MONO_IMAGE_IMAGE_INVALID,
    }

    public enum MonoExceptionEnum
    {
        MONO_EXCEPTION_CLAUSE_NONE,
        MONO_EXCEPTION_CLAUSE_FILTER,
        MONO_EXCEPTION_CLAUSE_FINALLY,
        MONO_EXCEPTION_CLAUSE_FAULT = 4,
    }

    public enum MonoCallConvention
    {
        MONO_CALL_DEFAULT,
        MONO_CALL_C,
        MONO_CALL_STDCALL,
        MONO_CALL_THISCALL,
        MONO_CALL_FASTCALL,
        MONO_CALL_VARARG,
    }

    public enum MonoMarshalNative
    {
        MONO_NATIVE_BOOLEAN = 0x02,
        MONO_NATIVE_I1 = 0x03,
        MONO_NATIVE_U1 = 0x04,
        MONO_NATIVE_I2 = 0x05,
        MONO_NATIVE_U2 = 0x06,
        MONO_NATIVE_I4 = 0x07,
        MONO_NATIVE_U4 = 0x08,
        MONO_NATIVE_I8 = 0x09,
        MONO_NATIVE_U8 = 0x0a,
        MONO_NATIVE_R4 = 0x0b,
        MONO_NATIVE_R8 = 0x0c,
        MONO_NATIVE_CURRENCY = 0x0f,
        MONO_NATIVE_BSTR = 0x13,
        MONO_NATIVE_LPSTR = 0x14,
        MONO_NATIVE_LPWSTR = 0x15,
        MONO_NATIVE_LPTSTR = 0x16,
        MONO_NATIVE_BYVALTSTR = 0x17,
        MONO_NATIVE_IUNKNOWN = 0x19,
        MONO_NATIVE_IDISPATCH = 0x1a,
        MONO_NATIVE_STRUCT = 0x1b,
        MONO_NATIVE_INTERFACE = 0x1c,
        MONO_NATIVE_SAFEARRAY = 0x1d,
        MONO_NATIVE_BYVALARRAY = 0x1e,
        MONO_NATIVE_INT = 0x1f,
        MONO_NATIVE_UINT = 0x20,
        MONO_NATIVE_VBBYREFSTR = 0x22,
        MONO_NATIVE_ANSIBSTR = 0x23,
        MONO_NATIVE_TBSTR = 0x24,
        MONO_NATIVE_VARIANTBOOL = 0x25,
        MONO_NATIVE_FUNC = 0x26,
        MONO_NATIVE_ASANY = 0x28,
        MONO_NATIVE_LPARRAY = 0x2a,
        MONO_NATIVE_LPSTRUCT = 0x2b,
        MONO_NATIVE_CUSTOM = 0x2c,
        MONO_NATIVE_ERROR = 0x2d,
        MONO_NATIVE_UTF8STR = 0x30,
        MONO_NATIVE_MAX = 0x50,
    }

    public enum MonoMarshalVariant
    {
        MONO_VARIANT_EMPTY = 0x00,
        MONO_VARIANT_NULL = 0x01,
        MONO_VARIANT_I2 = 0x02,
        MONO_VARIANT_I4 = 0x03,
        MONO_VARIANT_R4 = 0x04,
        MONO_VARIANT_R8 = 0x05,
        MONO_VARIANT_CY = 0x06,
        MONO_VARIANT_DATE = 0x07,
        MONO_VARIANT_BSTR = 0x08,
        MONO_VARIANT_DISPATCH = 0x09,
        MONO_VARIANT_ERROR = 0x0a,
        MONO_VARIANT_BOOL = 0x0b,
        MONO_VARIANT_VARIANT = 0x0c,
        MONO_VARIANT_UNKNOWN = 0x0d,
        MONO_VARIANT_DECIMAL = 0x0e,
        MONO_VARIANT_I1 = 0x10,
        MONO_VARIANT_UI1 = 0x11,
        MONO_VARIANT_UI2 = 0x12,
        MONO_VARIANT_UI4 = 0x13,
        MONO_VARIANT_I8 = 0x14,
        MONO_VARIANT_UI8 = 0x15,
        MONO_VARIANT_INT = 0x16,
        MONO_VARIANT_UINT = 0x17,
        MONO_VARIANT_VOID = 0x18,
        MONO_VARIANT_HRESULT = 0x19,
        MONO_VARIANT_PTR = 0x1a,
        MONO_VARIANT_SAFEARRAY = 0x1b,
        MONO_VARIANT_CARRAY = 0x1c,
        MONO_VARIANT_USERDEFINED = 0x1d,
        MONO_VARIANT_LPSTR = 0x1e,
        MONO_VARIANT_LPWSTR = 0x1f,
        MONO_VARIANT_RECORD = 0x24,
        MONO_VARIANT_FILETIME = 0x40,
        MONO_VARIANT_BLOB = 0x41,
        MONO_VARIANT_STREAM = 0x42,
        MONO_VARIANT_STORAGE = 0x43,
        MONO_VARIANT_STREAMED_OBJECT = 0x44,
        MONO_VARIANT_STORED_OBJECT = 0x45,
        MONO_VARIANT_BLOB_OBJECT = 0x46,
        MONO_VARIANT_CF = 0x47,
        MONO_VARIANT_CLSID = 0x48,
        MONO_VARIANT_VECTOR = 0x1000,
        MONO_VARIANT_ARRAY = 0x2000,
        MONO_VARIANT_BYREF = 0x4000,
    }

    public enum MonoMarshalConv
    {
        MONO_MARSHAL_CONV_NONE,
        MONO_MARSHAL_CONV_BOOL_VARIANTBOOL,
        MONO_MARSHAL_CONV_BOOL_I4,
        MONO_MARSHAL_CONV_STR_BSTR,
        MONO_MARSHAL_CONV_STR_LPSTR,
        MONO_MARSHAL_CONV_LPSTR_STR,
        MONO_MARSHAL_CONV_LPTSTR_STR,
        MONO_MARSHAL_CONV_STR_LPWSTR,
        MONO_MARSHAL_CONV_LPWSTR_STR,
        MONO_MARSHAL_CONV_STR_LPTSTR,
        MONO_MARSHAL_CONV_STR_ANSIBSTR,
        MONO_MARSHAL_CONV_STR_TBSTR,
        MONO_MARSHAL_CONV_STR_BYVALSTR,
        MONO_MARSHAL_CONV_STR_BYVALWSTR,
        MONO_MARSHAL_CONV_SB_LPSTR,
        MONO_MARSHAL_CONV_SB_LPTSTR,
        MONO_MARSHAL_CONV_SB_LPWSTR,
        MONO_MARSHAL_CONV_LPSTR_SB,
        MONO_MARSHAL_CONV_LPTSTR_SB,
        MONO_MARSHAL_CONV_LPWSTR_SB,
        MONO_MARSHAL_CONV_ARRAY_BYVALARRAY,
        MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY,
        MONO_MARSHAL_CONV_ARRAY_SAVEARRAY,
        MONO_MARSHAL_CONV_ARRAY_LPARRAY,
        MONO_MARSHAL_FREE_LPARRAY,
        MONO_MARSHAL_CONV_OBJECT_INTERFACE,
        MONO_MARSHAL_CONV_OBJECT_IDISPATCH,
        MONO_MARSHAL_CONV_OBJECT_IUNKNOWN,
        MONO_MARSHAL_CONV_OBJECT_STRUCT,
        MONO_MARSHAL_CONV_DEL_FTN,
        MONO_MARSHAL_CONV_FTN_DEL,
        MONO_MARSHAL_FREE_ARRAY,
        MONO_MARSHAL_CONV_BSTR_STR,
        MONO_MARSHAL_CONV_SAFEHANDLE,
        MONO_MARSHAL_CONV_HANDLEREF,
        MONO_MARSHAL_CONV_STR_UTF8STR,
        MONO_MARSHAL_CONV_SB_UTF8STR,
        MONO_MARSHAL_CONV_UTF8STR_STR,
        MONO_MARSHAL_CONV_UTF8STR_SB,
        MONO_MARSHAL_CONV_FIXED_BUFFER,
    }

    public partial struct MonoMarshalSpec
    {
        public MonoMarshalNative native;

        [NativeTypeName("__AnonymousRecord_metadata_L181_C2")]
        public _data_e__Union data;

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _data_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_metadata_L182_C3")]
            public _array_data_e__Struct array_data;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_metadata_L188_C3")]
            public _custom_data_e__Struct custom_data;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_metadata_L193_C3")]
            public _safearray_data_e__Struct safearray_data;

            public partial struct _array_data_e__Struct
            {
                public MonoMarshalNative elem_type;

                [NativeTypeName("int32_t")]
                public int num_elem;

                [NativeTypeName("int16_t")]
                public short param_num;

                [NativeTypeName("int16_t")]
                public short elem_mult;
            }

            public unsafe partial struct _custom_data_e__Struct
            {
                [NativeTypeName("char *")]
                public sbyte* custom_name;

                [NativeTypeName("char *")]
                public sbyte* cookie;

                [NativeTypeName("MonoImage *")]
                public _MonoImage* image;
            }

            public partial struct _safearray_data_e__Struct
            {
                public MonoMarshalVariant elem_type;

                [NativeTypeName("int32_t")]
                public int num_elem;
            }
        }
    }

    public partial struct MonoExceptionClause
    {
        [NativeTypeName("uint32_t")]
        public uint flags;

        [NativeTypeName("uint32_t")]
        public uint try_offset;

        [NativeTypeName("uint32_t")]
        public uint try_len;

        [NativeTypeName("uint32_t")]
        public uint handler_offset;

        [NativeTypeName("uint32_t")]
        public uint handler_len;

        [NativeTypeName("__AnonymousRecord_metadata_L288_C2")]
        public _data_e__Union data;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct _data_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint32_t")]
            public uint filter_offset;

            [FieldOffset(0)]
            [NativeTypeName("MonoClass *")]
            public _MonoClass* catch_class;
        }
    }

    public partial struct _MonoType
    {
    }

    public partial struct _MonoGenericInst
    {
    }

    public partial struct _MonoGenericClass
    {
    }

    public partial struct _MonoGenericContext
    {
    }

    public partial struct _MonoGenericContainer
    {
    }

    public partial struct _MonoGenericParam
    {
    }

    public partial struct _MonoMethodSignature
    {
    }

    public partial struct invalid_name
    {
    }

    public partial struct MonoCustomMod
    {
        public uint _bitfield;

        [NativeTypeName("unsigned int : 1")]
        public uint required
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("unsigned int : 31")]
        public uint token
        {
            readonly get
            {
                return (_bitfield >> 1) & 0x7FFFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x7FFFFFFFu << 1)) | ((value & 0x7FFFFFFFu) << 1);
            }
        }
    }

    public unsafe partial struct _MonoCustomModContainer
    {
        [NativeTypeName("uint8_t")]
        public byte count;

        [NativeTypeName("MonoImage *")]
        public _MonoImage* image;

        [NativeTypeName("MonoCustomMod[1]")]
        public _modifiers_e__FixedBuffer modifiers;

        public partial struct _modifiers_e__FixedBuffer
        {
            public MonoCustomMod e0;

            [UnscopedRef]
            public ref MonoCustomMod this[int index]
            {
                get
                {
                    return ref Unsafe.Add(ref e0, index);
                }
            }

            [UnscopedRef]
            public Span<MonoCustomMod> AsSpan(int length) => MemoryMarshal.CreateSpan(ref e0, length);
        }
    }

    public unsafe partial struct _MonoArrayType
    {
        [NativeTypeName("MonoClass *")]
        public _MonoClass* eklass;

        [NativeTypeName("uint8_t")]
        public byte rank;

        [NativeTypeName("uint8_t")]
        public byte numsizes;

        [NativeTypeName("uint8_t")]
        public byte numlobounds;

        public int* sizes;

        public int* lobounds;
    }

    public partial struct _MonoMethodHeader
    {
    }

    public enum MonoParseTypeMode
    {
        MONO_PARSE_TYPE,
        MONO_PARSE_MOD_TYPE,
        MONO_PARSE_LOCAL,
        MONO_PARSE_PARAM,
        MONO_PARSE_RET,
        MONO_PARSE_FIELD,
    }

    public partial struct _MonoDebugDataTable
    {
    }

    public partial struct _MonoSymbolFile
    {
    }

    public partial struct _MonoPPDBFile
    {
    }

    public partial struct _MonoDebugLineNumberEntry
    {
    }

    public partial struct _MonoDebugMethodAddress
    {
    }

    public partial struct _MonoDebugClassEntry
    {
    }

    public partial struct _MonoDebugMethodInfo
    {
    }

    public partial struct _MonoDebugLocalsInfo
    {
    }

    public partial struct _MonoDebugMethodAsyncInfo
    {
    }

    public enum MonoDebugFormat
    {
        MONO_DEBUG_FORMAT_NONE,
        MONO_DEBUG_FORMAT_MONO,
        MONO_DEBUG_FORMAT_DEBUGGER,
    }

    public unsafe partial struct _MonoDebugList
    {
        [NativeTypeName("MonoDebugList *")]
        public _MonoDebugList* next;

        [NativeTypeName("const void *")]
        public void* data;
    }

    public unsafe partial struct _MonoSymbolTable
    {
        [NativeTypeName("uint64_t")]
        public ulong magic;

        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("uint32_t")]
        public uint total_size;

        [NativeTypeName("MonoDebugHandle *")]
        public _MonoDebugHandle* corlib;

        [NativeTypeName("MonoDebugDataTable *")]
        public _MonoDebugDataTable* global_data_table;

        [NativeTypeName("MonoDebugList *")]
        public _MonoDebugList* data_tables;

        [NativeTypeName("MonoDebugList *")]
        public _MonoDebugList* symbol_files;
    }

    public unsafe partial struct _MonoDebugHandle
    {
        [NativeTypeName("uint32_t")]
        public uint index;

        [NativeTypeName("char *")]
        public sbyte* image_file;

        [NativeTypeName("MonoImage *")]
        public _MonoImage* image;

        [NativeTypeName("MonoDebugDataTable *")]
        public _MonoDebugDataTable* type_table;

        [NativeTypeName("MonoSymbolFile *")]
        public _MonoSymbolFile* symfile;

        [NativeTypeName("MonoPPDBFile *")]
        public _MonoPPDBFile* ppdb;
    }

    public unsafe partial struct _MonoDebugMethodJitInfo
    {
        [NativeTypeName("const mono_byte *")]
        public byte* code_start;

        [NativeTypeName("uint32_t")]
        public uint code_size;

        [NativeTypeName("uint32_t")]
        public uint prologue_end;

        [NativeTypeName("uint32_t")]
        public uint epilogue_begin;

        [NativeTypeName("const mono_byte *")]
        public byte* wrapper_addr;

        [NativeTypeName("uint32_t")]
        public uint num_line_numbers;

        [NativeTypeName("MonoDebugLineNumberEntry *")]
        public _MonoDebugLineNumberEntry* line_numbers;

        [NativeTypeName("uint32_t")]
        public uint has_var_info;

        [NativeTypeName("uint32_t")]
        public uint num_params;

        [NativeTypeName("MonoDebugVarInfo *")]
        public _MonoDebugVarInfo* this_var;

        [NativeTypeName("MonoDebugVarInfo *")]
        public _MonoDebugVarInfo* @params;

        [NativeTypeName("uint32_t")]
        public uint num_locals;

        [NativeTypeName("MonoDebugVarInfo *")]
        public _MonoDebugVarInfo* locals;

        [NativeTypeName("MonoDebugVarInfo *")]
        public _MonoDebugVarInfo* gsharedvt_info_var;

        [NativeTypeName("MonoDebugVarInfo *")]
        public _MonoDebugVarInfo* gsharedvt_locals_var;
    }

    public partial struct _MonoDebugMethodAddressList
    {
        [NativeTypeName("uint32_t")]
        public uint size;

        [NativeTypeName("uint32_t")]
        public uint count;

        [NativeTypeName("mono_byte[0]")]
        public _data_e__FixedBuffer data;

        public partial struct _data_e__FixedBuffer
        {
            public byte e0;

            [UnscopedRef]
            public ref byte this[int index]
            {
                get
                {
                    return ref Unsafe.Add(ref e0, index);
                }
            }

            [UnscopedRef]
            public Span<byte> AsSpan(int length) => MemoryMarshal.CreateSpan(ref e0, length);
        }
    }

    public unsafe partial struct _MonoDebugSourceLocation
    {
        [NativeTypeName("char *")]
        public sbyte* source_file;

        [NativeTypeName("uint32_t")]
        public uint row;

        [NativeTypeName("uint32_t")]
        public uint column;

        [NativeTypeName("uint32_t")]
        public uint il_offset;
    }

    public unsafe partial struct _MonoDebugVarInfo
    {
        [NativeTypeName("uint32_t")]
        public uint index;

        [NativeTypeName("uint32_t")]
        public uint offset;

        [NativeTypeName("uint32_t")]
        public uint size;

        [NativeTypeName("uint32_t")]
        public uint begin_scope;

        [NativeTypeName("uint32_t")]
        public uint end_scope;

        [NativeTypeName("MonoType *")]
        public _MonoType* type;
    }

    public enum MonoGCRootSource
    {
        MONO_ROOT_SOURCE_EXTERNAL = 0,
        MONO_ROOT_SOURCE_STACK = 1,
        MONO_ROOT_SOURCE_FINALIZER_QUEUE = 2,
        MONO_ROOT_SOURCE_STATIC = 3,
        MONO_ROOT_SOURCE_THREAD_STATIC = 4,
        MONO_ROOT_SOURCE_CONTEXT_STATIC = 5,
        MONO_ROOT_SOURCE_GC_HANDLE = 6,
        MONO_ROOT_SOURCE_JIT = 7,
        MONO_ROOT_SOURCE_THREADING = 8,
        MONO_ROOT_SOURCE_DOMAIN = 9,
        MONO_ROOT_SOURCE_REFLECTION = 10,
        MONO_ROOT_SOURCE_MARSHAL = 11,
        MONO_ROOT_SOURCE_THREAD_POOL = 12,
        MONO_ROOT_SOURCE_DEBUGGER = 13,
        MONO_ROOT_SOURCE_HANDLE = 14,
        MONO_ROOT_SOURCE_EPHEMERON = 15,
        MONO_ROOT_SOURCE_TOGGLEREF = 16,
    }

    public enum MonoGCHandleType
    {
        MONO_GC_HANDLE_TYPE_MIN = 0,
        MONO_GC_HANDLE_WEAK = MONO_GC_HANDLE_TYPE_MIN,
        MONO_GC_HANDLE_WEAK_TRACK_RESURRECTION,
        MONO_GC_HANDLE_NORMAL,
        MONO_GC_HANDLE_PINNED,
        MONO_GC_HANDLE_TYPE_MAX,
    }

    public partial struct _MonoClass
    {
    }

    public partial struct _MonoImage
    {
    }

    public partial struct _MonoMethod
    {
    }

    public partial struct _MonoObject
    {
    }

    public unsafe partial struct _MonoObject
    {
        public MonoVTable* vtable;

        [NativeTypeName("MonoThreadsSync *")]
        public _MonoThreadsSync* synchronisation;
    }

    public partial struct _MonoException
    {
    }

    public partial struct _MonoReflectionAssembly
    {
    }

    public partial struct _MonoReflectionTypeBuilder
    {
    }

    public partial struct _MonoString
    {
    }

    public partial struct _MonoArray
    {
    }

    public partial struct _MonoReflectionMethod
    {
    }

    public partial struct _MonoReflectionModule
    {
    }

    public partial struct _MonoReflectionField
    {
    }

    public partial struct _MonoReflectionProperty
    {
    }

    public partial struct _MonoReflectionEvent
    {
    }

    public partial struct _MonoReflectionType
    {
    }

    public partial struct _MonoDelegate
    {
    }

    public partial struct _MonoThreadsSync
    {
    }

    public partial struct _MonoThread
    {
    }

    public partial struct _MonoDynamicAssembly
    {
    }

    public partial struct _MonoDynamicImage
    {
    }

    public partial struct _MonoReflectionMethodBody
    {
    }

    public partial struct _MonoAppContext
    {
    }

    public partial struct _MonoReferenceQueue
    {
    }

    public enum MonoOpcodeEnum
    {
        MonoOpcodeEnum_Invalid = -1,
        MONO_CEE_NOP,
        MONO_CEE_BREAK,
        MONO_CEE_LDARG_0,
        MONO_CEE_LDARG_1,
        MONO_CEE_LDARG_2,
        MONO_CEE_LDARG_3,
        MONO_CEE_LDLOC_0,
        MONO_CEE_LDLOC_1,
        MONO_CEE_LDLOC_2,
        MONO_CEE_LDLOC_3,
        MONO_CEE_STLOC_0,
        MONO_CEE_STLOC_1,
        MONO_CEE_STLOC_2,
        MONO_CEE_STLOC_3,
        MONO_CEE_LDARG_S,
        MONO_CEE_LDARGA_S,
        MONO_CEE_STARG_S,
        MONO_CEE_LDLOC_S,
        MONO_CEE_LDLOCA_S,
        MONO_CEE_STLOC_S,
        MONO_CEE_LDNULL,
        MONO_CEE_LDC_I4_M1,
        MONO_CEE_LDC_I4_0,
        MONO_CEE_LDC_I4_1,
        MONO_CEE_LDC_I4_2,
        MONO_CEE_LDC_I4_3,
        MONO_CEE_LDC_I4_4,
        MONO_CEE_LDC_I4_5,
        MONO_CEE_LDC_I4_6,
        MONO_CEE_LDC_I4_7,
        MONO_CEE_LDC_I4_8,
        MONO_CEE_LDC_I4_S,
        MONO_CEE_LDC_I4,
        MONO_CEE_LDC_I8,
        MONO_CEE_LDC_R4,
        MONO_CEE_LDC_R8,
        MONO_CEE_UNUSED99,
        MONO_CEE_DUP,
        MONO_CEE_POP,
        MONO_CEE_JMP,
        MONO_CEE_CALL,
        MONO_CEE_CALLI,
        MONO_CEE_RET,
        MONO_CEE_BR_S,
        MONO_CEE_BRFALSE_S,
        MONO_CEE_BRTRUE_S,
        MONO_CEE_BEQ_S,
        MONO_CEE_BGE_S,
        MONO_CEE_BGT_S,
        MONO_CEE_BLE_S,
        MONO_CEE_BLT_S,
        MONO_CEE_BNE_UN_S,
        MONO_CEE_BGE_UN_S,
        MONO_CEE_BGT_UN_S,
        MONO_CEE_BLE_UN_S,
        MONO_CEE_BLT_UN_S,
        MONO_CEE_BR,
        MONO_CEE_BRFALSE,
        MONO_CEE_BRTRUE,
        MONO_CEE_BEQ,
        MONO_CEE_BGE,
        MONO_CEE_BGT,
        MONO_CEE_BLE,
        MONO_CEE_BLT,
        MONO_CEE_BNE_UN,
        MONO_CEE_BGE_UN,
        MONO_CEE_BGT_UN,
        MONO_CEE_BLE_UN,
        MONO_CEE_BLT_UN,
        MONO_CEE_SWITCH,
        MONO_CEE_LDIND_I1,
        MONO_CEE_LDIND_U1,
        MONO_CEE_LDIND_I2,
        MONO_CEE_LDIND_U2,
        MONO_CEE_LDIND_I4,
        MONO_CEE_LDIND_U4,
        MONO_CEE_LDIND_I8,
        MONO_CEE_LDIND_I,
        MONO_CEE_LDIND_R4,
        MONO_CEE_LDIND_R8,
        MONO_CEE_LDIND_REF,
        MONO_CEE_STIND_REF,
        MONO_CEE_STIND_I1,
        MONO_CEE_STIND_I2,
        MONO_CEE_STIND_I4,
        MONO_CEE_STIND_I8,
        MONO_CEE_STIND_R4,
        MONO_CEE_STIND_R8,
        MONO_CEE_ADD,
        MONO_CEE_SUB,
        MONO_CEE_MUL,
        MONO_CEE_DIV,
        MONO_CEE_DIV_UN,
        MONO_CEE_REM,
        MONO_CEE_REM_UN,
        MONO_CEE_AND,
        MONO_CEE_OR,
        MONO_CEE_XOR,
        MONO_CEE_SHL,
        MONO_CEE_SHR,
        MONO_CEE_SHR_UN,
        MONO_CEE_NEG,
        MONO_CEE_NOT,
        MONO_CEE_CONV_I1,
        MONO_CEE_CONV_I2,
        MONO_CEE_CONV_I4,
        MONO_CEE_CONV_I8,
        MONO_CEE_CONV_R4,
        MONO_CEE_CONV_R8,
        MONO_CEE_CONV_U4,
        MONO_CEE_CONV_U8,
        MONO_CEE_CALLVIRT,
        MONO_CEE_CPOBJ,
        MONO_CEE_LDOBJ,
        MONO_CEE_LDSTR,
        MONO_CEE_NEWOBJ,
        MONO_CEE_CASTCLASS,
        MONO_CEE_ISINST,
        MONO_CEE_CONV_R_UN,
        MONO_CEE_UNUSED58,
        MONO_CEE_UNUSED1,
        MONO_CEE_UNBOX,
        MONO_CEE_THROW,
        MONO_CEE_LDFLD,
        MONO_CEE_LDFLDA,
        MONO_CEE_STFLD,
        MONO_CEE_LDSFLD,
        MONO_CEE_LDSFLDA,
        MONO_CEE_STSFLD,
        MONO_CEE_STOBJ,
        MONO_CEE_CONV_OVF_I1_UN,
        MONO_CEE_CONV_OVF_I2_UN,
        MONO_CEE_CONV_OVF_I4_UN,
        MONO_CEE_CONV_OVF_I8_UN,
        MONO_CEE_CONV_OVF_U1_UN,
        MONO_CEE_CONV_OVF_U2_UN,
        MONO_CEE_CONV_OVF_U4_UN,
        MONO_CEE_CONV_OVF_U8_UN,
        MONO_CEE_CONV_OVF_I_UN,
        MONO_CEE_CONV_OVF_U_UN,
        MONO_CEE_BOX,
        MONO_CEE_NEWARR,
        MONO_CEE_LDLEN,
        MONO_CEE_LDELEMA,
        MONO_CEE_LDELEM_I1,
        MONO_CEE_LDELEM_U1,
        MONO_CEE_LDELEM_I2,
        MONO_CEE_LDELEM_U2,
        MONO_CEE_LDELEM_I4,
        MONO_CEE_LDELEM_U4,
        MONO_CEE_LDELEM_I8,
        MONO_CEE_LDELEM_I,
        MONO_CEE_LDELEM_R4,
        MONO_CEE_LDELEM_R8,
        MONO_CEE_LDELEM_REF,
        MONO_CEE_STELEM_I,
        MONO_CEE_STELEM_I1,
        MONO_CEE_STELEM_I2,
        MONO_CEE_STELEM_I4,
        MONO_CEE_STELEM_I8,
        MONO_CEE_STELEM_R4,
        MONO_CEE_STELEM_R8,
        MONO_CEE_STELEM_REF,
        MONO_CEE_LDELEM,
        MONO_CEE_STELEM,
        MONO_CEE_UNBOX_ANY,
        MONO_CEE_UNUSED5,
        MONO_CEE_UNUSED6,
        MONO_CEE_UNUSED7,
        MONO_CEE_UNUSED8,
        MONO_CEE_UNUSED9,
        MONO_CEE_UNUSED10,
        MONO_CEE_UNUSED11,
        MONO_CEE_UNUSED12,
        MONO_CEE_UNUSED13,
        MONO_CEE_UNUSED14,
        MONO_CEE_UNUSED15,
        MONO_CEE_UNUSED16,
        MONO_CEE_UNUSED17,
        MONO_CEE_CONV_OVF_I1,
        MONO_CEE_CONV_OVF_U1,
        MONO_CEE_CONV_OVF_I2,
        MONO_CEE_CONV_OVF_U2,
        MONO_CEE_CONV_OVF_I4,
        MONO_CEE_CONV_OVF_U4,
        MONO_CEE_CONV_OVF_I8,
        MONO_CEE_CONV_OVF_U8,
        MONO_CEE_UNUSED50,
        MONO_CEE_UNUSED18,
        MONO_CEE_UNUSED19,
        MONO_CEE_UNUSED20,
        MONO_CEE_UNUSED21,
        MONO_CEE_UNUSED22,
        MONO_CEE_UNUSED23,
        MONO_CEE_REFANYVAL,
        MONO_CEE_CKFINITE,
        MONO_CEE_UNUSED24,
        MONO_CEE_UNUSED25,
        MONO_CEE_MKREFANY,
        MONO_CEE_UNUSED59,
        MONO_CEE_UNUSED60,
        MONO_CEE_UNUSED61,
        MONO_CEE_UNUSED62,
        MONO_CEE_UNUSED63,
        MONO_CEE_UNUSED64,
        MONO_CEE_UNUSED65,
        MONO_CEE_UNUSED66,
        MONO_CEE_UNUSED67,
        MONO_CEE_LDTOKEN,
        MONO_CEE_CONV_U2,
        MONO_CEE_CONV_U1,
        MONO_CEE_CONV_I,
        MONO_CEE_CONV_OVF_I,
        MONO_CEE_CONV_OVF_U,
        MONO_CEE_ADD_OVF,
        MONO_CEE_ADD_OVF_UN,
        MONO_CEE_MUL_OVF,
        MONO_CEE_MUL_OVF_UN,
        MONO_CEE_SUB_OVF,
        MONO_CEE_SUB_OVF_UN,
        MONO_CEE_ENDFINALLY,
        MONO_CEE_LEAVE,
        MONO_CEE_LEAVE_S,
        MONO_CEE_STIND_I,
        MONO_CEE_CONV_U,
        MONO_CEE_UNUSED26,
        MONO_CEE_UNUSED27,
        MONO_CEE_UNUSED28,
        MONO_CEE_UNUSED29,
        MONO_CEE_UNUSED30,
        MONO_CEE_UNUSED31,
        MONO_CEE_UNUSED32,
        MONO_CEE_UNUSED33,
        MONO_CEE_UNUSED34,
        MONO_CEE_UNUSED35,
        MONO_CEE_UNUSED36,
        MONO_CEE_UNUSED37,
        MONO_CEE_UNUSED38,
        MONO_CEE_UNUSED39,
        MONO_CEE_UNUSED40,
        MONO_CEE_UNUSED41,
        MONO_CEE_UNUSED42,
        MONO_CEE_UNUSED43,
        MONO_CEE_UNUSED44,
        MONO_CEE_UNUSED45,
        MONO_CEE_UNUSED46,
        MONO_CEE_UNUSED47,
        MONO_CEE_UNUSED48,
        MONO_CEE_PREFIX7,
        MONO_CEE_PREFIX6,
        MONO_CEE_PREFIX5,
        MONO_CEE_PREFIX4,
        MONO_CEE_PREFIX3,
        MONO_CEE_PREFIX2,
        MONO_CEE_PREFIX1,
        MONO_CEE_PREFIXREF,
        MONO_CEE_ARGLIST,
        MONO_CEE_CEQ,
        MONO_CEE_CGT,
        MONO_CEE_CGT_UN,
        MONO_CEE_CLT,
        MONO_CEE_CLT_UN,
        MONO_CEE_LDFTN,
        MONO_CEE_LDVIRTFTN,
        MONO_CEE_UNUSED56,
        MONO_CEE_LDARG,
        MONO_CEE_LDARGA,
        MONO_CEE_STARG,
        MONO_CEE_LDLOC,
        MONO_CEE_LDLOCA,
        MONO_CEE_STLOC,
        MONO_CEE_LOCALLOC,
        MONO_CEE_UNUSED57,
        MONO_CEE_ENDFILTER,
        MONO_CEE_UNALIGNED_,
        MONO_CEE_VOLATILE_,
        MONO_CEE_TAIL_,
        MONO_CEE_INITOBJ,
        MONO_CEE_CONSTRAINED_,
        MONO_CEE_CPBLK,
        MONO_CEE_INITBLK,
        MONO_CEE_NO_,
        MONO_CEE_RETHROW,
        MONO_CEE_UNUSED,
        MONO_CEE_SIZEOF,
        MONO_CEE_REFANYTYPE,
        MONO_CEE_READONLY_,
        MONO_CEE_UNUSED53,
        MONO_CEE_UNUSED54,
        MONO_CEE_UNUSED55,
        MONO_CEE_UNUSED70,
        MONO_CEE_ILLEGAL,
        MONO_CEE_ENDMAC,
        MONO_CEE_MONO_ICALL,
        MONO_CEE_MONO_OBJADDR,
        MONO_CEE_MONO_LDPTR,
        MONO_CEE_MONO_VTADDR,
        MONO_CEE_MONO_NEWOBJ,
        MONO_CEE_MONO_RETOBJ,
        MONO_CEE_MONO_LDNATIVEOBJ,
        MONO_CEE_MONO_CISINST,
        MONO_CEE_MONO_CCASTCLASS,
        MONO_CEE_MONO_SAVE_LMF,
        MONO_CEE_MONO_RESTORE_LMF,
        MONO_CEE_MONO_CLASSCONST,
        MONO_CEE_MONO_NOT_TAKEN,
        MONO_CEE_MONO_TLS,
        MONO_CEE_MONO_ICALL_ADDR,
        MONO_CEE_MONO_DYN_CALL,
        MONO_CEE_MONO_MEMORY_BARRIER,
        MONO_CEE_UNUSED71,
        MONO_CEE_UNUSED72,
        MONO_CEE_MONO_JIT_ICALL_ADDR,
        MONO_CEE_MONO_LDPTR_INT_REQ_FLAG,
        MONO_CEE_MONO_LDPTR_CARD_TABLE,
        MONO_CEE_MONO_LDPTR_NURSERY_START,
        MONO_CEE_MONO_LDPTR_NURSERY_BITS,
        MONO_CEE_MONO_CALLI_EXTRA_ARG,
        MONO_CEE_MONO_LDDOMAIN,
        MONO_CEE_MONO_ATOMIC_STORE_I4,
        MONO_CEE_MONO_SAVE_LAST_ERROR,
        MONO_CEE_MONO_GET_RGCTX_ARG,
        MONO_CEE_MONO_LDPTR_PROFILER_ALLOCATION_COUNT,
        MONO_CEE_MONO_LD_DELEGATE_METHOD_PTR,
        MONO_CEE_MONO_RETHROW,
        MONO_CEE_MONO_GET_SP,
        MONO_CEE_LAST,
    }

    public partial struct MonoOpcode
    {
        [NativeTypeName("unsigned char")]
        public byte argument;

        [NativeTypeName("unsigned char")]
        public byte flow_type;

        [NativeTypeName("unsigned short")]
        public ushort opval;
    }

    public partial struct _MonoProfiler
    {
    }

    public partial struct _MonoProfilerDesc
    {
    }

    public unsafe partial struct MonoProfilerCoverageData
    {
        [NativeTypeName("MonoMethod *")]
        public _MonoMethod* method;

        [NativeTypeName("uint32_t")]
        public uint il_offset;

        [NativeTypeName("uint32_t")]
        public uint counter;

        [NativeTypeName("const char *")]
        public sbyte* file_name;

        [NativeTypeName("uint32_t")]
        public uint line;

        [NativeTypeName("uint32_t")]
        public uint column;
    }

    public enum MonoProfilerSampleMode
    {
        MONO_PROFILER_SAMPLE_MODE_NONE = 0,
        MONO_PROFILER_SAMPLE_MODE_PROCESS = 1,
        MONO_PROFILER_SAMPLE_MODE_REAL = 2,
    }

    public partial struct _MonoProfilerCallContext
    {
    }

    public enum MonoProfilerCallInstrumentationFlags
    {
        MONO_PROFILER_CALL_INSTRUMENTATION_NONE = 0,
        MONO_PROFILER_CALL_INSTRUMENTATION_ENTER = 1 << 1,
        MONO_PROFILER_CALL_INSTRUMENTATION_ENTER_CONTEXT = 1 << 2,
        MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE = 1 << 3,
        MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE_CONTEXT = 1 << 4,
        MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL = 1 << 5,
        MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE = 1 << 6,
    }

    public enum MonoProfilerCodeBufferType
    {
        MONO_PROFILER_CODE_BUFFER_METHOD = 0,
        MONO_PROFILER_CODE_BUFFER_METHOD_TRAMPOLINE = 1,
        MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE = 2,
        MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE = 3,
        MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE = 4,
        MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE = 5,
        MONO_PROFILER_CODE_BUFFER_HELPER = 6,
        MONO_PROFILER_CODE_BUFFER_MONITOR = 7,
        MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE = 8,
        MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING = 9,
    }

    public enum MonoProfilerGCEvent
    {
        MONO_GC_EVENT_PRE_STOP_WORLD = 6,
        MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED = 10,
        MONO_GC_EVENT_POST_STOP_WORLD = 7,
        MONO_GC_EVENT_START = 0,
        MONO_GC_EVENT_END = 5,
        MONO_GC_EVENT_PRE_START_WORLD = 8,
        MONO_GC_EVENT_POST_START_WORLD_UNLOCKED = 11,
        MONO_GC_EVENT_POST_START_WORLD = 9,
    }

    public partial struct MonoTypeNameParse
    {
    }

    public unsafe partial struct MonoCustomAttrEntry
    {
        [NativeTypeName("MonoMethod *")]
        public _MonoMethod* ctor;

        [NativeTypeName("uint32_t")]
        public uint data_size;

        [NativeTypeName("const mono_byte *")]
        public byte* data;
    }

    public unsafe partial struct MonoCustomAttrInfo
    {
        public int num_attrs;

        public int cached;

        [NativeTypeName("MonoImage *")]
        public _MonoImage* image;

        [NativeTypeName("MonoCustomAttrEntry[0]")]
        public _attrs_e__FixedBuffer attrs;

        public partial struct _attrs_e__FixedBuffer
        {
            public MonoCustomAttrEntry e0;

            [UnscopedRef]
            public ref MonoCustomAttrEntry this[int index]
            {
                get
                {
                    return ref Unsafe.Add(ref e0, index);
                }
            }

            [UnscopedRef]
            public Span<MonoCustomAttrEntry> AsSpan(int length) => MemoryMarshal.CreateSpan(ref e0, length);
        }
    }

    public unsafe partial struct MonoReflectionMethodAux
    {
        [NativeTypeName("char **")]
        public sbyte** param_names;

        public MonoMarshalSpec** param_marshall;

        public MonoCustomAttrInfo** param_cattr;

        [NativeTypeName("uint8_t **")]
        public byte** param_defaults;

        [NativeTypeName("uint32_t *")]
        public uint* param_default_types;

        [NativeTypeName("char *")]
        public sbyte* dllentry;

        [NativeTypeName("char *")]
        public sbyte* dll;
    }

    public enum MonoResolveTokenError
    {
        ResolveTokenError_OutOfRange,
        ResolveTokenError_BadTable,
        ResolveTokenError_Other,
    }

    public unsafe partial struct MonoDeclSecurityEntry
    {
        [NativeTypeName("char *")]
        public sbyte* blob;

        [NativeTypeName("uint32_t")]
        public uint size;

        [NativeTypeName("uint32_t")]
        public uint index;
    }

    public partial struct MonoDeclSecurityActions
    {
        public MonoDeclSecurityEntry demand;

        public MonoDeclSecurityEntry noncasdemand;

        public MonoDeclSecurityEntry demandchoice;
    }

    public enum MonoGCBridgeObjectKind
    {
        GC_BRIDGE_TRANSPARENT_CLASS,
        GC_BRIDGE_OPAQUE_CLASS,
        GC_BRIDGE_TRANSPARENT_BRIDGE_CLASS,
        GC_BRIDGE_OPAQUE_BRIDGE_CLASS,
    }

    public partial struct MonoGCBridgeSCC
    {
        [NativeTypeName("mono_bool")]
        public int is_alive;

        public int num_objs;

        [NativeTypeName("MonoObject *[0]")]
        public _objs_e__FixedBuffer objs;

        public unsafe partial struct _objs_e__FixedBuffer
        {
            public _MonoObject* e0;

            public ref _MonoObject* this[int index]
            {
                get
                {
                    fixed (_MonoObject** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }

    public partial struct MonoGCBridgeXRef
    {
        public int src_scc_index;

        public int dst_scc_index;
    }

    public unsafe partial struct MonoGCBridgeCallbacks
    {
        public int bridge_version;

        [NativeTypeName("MonoGCBridgeObjectKind (*)(MonoClass *)")]
        public delegate* unmanaged[Cdecl]<_MonoClass*, MonoGCBridgeObjectKind> bridge_class_kind;

        [NativeTypeName("mono_bool (*)(MonoObject *)")]
        public delegate* unmanaged[Cdecl]<_MonoObject*, int> is_bridge_object;

        [NativeTypeName("void (*)(int, MonoGCBridgeSCC **, int, MonoGCBridgeXRef *)")]
        public delegate* unmanaged[Cdecl]<int, MonoGCBridgeSCC**, int, MonoGCBridgeXRef*, void> cross_references;
    }

    public enum MonoTokenType
    {
        MONO_TOKEN_MODULE = 0x00000000,
        MONO_TOKEN_TYPE_REF = 0x01000000,
        MONO_TOKEN_TYPE_DEF = 0x02000000,
        MONO_TOKEN_FIELD_DEF = 0x04000000,
        MONO_TOKEN_METHOD_DEF = 0x06000000,
        MONO_TOKEN_PARAM_DEF = 0x08000000,
        MONO_TOKEN_INTERFACE_IMPL = 0x09000000,
        MONO_TOKEN_MEMBER_REF = 0x0a000000,
        MONO_TOKEN_CUSTOM_ATTRIBUTE = 0x0c000000,
        MONO_TOKEN_PERMISSION = 0x0e000000,
        MONO_TOKEN_SIGNATURE = 0x11000000,
        MONO_TOKEN_EVENT = 0x14000000,
        MONO_TOKEN_PROPERTY = 0x17000000,
        MONO_TOKEN_MODULE_REF = 0x1a000000,
        MONO_TOKEN_TYPE_SPEC = 0x1b000000,
        MONO_TOKEN_ASSEMBLY = 0x20000000,
        MONO_TOKEN_ASSEMBLY_REF = 0x23000000,
        MONO_TOKEN_FILE = 0x26000000,
        MONO_TOKEN_EXPORTED_TYPE = 0x27000000,
        MONO_TOKEN_MANIFEST_RESOURCE = 0x28000000,
        MONO_TOKEN_GENERIC_PARAM = 0x2a000000,
        MONO_TOKEN_METHOD_SPEC = 0x2b000000,
        MONO_TOKEN_STRING = 0x70000000,
        MONO_TOKEN_NAME = 0x71000000,
        MONO_TOKEN_BASE_TYPE = 0x72000000,
    }

    public enum MonoVerifyStatus
    {
        MONO_VERIFY_OK,
        MONO_VERIFY_ERROR,
        MONO_VERIFY_WARNING,
        MONO_VERIFY_CLS = 4,
        MONO_VERIFY_ALL = 7,
        MONO_VERIFY_NOT_VERIFIABLE = 8,
        MONO_VERIFY_FAIL_FAST = 16,
        MONO_VERIFY_NON_STRICT = 32,
        MONO_VERIFY_SKIP_VISIBILITY = 64,
        MONO_VERIFY_REPORT_ALL_ERRORS = 128,
    }

    public unsafe partial struct MonoVerifyInfo
    {
        [NativeTypeName("char *")]
        public sbyte* message;

        public MonoVerifyStatus status;
    }

    public partial struct MonoVerifyInfoExtended
    {
        public MonoVerifyInfo info;

        [NativeTypeName("int8_t")]
        public sbyte exception_type;
    }

    public partial struct _MonoCounter
    {
    }

    public enum MonoResourceType
    {
        MONO_RESOURCE_JIT_CODE,
        MONO_RESOURCE_METADATA,
        MONO_RESOURCE_GC_HEAP,
        MONO_RESOURCE_COUNT,
    }

    public partial struct MonoDlFallbackHandler
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _MonoError
    {
        [FieldOffset(0)]
        [NativeTypeName("uint32_t")]
        public uint init;

        [FieldOffset(0)]
        [NativeTypeName("__AnonymousRecord_mono-error_L63_C2")]
        public _Anonymous_e__Struct Anonymous;

        [UnscopedRef]
        public ref ushort error_code
        {
            get
            {
                return ref Anonymous.error_code;
            }
        }

        [UnscopedRef]
        public ref ushort private_flags
        {
            get
            {
                return ref Anonymous.private_flags;
            }
        }

        [UnscopedRef]
        public _Anonymous_e__Struct._hidden_1_e__FixedBuffer hidden_1
        {
            get
            {
                return Anonymous.hidden_1;
            }
        }

        public partial struct _Anonymous_e__Struct
        {
            [NativeTypeName("uint16_t")]
            public ushort error_code;

            [NativeTypeName("uint16_t")]
            public ushort private_flags;

            [NativeTypeName("void *[12]")]
            public _hidden_1_e__FixedBuffer hidden_1;

            public unsafe partial struct _hidden_1_e__FixedBuffer
            {
                public void* e0;
                public void* e1;
                public void* e2;
                public void* e3;
                public void* e4;
                public void* e5;
                public void* e6;
                public void* e7;
                public void* e8;
                public void* e9;
                public void* e10;
                public void* e11;

                public ref void* this[int index]
                {
                    get
                    {
                        fixed (void** pThis = &e0)
                        {
                            return ref pThis[index];
                        }
                    }
                }
            }
        }
    }

    public partial struct _MonoErrorBoxed
    {
    }

    public partial struct _MonoDomain
    {
    }

    public partial struct _MonoJitInfo
    {
    }

    public unsafe partial struct MonoAllocatorVTable
    {
        public int version;

        [NativeTypeName("void *(*)(size_t)")]
        public delegate* unmanaged[Cdecl]<nuint, void*> malloc;

        [NativeTypeName("void *(*)(void *, size_t)")]
        public delegate* unmanaged[Cdecl]<void*, nuint, void*> realloc;

        [NativeTypeName("void (*)(void *)")]
        public delegate* unmanaged[Cdecl]<void*, void> free;

        [NativeTypeName("void *(*)(size_t, size_t)")]
        public delegate* unmanaged[Cdecl]<nuint, nuint, void*> calloc;
    }

    public static unsafe partial class Methods
    {
        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_jit_init([NativeTypeName("const char *")] sbyte* file);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_jit_init_version([NativeTypeName("const char *")] sbyte* root_domain_name, [NativeTypeName("const char *")] sbyte* runtime_version);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_jit_init_version_for_test_only([NativeTypeName("const char *")] sbyte* root_domain_name, [NativeTypeName("const char *")] sbyte* runtime_version);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_jit_exec([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly, int argc, [NativeTypeName("char *[]")] sbyte** argv);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_jit_cleanup([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_jit_set_trace_options([NativeTypeName("const char *")] sbyte* options);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_signal_chaining([NativeTypeName("mono_bool")] int chain_signals);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_crash_chaining([NativeTypeName("mono_bool")] int chain_signals);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_jit_set_aot_only([NativeTypeName("mono_bool")] int aot_only);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_jit_set_aot_mode(MonoAotMode mode);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_jit_aot_compiling();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_break_policy([NativeTypeName("MonoBreakPolicyFunc")] delegate* unmanaged[Cdecl]<_MonoMethod*, MonoBreakPolicy> policy_callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_jit_parse_options(int argc, [NativeTypeName("char *[]")] sbyte** argv);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_get_runtime_build_info();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_use_llvm([NativeTypeName("mono_bool")] int use_llvm);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_aot_register_module(void** aot_info);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_jit_thread_attach([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_init([NativeTypeName("const char *")] sbyte* filename);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_init_from_assembly([NativeTypeName("const char *")] sbyte* domain_name, [NativeTypeName("const char *")] sbyte* filename);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_init_version([NativeTypeName("const char *")] sbyte* domain_name, [NativeTypeName("const char *")] sbyte* version);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_get_root_domain();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_init([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoThreadStartCB")] delegate* unmanaged[Cdecl]<nint, void*, void*, void> start_cb, [NativeTypeName("MonoThreadAttachCB")] delegate* unmanaged[Cdecl]<nint, void*, void> attach_cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_cleanup([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_runtime_cleanup([NativeTypeName("MonoDomainFunc")] delegate* unmanaged[Cdecl]<_MonoDomain*, void*, void> func);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_quit();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_set_shutting_down();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_runtime_is_shutting_down();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_check_corlib_version();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_domain_create();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_domain_create_appdomain([NativeTypeName("char *")] sbyte* friendly_name, [NativeTypeName("char *")] sbyte* configuration_file);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_set_config([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const char *")] sbyte* base_dir, [NativeTypeName("const char *")] sbyte* config_file_name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_domain_get();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_domain_get_by_id([NativeTypeName("int32_t")] int domainid);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_domain_get_id([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_domain_get_friendly_name([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_domain_set([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("mono_bool")] int force);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_set_internal([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_unload([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_try_unload([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_domain_is_unloading([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_domain_from_appdomain([NativeTypeName("MonoAppDomain *")] _MonoAppDomain* appdomain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_foreach([NativeTypeName("MonoDomainFunc")] delegate* unmanaged[Cdecl]<_MonoDomain*, void*, void> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_domain_assembly_open([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_domain_finalize([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("uint32_t")] uint timeout);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_domain_free([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("mono_bool")] int force);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_domain_has_type_resolve([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionAssembly *")]
        public static extern _MonoReflectionAssembly* mono_domain_try_type_resolve([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("char *")] sbyte* name, [NativeTypeName("MonoObject *")] _MonoObject* tb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_domain_owns_vtable_slot([NativeTypeName("MonoDomain *")] _MonoDomain* domain, void* vtable_slot);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_context_init([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_context_set([NativeTypeName("MonoAppContext *")] _MonoAppContext* new_context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAppContext *")]
        public static extern _MonoAppContext* mono_context_get();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_context_get_id([NativeTypeName("MonoAppContext *")] _MonoAppContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_context_get_domain_id([NativeTypeName("MonoAppContext *")] _MonoAppContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoJitInfo *")]
        public static extern _MonoJitInfo* mono_jit_info_table_find([NativeTypeName("MonoDomain *")] _MonoDomain* domain, void* addr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_jit_info_get_code_start([NativeTypeName("MonoJitInfo *")] _MonoJitInfo* ji);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_jit_info_get_code_size([NativeTypeName("MonoJitInfo *")] _MonoJitInfo* ji);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_jit_info_get_method([NativeTypeName("MonoJitInfo *")] _MonoJitInfo* ji);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_get_corlib();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_object_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_byte_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_void_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_boolean_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_sbyte_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_int16_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_uint16_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_int32_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_uint32_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_intptr_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_uintptr_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_int64_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_uint64_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_single_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_double_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_char_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_string_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_enum_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_array_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_thread_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_get_exception_class();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_security_enable_core_clr();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_security_set_core_clr_platform_callback([NativeTypeName("MonoCoreClrPlatformCB")] delegate* unmanaged[Cdecl]<sbyte*, int> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assemblies_init();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assemblies_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_open([NativeTypeName("const char *")] sbyte* filename, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_open_full([NativeTypeName("const char *")] sbyte* filename, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_load([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname, [NativeTypeName("const char *")] sbyte* basedir, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_load_full([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname, [NativeTypeName("const char *")] sbyte* basedir, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_load_from([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* fname, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_load_from_full([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* fname, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_load_with_partial_name([NativeTypeName("const char *")] sbyte* name, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_loaded([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_loaded_full([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_get_assemblyref([NativeTypeName("MonoImage *")] _MonoImage* image, int index, [NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_load_reference([NativeTypeName("MonoImage *")] _MonoImage* image, int index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_load_references([NativeTypeName("MonoImage *")] _MonoImage* image, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_assembly_load_module([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly, [NativeTypeName("uint32_t")] uint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_close([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_setrootdir([NativeTypeName("const char *")] sbyte* root_dir);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_assembly_getrootdir();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_native_getrootdir();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_foreach([NativeTypeName("MonoFunc")] delegate* unmanaged[Cdecl]<void*, void*, void> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_set_main([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_get_main();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_assembly_get_image([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssemblyName *")]
        public static extern _MonoAssemblyName* mono_assembly_get_name([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_assembly_fill_assembly_name([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_assembly_names_equal([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* l, [NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* r);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_stringify_assembly_name([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_load_hook([NativeTypeName("MonoAssemblyLoadFunc")] delegate* unmanaged[Cdecl]<_MonoAssembly*, void*, void> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_search_hook([NativeTypeName("MonoAssemblySearchFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_refonly_search_hook([NativeTypeName("MonoAssemblySearchFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_assembly_invoke_search_hook([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_postload_search_hook([NativeTypeName("MonoAssemblySearchFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_postload_refonly_search_hook([NativeTypeName("MonoAssemblySearchFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_preload_hook([NativeTypeName("MonoAssemblyPreLoadFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, sbyte**, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_assembly_refonly_preload_hook([NativeTypeName("MonoAssemblyPreLoadFunc")] delegate* unmanaged[Cdecl]<_MonoAssemblyName*, sbyte**, void*, _MonoAssembly*> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_invoke_load_hook([NativeTypeName("MonoAssembly *")] _MonoAssembly* ass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssemblyName *")]
        public static extern _MonoAssemblyName* mono_assembly_name_new([NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_assembly_name_get_name([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_assembly_name_get_culture([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint16_t")]
        public static extern ushort mono_assembly_name_get_version([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname, [NativeTypeName("uint16_t *")] ushort* minor, [NativeTypeName("uint16_t *")] ushort* build, [NativeTypeName("uint16_t *")] ushort* revision);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_byte *")]
        public static extern byte* mono_assembly_name_get_pubkeytoken([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_assembly_name_free([NativeTypeName("MonoAssemblyName *")] _MonoAssemblyName* aname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_register_bundled_assemblies([NativeTypeName("const MonoBundledAssembly **")] MonoBundledAssembly** assemblies);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_register_config_for_assembly([NativeTypeName("const char *")] sbyte* assembly_name, [NativeTypeName("const char *")] sbyte* config_xml);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_register_symfile_for_assembly([NativeTypeName("const char *")] sbyte* assembly_name, [NativeTypeName("const mono_byte *")] byte* raw_contents, int size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_register_machine_config([NativeTypeName("const char *")] sbyte* config_xml);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_rootdir();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_dirs([NativeTypeName("const char *")] sbyte* assembly_dir, [NativeTypeName("const char *")] sbyte* config_dir);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_assemblies_path([NativeTypeName("const char *")] sbyte* path);

        public const int MONO_ASSEMBLY_HASH_NONE = 0;
        public const int MONO_ASSEMBLY_HASH_MD5 = 0x8003;
        public const int MONO_ASSEMBLY_HASH_SHA1 = 0x8004;

        public const int MONO_ASSEMBLYREF_FULL_PUBLIC_KEY = 0x0001;
        public const int MONO_ASSEMBLYREF_RETARGETABLE = 0x0100;
        public const int MONO_ASSEMBLYREF_JIT_TRACKING = 0x8000;
        public const int MONO_ASSEMBLYREF_NO_JIT_OPT = 0x4000;

        public const int MONO_EVENT_SPECIALNAME = 0x0200;
        public const int MONO_EVENT_RTSPECIALNAME = 0x0400;

        public const int MONO_FIELD_ATTR_FIELD_ACCESS_MASK = 0x0007;
        public const int MONO_FIELD_ATTR_COMPILER_CONTROLLED = 0x0000;
        public const int MONO_FIELD_ATTR_PRIVATE = 0x0001;
        public const int MONO_FIELD_ATTR_FAM_AND_ASSEM = 0x0002;
        public const int MONO_FIELD_ATTR_ASSEMBLY = 0x0003;
        public const int MONO_FIELD_ATTR_FAMILY = 0x0004;
        public const int MONO_FIELD_ATTR_FAM_OR_ASSEM = 0x0005;
        public const int MONO_FIELD_ATTR_PUBLIC = 0x0006;
        public const int MONO_FIELD_ATTR_STATIC = 0x0010;
        public const int MONO_FIELD_ATTR_INIT_ONLY = 0x0020;
        public const int MONO_FIELD_ATTR_LITERAL = 0x0040;
        public const int MONO_FIELD_ATTR_NOT_SERIALIZED = 0x0080;
        public const int MONO_FIELD_ATTR_SPECIAL_NAME = 0x0200;
        public const int MONO_FIELD_ATTR_PINVOKE_IMPL = 0x2000;
        public const int MONO_FIELD_ATTR_RESERVED_MASK = 0x9500;
        public const int MONO_FIELD_ATTR_RT_SPECIAL_NAME = 0x0400;
        public const int MONO_FIELD_ATTR_HAS_MARSHAL = 0x1000;
        public const int MONO_FIELD_ATTR_HAS_DEFAULT = 0x8000;
        public const int MONO_FIELD_ATTR_HAS_RVA = 0x0100;

        public const int MONO_FILE_HAS_METADATA = 0;
        public const int MONO_FILE_HAS_NO_METADATA = 1;

        public const int MONO_GEN_PARAM_VARIANCE_MASK = 0x0003;
        public const int MONO_GEN_PARAM_NON_VARIANT = 0x0000;
        public const int MONO_GEN_PARAM_VARIANT = 0x0001;
        public const int MONO_GEN_PARAM_COVARIANT = 0x0002;
        public const int MONO_GEN_PARAM_CONSTRAINT_MASK = 0x001c;
        public const int MONO_GEN_PARAM_CONSTRAINT_CLASS = 0x0004;
        public const int MONO_GEN_PARAM_CONSTRAINT_VTYPE = 0x0008;
        public const int MONO_GEN_PARAM_CONSTRAINT_DCTOR = 0x0010;

        public const int MONO_PINVOKE_NO_MANGLE = 0x0001;
        public const int MONO_PINVOKE_CHAR_SET_MASK = 0x0006;
        public const int MONO_PINVOKE_CHAR_SET_NOT_SPEC = 0x0000;
        public const int MONO_PINVOKE_CHAR_SET_ANSI = 0x0002;
        public const int MONO_PINVOKE_CHAR_SET_UNICODE = 0x0004;
        public const int MONO_PINVOKE_CHAR_SET_AUTO = 0x0006;
        public const int MONO_PINVOKE_BEST_FIT_ENABLED = 0x0010;
        public const int MONO_PINVOKE_BEST_FIT_DISABLED = 0x0020;
        public const int MONO_PINVOKE_BEST_FIT_MASK = 0x0030;
        public const int MONO_PINVOKE_SUPPORTS_LAST_ERROR = 0x0040;
        public const int MONO_PINVOKE_CALL_CONV_MASK = 0x0700;
        public const int MONO_PINVOKE_CALL_CONV_WINAPI = 0x0100;
        public const int MONO_PINVOKE_CALL_CONV_CDECL = 0x0200;
        public const int MONO_PINVOKE_CALL_CONV_STDCALL = 0x0300;
        public const int MONO_PINVOKE_CALL_CONV_THISCALL = 0x0400;
        public const int MONO_PINVOKE_CALL_CONV_FASTCALL = 0x0500;
        public const int MONO_PINVOKE_THROW_ON_UNMAPPABLE_ENABLED = 0x1000;
        public const int MONO_PINVOKE_THROW_ON_UNMAPPABLE_DISABLED = 0x2000;
        public const int MONO_PINVOKE_THROW_ON_UNMAPPABLE_MASK = 0x3000;
        public const int MONO_PINVOKE_CALL_CONV_GENERIC = 0x0010;
        public const int MONO_PINVOKE_CALL_CONV_GENERICINST = 0x000a;

        public const int MONO_MANIFEST_RESOURCE_VISIBILITY_MASK = 0x00000007;
        public const int MONO_MANIFEST_RESOURCE_PUBLIC = 0x00000001;
        public const int MONO_MANIFEST_RESOURCE_PRIVATE = 0x00000002;

        public const int MONO_METHOD_ATTR_ACCESS_MASK = 0x0007;
        public const int MONO_METHOD_ATTR_COMPILER_CONTROLLED = 0x0000;
        public const int MONO_METHOD_ATTR_PRIVATE = 0x0001;
        public const int MONO_METHOD_ATTR_FAM_AND_ASSEM = 0x0002;
        public const int MONO_METHOD_ATTR_ASSEM = 0x0003;
        public const int MONO_METHOD_ATTR_FAMILY = 0x0004;
        public const int MONO_METHOD_ATTR_FAM_OR_ASSEM = 0x0005;
        public const int MONO_METHOD_ATTR_PUBLIC = 0x0006;
        public const int MONO_METHOD_ATTR_STATIC = 0x0010;
        public const int MONO_METHOD_ATTR_FINAL = 0x0020;
        public const int MONO_METHOD_ATTR_VIRTUAL = 0x0040;
        public const int MONO_METHOD_ATTR_HIDE_BY_SIG = 0x0080;
        public const int MONO_METHOD_ATTR_VTABLE_LAYOUT_MASK = 0x0100;
        public const int MONO_METHOD_ATTR_REUSE_SLOT = 0x0000;
        public const int MONO_METHOD_ATTR_NEW_SLOT = 0x0100;
        public const int MONO_METHOD_ATTR_STRICT = 0x0200;
        public const int MONO_METHOD_ATTR_ABSTRACT = 0x0400;
        public const int MONO_METHOD_ATTR_SPECIAL_NAME = 0x0800;
        public const int MONO_METHOD_ATTR_PINVOKE_IMPL = 0x2000;
        public const int MONO_METHOD_ATTR_UNMANAGED_EXPORT = 0x0008;
        public const int MONO_METHOD_ATTR_RESERVED_MASK = 0xd000;
        public const int MONO_METHOD_ATTR_RT_SPECIAL_NAME = 0x1000;
        public const int MONO_METHOD_ATTR_HAS_SECURITY = 0x4000;
        public const int MONO_METHOD_ATTR_REQUIRE_SEC_OBJECT = 0x8000;

        public const int MONO_METHOD_IMPL_ATTR_CODE_TYPE_MASK = 0x0003;
        public const int MONO_METHOD_IMPL_ATTR_IL = 0x0000;
        public const int MONO_METHOD_IMPL_ATTR_NATIVE = 0x0001;
        public const int MONO_METHOD_IMPL_ATTR_OPTIL = 0x0002;
        public const int MONO_METHOD_IMPL_ATTR_RUNTIME = 0x0003;
        public const int MONO_METHOD_IMPL_ATTR_MANAGED_MASK = 0x0004;
        public const int MONO_METHOD_IMPL_ATTR_UNMANAGED = 0x0004;
        public const int MONO_METHOD_IMPL_ATTR_MANAGED = 0x0000;
        public const int MONO_METHOD_IMPL_ATTR_FORWARD_REF = 0x0010;
        public const int MONO_METHOD_IMPL_ATTR_PRESERVE_SIG = 0x0080;
        public const int MONO_METHOD_IMPL_ATTR_INTERNAL_CALL = 0x1000;
        public const int MONO_METHOD_IMPL_ATTR_SYNCHRONIZED = 0x0020;
        public const int MONO_METHOD_IMPL_ATTR_NOINLINING = 0x0008;
        public const int MONO_METHOD_IMPL_ATTR_NOOPTIMIZATION = 0x0040;
        public const int MONO_METHOD_IMPL_ATTR_MAX_METHOD_IMPL_VAL = 0xffff;

        public const int MONO_METHOD_SEMANTIC_SETTER = 0x0001;
        public const int MONO_METHOD_SEMANTIC_GETTER = 0x0002;
        public const int MONO_METHOD_SEMANTIC_OTHER = 0x0004;
        public const int MONO_METHOD_SEMANTIC_ADD_ON = 0x0008;
        public const int MONO_METHOD_SEMANTIC_REMOVE_ON = 0x0010;
        public const int MONO_METHOD_SEMANTIC_FIRE = 0x0020;

        public const int MONO_PARAM_ATTR_IN = 0x0001;
        public const int MONO_PARAM_ATTR_OUT = 0x0002;
        public const int MONO_PARAM_ATTR_OPTIONAL = 0x0010;
        public const int MONO_PARAM_ATTR_RESERVED_MASK = 0xf000;
        public const int MONO_PARAM_ATTR_HAS_DEFAULT = 0x1000;
        public const int MONO_PARAM_ATTR_HAS_MARSHAL = 0x2000;
        public const int MONO_PARAM_ATTR_UNUSED = 0xcfe0;

        public const int MONO_PROPERTY_ATTR_SPECIAL_NAME = 0x0200;
        public const int MONO_PROPERTY_ATTR_RESERVED_MASK = 0xf400;
        public const int MONO_PROPERTY_ATTR_RT_SPECIAL_NAME = 0x0400;
        public const int MONO_PROPERTY_ATTR_HAS_DEFAULT = 0x1000;
        public const int MONO_PROPERTY_ATTR_UNUSED = 0xe9ff;

        public const int MONO_TYPE_ATTR_VISIBILITY_MASK = 0x00000007;
        public const int MONO_TYPE_ATTR_NOT_PUBLIC = 0x00000000;
        public const int MONO_TYPE_ATTR_PUBLIC = 0x00000001;
        public const int MONO_TYPE_ATTR_NESTED_PUBLIC = 0x00000002;
        public const int MONO_TYPE_ATTR_NESTED_PRIVATE = 0x00000003;
        public const int MONO_TYPE_ATTR_NESTED_FAMILY = 0x00000004;
        public const int MONO_TYPE_ATTR_NESTED_ASSEMBLY = 0x00000005;
        public const int MONO_TYPE_ATTR_NESTED_FAM_AND_ASSEM = 0x00000006;
        public const int MONO_TYPE_ATTR_NESTED_FAM_OR_ASSEM = 0x00000007;
        public const int MONO_TYPE_ATTR_LAYOUT_MASK = 0x00000018;
        public const int MONO_TYPE_ATTR_AUTO_LAYOUT = 0x00000000;
        public const int MONO_TYPE_ATTR_SEQUENTIAL_LAYOUT = 0x00000008;
        public const int MONO_TYPE_ATTR_EXPLICIT_LAYOUT = 0x00000010;
        public const int MONO_TYPE_ATTR_CLASS_SEMANTIC_MASK = 0x00000020;
        public const int MONO_TYPE_ATTR_CLASS = 0x00000000;
        public const int MONO_TYPE_ATTR_INTERFACE = 0x00000020;
        public const int MONO_TYPE_ATTR_ABSTRACT = 0x00000080;
        public const int MONO_TYPE_ATTR_SEALED = 0x00000100;
        public const int MONO_TYPE_ATTR_SPECIAL_NAME = 0x00000400;
        public const int MONO_TYPE_ATTR_IMPORT = 0x00001000;
        public const int MONO_TYPE_ATTR_SERIALIZABLE = 0x00002000;
        public const int MONO_TYPE_ATTR_STRING_FORMAT_MASK = 0x00030000;
        public const int MONO_TYPE_ATTR_ANSI_CLASS = 0x00000000;
        public const int MONO_TYPE_ATTR_UNICODE_CLASS = 0x00010000;
        public const int MONO_TYPE_ATTR_AUTO_CLASS = 0x00020000;
        public const int MONO_TYPE_ATTR_CUSTOM_CLASS = 0x00030000;
        public const int MONO_TYPE_ATTR_CUSTOM_MASK = 0x00c00000;
        public const int MONO_TYPE_ATTR_BEFORE_FIELD_INIT = 0x00100000;
        public const int MONO_TYPE_ATTR_FORWARDER = 0x00200000;
        public const int MONO_TYPE_ATTR_RESERVED_MASK = 0x00040800;
        public const int MONO_TYPE_ATTR_RT_SPECIAL_NAME = 0x00000800;
        public const int MONO_TYPE_ATTR_HAS_SECURITY = 0x00040000;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_full([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_token, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_init([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoVTable* mono_class_vtable([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_name([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_name_case([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_class_get_method_from_name_flags([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("const char *")] sbyte* name, int param_count, int flags);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_typeref([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_typeref_checked([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_token, [NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_generic_parameter([NativeTypeName("MonoGenericParam *")] _MonoGenericParam* param0, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("mono_bool")] int is_mvar);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_class_inflate_generic_type([NativeTypeName("MonoType *")] _MonoType* type, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_class_inflate_generic_method([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_inflated_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClassField *")]
        public static extern _MonoClassField* mono_field_from_token([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoClass **")] _MonoClass** retklass, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_bounded_array_class_get([NativeTypeName("MonoClass *")] _MonoClass* element_class, [NativeTypeName("uint32_t")] uint rank, [NativeTypeName("mono_bool")] int bounded);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_array_class_get([NativeTypeName("MonoClass *")] _MonoClass* element_class, [NativeTypeName("uint32_t")] uint rank);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_ptr_class_get([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClassField *")]
        public static extern _MonoClassField* mono_class_get_field([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("uint32_t")] uint field_token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClassField *")]
        public static extern _MonoClassField* mono_class_get_field_from_name([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_class_get_field_token([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_class_get_event_token([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoProperty *")]
        public static extern _MonoProperty* mono_class_get_property_from_name([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_class_get_property_token([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_array_element_size([NativeTypeName("MonoClass *")] _MonoClass* ac);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_class_instance_size([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_class_array_element_size([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_class_data_size([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_class_value_size([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("uint32_t *")] uint* align);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_class_min_align([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_from_mono_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_subclass_of([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClass *")] _MonoClass* klassc, [NativeTypeName("mono_bool")] int check_interfaces);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_assignable_from([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClass *")] _MonoClass* oklass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_ldtoken([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoClass **")] _MonoClass** retclass, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_type_get_name_full([NativeTypeName("MonoType *")] _MonoType* type, MonoTypeNameFormat format);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_type_get_name([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_type_get_underlying_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_class_get_image([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_element_class([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_valuetype([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_enum([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_class_enum_basetype([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_parent([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_nesting_type([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_class_get_rank([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_class_get_flags([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_class_get_name([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_class_get_namespace([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_class_get_type([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_class_get_type_token([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_class_get_byref_type([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_class_num_fields([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_class_num_methods([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_class_num_properties([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_class_num_events([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClassField *")]
        public static extern _MonoClassField* mono_class_get_fields([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_class_get_methods([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoProperty *")]
        public static extern _MonoProperty* mono_class_get_properties([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoEvent *")]
        public static extern _MonoEvent* mono_class_get_events([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_interfaces([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_nested_types([NativeTypeName("MonoClass *")] _MonoClass* klass, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_delegate([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_implements_interface([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClass *")] _MonoClass* iface);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_field_get_name([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_field_get_type([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_field_get_parent([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_field_get_flags([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_field_get_offset([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_field_get_data([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_property_get_name([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_property_get_set_method([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_property_get_get_method([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_property_get_parent([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_property_get_flags([NativeTypeName("MonoProperty *")] _MonoProperty* prop);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_event_get_name([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_event_get_add_method([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_event_get_remove_method([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_event_get_raise_method([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_event_get_parent([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_event_get_flags([NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_class_get_method_from_name([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("const char *")] sbyte* name, int param_count);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_class_name_from_token([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_can_access_field([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_can_access_method([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoMethod *")] _MonoMethod* called);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_class_is_nullable([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_class_get_nullable_param([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_disasm_code_one(MonoDisHelper* dh, [NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("const mono_byte *")] byte* ip, [NativeTypeName("const mono_byte **")] byte** endp);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_disasm_code(MonoDisHelper* dh, [NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("const mono_byte *")] byte* ip, [NativeTypeName("const mono_byte *")] byte* end);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_type_full_name([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_signature_get_desc([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig, [NativeTypeName("mono_bool")] int include_namespace);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_context_get_desc([NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoMethodDesc* mono_method_desc_new([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("mono_bool")] int include_namespace);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoMethodDesc* mono_method_desc_from_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_method_desc_free(MonoMethodDesc* desc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_desc_match(MonoMethodDesc* desc, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_desc_is_full(MonoMethodDesc* desc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_desc_full_match(MonoMethodDesc* desc, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_method_desc_search_in_class(MonoMethodDesc* desc, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_method_desc_search_in_image(MonoMethodDesc* desc, [NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_method_full_name([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("mono_bool")] int signature);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_method_get_reflection_name([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_field_full_name([NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoSymbolFile *")]
        public static extern _MonoSymbolFile* mono_debug_open_mono_symbols([NativeTypeName("MonoDebugHandle *")] _MonoDebugHandle* handle, [NativeTypeName("const uint8_t *")] byte* raw_contents, int size, [NativeTypeName("mono_bool")] int in_the_debugger);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_close_mono_symbol_file([NativeTypeName("MonoSymbolFile *")] _MonoSymbolFile* symfile);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_debug_symfile_is_loaded([NativeTypeName("MonoSymbolFile *")] _MonoSymbolFile* symfile);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugSourceLocation *")]
        public static extern _MonoDebugSourceLocation* mono_debug_symfile_lookup_location([NativeTypeName("MonoDebugMethodInfo *")] _MonoDebugMethodInfo* minfo, [NativeTypeName("uint32_t")] uint offset);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_symfile_free_location([NativeTypeName("MonoDebugSourceLocation *")] _MonoDebugSourceLocation* location);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodInfo *")]
        public static extern _MonoDebugMethodInfo* mono_debug_symfile_lookup_method([NativeTypeName("MonoDebugHandle *")] _MonoDebugHandle* handle, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugLocalsInfo *")]
        public static extern _MonoDebugLocalsInfo* mono_debug_symfile_lookup_locals([NativeTypeName("MonoDebugMethodInfo *")] _MonoDebugMethodInfo* minfo);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_symfile_get_seq_points([NativeTypeName("MonoDebugMethodInfo *")] _MonoDebugMethodInfo* minfo, [NativeTypeName("char **")] sbyte** source_file, [NativeTypeName("GPtrArray **")] void** source_file_list, int** source_files, [NativeTypeName("MonoSymSeqPoint **")] void** seq_points, int* n_seq_points);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_environment_exitcode_get();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_environment_exitcode_set([NativeTypeName("int32_t")] int value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_name([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_token([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_name_two_strings([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("MonoString *")] _MonoString* a1, [NativeTypeName("MonoString *")] _MonoString* a2);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_name_msg([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_token_two_strings([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoString *")] _MonoString* a1, [NativeTypeName("MonoString *")] _MonoString* a2);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_exception_from_name_domain([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* name_space, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_divide_by_zero();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_security();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_arithmetic();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_overflow();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_null_reference();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_execution_engine([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_thread_abort();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_thread_state([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_thread_interrupted();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_serialization([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_invalid_cast();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_invalid_operation([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_index_out_of_range();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_array_type_mismatch();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_type_load([NativeTypeName("MonoString *")] _MonoString* class_name, [NativeTypeName("char *")] sbyte* assembly_name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_missing_method([NativeTypeName("const char *")] sbyte* class_name, [NativeTypeName("const char *")] sbyte* member_name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_missing_field([NativeTypeName("const char *")] sbyte* class_name, [NativeTypeName("const char *")] sbyte* member_name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_not_implemented([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_not_supported([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_argument_null([NativeTypeName("const char *")] sbyte* arg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_argument([NativeTypeName("const char *")] sbyte* arg, [NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_argument_out_of_range([NativeTypeName("const char *")] sbyte* arg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_io([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_file_not_found([NativeTypeName("MonoString *")] _MonoString* fname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_file_not_found2([NativeTypeName("const char *")] sbyte* msg, [NativeTypeName("MonoString *")] _MonoString* fname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_type_initialization([NativeTypeName("const char *")] sbyte* type_name, [NativeTypeName("MonoException *")] _MonoException* inner);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_synchronization_lock([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_cannot_unload_appdomain([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_appdomain_unloaded();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_bad_image_format([NativeTypeName("const char *")] sbyte* msg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_bad_image_format2([NativeTypeName("const char *")] sbyte* msg, [NativeTypeName("MonoString *")] _MonoString* fname);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_stack_overflow();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_out_of_memory();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_field_access();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_method_access();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_reflection_type_load([NativeTypeName("MonoArray *")] _MonoArray* types, [NativeTypeName("MonoArray *")] _MonoArray* exceptions);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoException *")]
        public static extern _MonoException* mono_get_exception_runtime_wrapped([NativeTypeName("MonoObject *")] _MonoObject* wrapped_exception);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_install_unhandled_exception_hook([NativeTypeName("MonoUnhandledExceptionFunc")] delegate* unmanaged[Cdecl]<_MonoObject*, void*, void> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_invoke_unhandled_exception_hook([NativeTypeName("MonoObject *")] _MonoObject* exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_images_init();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_images_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_open([NativeTypeName("const char *")] sbyte* fname, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_open_full([NativeTypeName("const char *")] sbyte* fname, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_pe_file_open([NativeTypeName("const char *")] sbyte* fname, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_open_from_data([NativeTypeName("char *")] sbyte* data, [NativeTypeName("uint32_t")] uint data_len, [NativeTypeName("mono_bool")] int need_copy, MonoImageOpenStatus* status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_open_from_data_full([NativeTypeName("char *")] sbyte* data, [NativeTypeName("uint32_t")] uint data_len, [NativeTypeName("mono_bool")] int need_copy, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_open_from_data_with_name([NativeTypeName("char *")] sbyte* data, [NativeTypeName("uint32_t")] uint data_len, [NativeTypeName("mono_bool")] int need_copy, MonoImageOpenStatus* status, [NativeTypeName("mono_bool")] int refonly, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_image_fixup_vtable([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_loaded([NativeTypeName("const char *")] sbyte* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_loaded_full([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_loaded_by_guid([NativeTypeName("const char *")] sbyte* guid);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_loaded_by_guid_full([NativeTypeName("const char *")] sbyte* guid, [NativeTypeName("mono_bool")] int refonly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_image_init([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_image_close([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_image_addref([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_strerror(MonoImageOpenStatus status);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_image_ensure_section([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* section);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_image_ensure_section_idx([NativeTypeName("MonoImage *")] _MonoImage* image, int section);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_image_get_entry_point([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_resource([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint offset, [NativeTypeName("uint32_t *")] uint* size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_load_file_for_image([NativeTypeName("MonoImage *")] _MonoImage* image, int fileidx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoImage *")]
        public static extern _MonoImage* mono_image_load_module([NativeTypeName("MonoImage *")] _MonoImage* image, int idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_name([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_filename([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_guid([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_image_get_assembly([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_image_is_dynamic([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_image_rva_map([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint rva);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const MonoTableInfo *")]
        public static extern _MonoTableInfo* mono_image_get_table_info([NativeTypeName("MonoImage *")] _MonoImage* image, int table_id);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_image_get_table_rows([NativeTypeName("MonoImage *")] _MonoImage* image, int table_id);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_table_info_get_rows([NativeTypeName("const MonoTableInfo *")] _MonoTableInfo* table);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_image_lookup_resource([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint res_id, [NativeTypeName("uint32_t")] uint lang_id, [NativeTypeName("mono_unichar2 *")] ushort* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_public_key([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t *")] uint* size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_image_get_strong_name([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t *")] uint* size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_image_strong_name_position([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t *")] uint* size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_image_add_to_name_cache([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* nspace, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("uint32_t")] uint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_image_has_authenticode_entry([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_method([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_method_full([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_method_constrained([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoClass *")] _MonoClass* constrained_class, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context, [NativeTypeName("MonoMethod **")] _MonoMethod** cil_method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_free_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_method_get_signature_full([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("MonoGenericContext *")] _MonoGenericContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_method_get_signature([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_method_signature([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodHeader *")]
        public static extern _MonoMethodHeader* mono_method_get_header([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_method_get_name([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_method_get_class([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_method_get_token([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_method_get_flags([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("uint32_t *")] uint* iflags);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_method_get_index([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_add_internal_call([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const void *")] void* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_dangerous_add_raw_internal_call([NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const void *")] void* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_lookup_internal_call([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_lookup_icall_symbol([NativeTypeName("MonoMethod *")] _MonoMethod* m);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_dllmap_insert([NativeTypeName("MonoImage *")] _MonoImage* assembly, [NativeTypeName("const char *")] sbyte* dll, [NativeTypeName("const char *")] sbyte* func, [NativeTypeName("const char *")] sbyte* tdll, [NativeTypeName("const char *")] sbyte* tfunc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_lookup_pinvoke_call([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("const char **")] sbyte** exc_class, [NativeTypeName("const char **")] sbyte** exc_arg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_method_get_param_names([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("const char **")] sbyte** names);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_method_get_param_token([NativeTypeName("MonoMethod *")] _MonoMethod* method, int idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_method_get_marshal_info([NativeTypeName("MonoMethod *")] _MonoMethod* method, MonoMarshalSpec** mspecs);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_method_has_marshal_info([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_method_get_last_managed();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_stack_walk([NativeTypeName("MonoStackWalk")] delegate* unmanaged[Cdecl]<_MonoMethod*, int, int, int, void*, int> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_stack_walk_no_il([NativeTypeName("MonoStackWalk")] delegate* unmanaged[Cdecl]<_MonoMethod*, int, int, int, void*, int> func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_stack_walk_async_safe([NativeTypeName("MonoStackWalkAsyncSafe")] delegate* unmanaged[Cdecl]<_MonoMethod*, _MonoDomain*, void*, int, void*, int> func, void* initial_sig_context, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodHeader *")]
        public static extern _MonoMethodHeader* mono_method_get_header_checked([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_init();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_decode_row([NativeTypeName("const MonoTableInfo *")] _MonoTableInfo* t, int idx, [NativeTypeName("uint32_t *")] uint* res, int res_size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_decode_row_col([NativeTypeName("const MonoTableInfo *")] _MonoTableInfo* t, int idx, [NativeTypeName("unsigned int")] uint col);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_metadata_compute_size([NativeTypeName("MonoImage *")] _MonoImage* meta, int tableindex, [NativeTypeName("uint32_t *")] uint* result_bitfield);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_locate([NativeTypeName("MonoImage *")] _MonoImage* meta, int table, int idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_locate_token([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_string_heap([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_blob_heap([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_user_string([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_guid_heap([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_typedef_from_field([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_typedef_from_method([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_nested_in_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_nesting_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("uint32_t")] uint start_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass **")]
        public static extern _MonoClass** mono_metadata_interfaces_from_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("unsigned int *")] uint* count);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_events_from_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("unsigned int *")] uint* end_idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_methods_from_event([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("unsigned int *")] uint* end);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_properties_from_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("unsigned int *")] uint* end);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_methods_from_property([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("unsigned int *")] uint* end);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_packing_from_typedef([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("uint32_t *")] uint* packing, [NativeTypeName("uint32_t *")] uint* size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_metadata_get_marshal_info([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint idx, [NativeTypeName("mono_bool")] int is_field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_custom_attrs_from_index([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint cattr_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoMarshalSpec* mono_metadata_parse_marshal_spec([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const char *")] sbyte* ptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_free_marshal_spec(MonoMarshalSpec* spec);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_implmap_from_method([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint method_idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_field_info([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint table_index, [NativeTypeName("uint32_t *")] uint* offset, [NativeTypeName("uint32_t *")] uint* rva, MonoMarshalSpec** marshal_spec);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_get_constant_index([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint token, [NativeTypeName("uint32_t")] uint hint);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_decode_value([NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_metadata_decode_signed_value([NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_decode_blob_size([NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_encode_value([NativeTypeName("uint32_t")] uint value, [NativeTypeName("char *")] sbyte* bug, [NativeTypeName("char **")] sbyte** endbuf);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_byref([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_type_get_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_type_get_signature([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_type_get_class([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArrayType *")]
        public static extern _MonoArrayType* mono_type_get_array_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_type_get_ptr_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_type_get_modifiers([NativeTypeName("MonoType *")] _MonoType* type, [NativeTypeName("mono_bool *")] int* is_required, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_struct([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_void([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_pointer([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_reference([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_is_generic_parameter([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_signature_get_return_type([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_signature_get_params([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig, void** iter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_signature_get_param_count([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_signature_get_call_conv([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_signature_vararg_start([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_signature_is_instance([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_signature_explicit_this([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_signature_param_is_out([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig, int param_num);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_parse_typedef_or_ref([NativeTypeName("MonoImage *")] _MonoImage* m, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_metadata_parse_custom_mod([NativeTypeName("MonoImage *")] _MonoImage* m, MonoCustomMod* dest, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArrayType *")]
        public static extern _MonoArrayType* mono_metadata_parse_array([NativeTypeName("MonoImage *")] _MonoImage* m, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_free_array([NativeTypeName("MonoArrayType *")] _MonoArrayType* array);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_metadata_parse_type([NativeTypeName("MonoImage *")] _MonoImage* m, MonoParseTypeMode mode, short opt_attrs, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_metadata_parse_param([NativeTypeName("MonoImage *")] _MonoImage* m, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_metadata_parse_field_type([NativeTypeName("MonoImage *")] _MonoImage* m, short field_flags, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_type_create_from_typespec([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint type_spec);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_free_type([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_type_size([NativeTypeName("MonoType *")] _MonoType* type, int* alignment);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_type_stack_size([NativeTypeName("MonoType *")] _MonoType* type, int* alignment);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_type_generic_inst_is_valuetype([NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_metadata_generic_class_is_valuetype([NativeTypeName("MonoGenericClass *")] _MonoGenericClass* gclass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint mono_metadata_type_hash([NativeTypeName("MonoType *")] _MonoType* t1);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_metadata_type_equal([NativeTypeName("MonoType *")] _MonoType* t1, [NativeTypeName("MonoType *")] _MonoType* t2);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_metadata_signature_alloc([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint nparams);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_metadata_signature_dup([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_metadata_parse_signature([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodSignature *")]
        public static extern _MonoMethodSignature* mono_metadata_parse_method_signature([NativeTypeName("MonoImage *")] _MonoImage* m, int def, [NativeTypeName("const char *")] sbyte* ptr, [NativeTypeName("const char **")] sbyte** rptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_free_method_signature([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_metadata_signature_equal([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig1, [NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig2);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint mono_signature_hash([NativeTypeName("MonoMethodSignature *")] _MonoMethodSignature* sig);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethodHeader *")]
        public static extern _MonoMethodHeader* mono_metadata_parse_mh([NativeTypeName("MonoImage *")] _MonoImage* m, [NativeTypeName("const char *")] sbyte* ptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_free_mh([NativeTypeName("MonoMethodHeader *")] _MonoMethodHeader* mh);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const unsigned char *")]
        public static extern byte* mono_method_header_get_code([NativeTypeName("MonoMethodHeader *")] _MonoMethodHeader* header, [NativeTypeName("uint32_t *")] uint* code_size, [NativeTypeName("uint32_t *")] uint* max_stack);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType **")]
        public static extern _MonoType** mono_method_header_get_locals([NativeTypeName("MonoMethodHeader *")] _MonoMethodHeader* header, [NativeTypeName("uint32_t *")] uint* num_locals, [NativeTypeName("mono_bool *")] int* init_locals);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_method_header_get_num_clauses([NativeTypeName("MonoMethodHeader *")] _MonoMethodHeader* header);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_method_header_get_clauses([NativeTypeName("MonoMethodHeader *")] _MonoMethodHeader* header, [NativeTypeName("MonoMethod *")] _MonoMethod* method, void** iter, MonoExceptionClause* clause);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_type_to_unmanaged([NativeTypeName("MonoType *")] _MonoType* type, MonoMarshalSpec* mspec, [NativeTypeName("mono_bool")] int as_field, [NativeTypeName("mono_bool")] int unicode, MonoMarshalConv* conv);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_token_from_dor([NativeTypeName("uint32_t")] uint dor_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_guid_to_string([NativeTypeName("const uint8_t *")] byte* guid);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_guid_to_string_minimal([NativeTypeName("const uint8_t *")] byte* guid);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_declsec_from_index([NativeTypeName("MonoImage *")] _MonoImage* meta, [NativeTypeName("uint32_t")] uint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_translate_token_index([NativeTypeName("MonoImage *")] _MonoImage* image, int table, [NativeTypeName("uint32_t")] uint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_metadata_decode_table_row([NativeTypeName("MonoImage *")] _MonoImage* image, int table, int idx, [NativeTypeName("uint32_t *")] uint* res, int res_size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_metadata_decode_table_row_col([NativeTypeName("MonoImage *")] _MonoImage* image, int table, int idx, [NativeTypeName("unsigned int")] uint col);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_config_get_os();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_config_get_cpu();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_config_get_wordsize();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_get_config_dir();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_config_dir([NativeTypeName("const char *")] sbyte* dir);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_get_machine_config();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_config_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_config_parse([NativeTypeName("const char *")] sbyte* filename);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_config_for_assembly([NativeTypeName("MonoImage *")] _MonoImage* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_config_parse_memory([NativeTypeName("const char *")] sbyte* buffer);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_config_string_for_assembly_file([NativeTypeName("const char *")] sbyte* filename);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_config_set_server_mode([NativeTypeName("mono_bool")] int server_mode);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_config_is_server_mode();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_debug_enabled();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_init(MonoDebugFormat format);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_open_image_from_memory([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("const mono_byte *")] byte* raw_contents, int size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_close_image([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_domain_unload([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_domain_create([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodAddress *")]
        public static extern _MonoDebugMethodAddress* mono_debug_add_method([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoDebugMethodJitInfo *")] _MonoDebugMethodJitInfo* jit, [NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_remove_method([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodInfo *")]
        public static extern _MonoDebugMethodInfo* mono_debug_lookup_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodAddressList *")]
        public static extern _MonoDebugMethodAddressList* mono_debug_lookup_method_addresses([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodJitInfo *")]
        public static extern _MonoDebugMethodJitInfo* mono_debug_find_method([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugHandle *")]
        public static extern _MonoDebugHandle* mono_debug_get_handle([NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_free_method_jit_info([NativeTypeName("MonoDebugMethodJitInfo *")] _MonoDebugMethodJitInfo* jit);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_add_delegate_trampoline(void* code, int size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugLocalsInfo *")]
        public static extern _MonoDebugLocalsInfo* mono_debug_lookup_locals([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugMethodAsyncInfo *")]
        public static extern _MonoDebugMethodAsyncInfo* mono_debug_lookup_method_async_debug_info([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugSourceLocation *")]
        public static extern _MonoDebugSourceLocation* mono_debug_method_lookup_location([NativeTypeName("MonoDebugMethodInfo *")] _MonoDebugMethodInfo* minfo, int il_offset);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDebugSourceLocation *")]
        public static extern _MonoDebugSourceLocation* mono_debug_lookup_source_location([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("uint32_t")] uint address, [NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_debug_il_offset_from_address([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("uint32_t")] uint native_offset);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_debug_free_source_location([NativeTypeName("MonoDebugSourceLocation *")] _MonoDebugSourceLocation* location);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_debug_print_stack_frame([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("uint32_t")] uint native_offset, [NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_debugger_method_has_breakpoint([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_debugger_insert_breakpoint([NativeTypeName("const char *")] sbyte* method_name, [NativeTypeName("mono_bool")] int include_namespace);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_set_is_debugger_attached([NativeTypeName("mono_bool")] int attached);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_is_debugger_attached();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_collect(int generation);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_gc_max_generation();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_gc_get_generation([NativeTypeName("MonoObject *")] _MonoObject* @object);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_gc_collection_count(int generation);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long mono_gc_get_used_size();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long mono_gc_get_heap_size();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_gc_pending_finalizers();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_finalize_notify();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_gc_invoke_finalizers();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_gc_walk_heap(int flags, [NativeTypeName("MonoGCReferences")] delegate* unmanaged[Cdecl]<_MonoObject*, _MonoClass*, nuint, nuint, _MonoObject**, nuint*, void*, int> callback, void* data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_unichar2 *")]
        public static extern ushort* mono_string_chars([NativeTypeName("MonoString *")] _MonoString* s);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_string_length([NativeTypeName("MonoString *")] _MonoString* s);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_new([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_new_specific(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_new_fast(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_new_alloc_specific(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_new_from_token([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint token);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_array_new([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* eclass, [NativeTypeName("uintptr_t")] nuint n);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_array_new_full([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* array_class, [NativeTypeName("uintptr_t *")] nuint* lengths, [NativeTypeName("intptr_t *")] nint* lower_bounds);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_array_new_specific(MonoVTable* vtable, [NativeTypeName("uintptr_t")] nuint n);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_array_clone([NativeTypeName("MonoArray *")] _MonoArray* array);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_array_addr_with_size([NativeTypeName("MonoArray *")] _MonoArray* array, int size, [NativeTypeName("uintptr_t")] nuint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uintptr_t")]
        public static extern nuint mono_array_length([NativeTypeName("MonoArray *")] _MonoArray* array);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_empty([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_empty_wrapper();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new_utf16([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const mono_unichar2 *")] ushort* text, [NativeTypeName("int32_t")] int len);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new_size([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("int32_t")] int len);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_ldstr([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint str_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_is_interned([NativeTypeName("MonoString *")] _MonoString* str);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_intern([NativeTypeName("MonoString *")] _MonoString* str);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const char *")] sbyte* text);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new_wrapper([NativeTypeName("const char *")] sbyte* text);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new_len([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const char *")] sbyte* text, [NativeTypeName("unsigned int")] uint length);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_new_utf32([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("const mono_unichar4 *")] uint* text, [NativeTypeName("int32_t")] int len);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_string_to_utf8([NativeTypeName("MonoString *")] _MonoString* string_obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_string_to_utf8_checked([NativeTypeName("MonoString *")] _MonoString* string_obj, [NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_unichar2 *")]
        public static extern ushort* mono_string_to_utf16([NativeTypeName("MonoString *")] _MonoString* string_obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_unichar4 *")]
        public static extern uint* mono_string_to_utf32([NativeTypeName("MonoString *")] _MonoString* string_obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_from_utf16([NativeTypeName("mono_unichar2 *")] ushort* data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_string_from_utf32([NativeTypeName("mono_unichar4 *")] uint* data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_string_equal([NativeTypeName("MonoString *")] _MonoString* s1, [NativeTypeName("MonoString *")] _MonoString* s2);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint mono_string_hash([NativeTypeName("MonoString *")] _MonoString* s);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_object_hash([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoString *")]
        public static extern _MonoString* mono_object_to_string([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_value_box([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass, void* val);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_value_copy(void* dest, void* src, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_value_copy_array([NativeTypeName("MonoArray *")] _MonoArray* dest, int dest_idx, void* src, int count);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoVTable* mono_object_get_vtable([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_object_get_domain([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_object_get_class([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_object_unbox([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_clone([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_isinst([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_isinst_mbyref([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_object_castclass_mbyref([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_monitor_try_enter([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("uint32_t")] uint ms);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_monitor_enter([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_monitor_enter_v4([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("char *")] sbyte* lock_taken);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint mono_object_get_size([NativeTypeName("MonoObject *")] _MonoObject* o);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_monitor_exit([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_raise_exception([NativeTypeName("MonoException *")] _MonoException* ex);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_runtime_set_pending_exception([NativeTypeName("MonoException *")] _MonoException* exc, [NativeTypeName("mono_bool")] int overwrite);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_reraise_exception([NativeTypeName("MonoException *")] _MonoException* ex);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_object_init([NativeTypeName("MonoObject *")] _MonoObject* this_obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_class_init(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoDomain *")]
        public static extern _MonoDomain* mono_vtable_domain(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoClass *")]
        public static extern _MonoClass* mono_vtable_class(MonoVTable* vtable);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_object_get_virtual_method([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_runtime_invoke([NativeTypeName("MonoMethod *")] _MonoMethod* method, void* obj, void** @params, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_delegate_invoke([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_delegate_begin_invoke([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoMethod *")]
        public static extern _MonoMethod* mono_get_delegate_end_invoke([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_runtime_delegate_invoke([NativeTypeName("MonoObject *")] _MonoObject* @delegate, void** @params, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_runtime_invoke_array([NativeTypeName("MonoMethod *")] _MonoMethod* method, void* obj, [NativeTypeName("MonoArray *")] _MonoArray* @params, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_method_get_unmanaged_thunk([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_runtime_get_main_args();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_exec_managed_code([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoMainThreadFunc")] delegate* unmanaged[Cdecl]<void*, void> main_func, void* main_args);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_runtime_run_main([NativeTypeName("MonoMethod *")] _MonoMethod* method, int argc, [NativeTypeName("char *[]")] sbyte** argv, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_runtime_exec_main([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoArray *")] _MonoArray* args, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_runtime_set_main_args(int argc, [NativeTypeName("char *[]")] sbyte** argv);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_load_remote_field([NativeTypeName("MonoObject *")] _MonoObject* this_obj, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void** res);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_load_remote_field_new([NativeTypeName("MonoObject *")] _MonoObject* this_obj, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_store_remote_field([NativeTypeName("MonoObject *")] _MonoObject* this_obj, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void* val);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_store_remote_field_new([NativeTypeName("MonoObject *")] _MonoObject* this_obj, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field, [NativeTypeName("MonoObject *")] _MonoObject* arg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_unhandled_exception([NativeTypeName("MonoObject *")] _MonoObject* exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_print_unhandled_exception([NativeTypeName("MonoObject *")] _MonoObject* exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_compile_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_field_set_value([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_field_static_set_value(MonoVTable* vt, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_field_get_value([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_field_static_get_value(MonoVTable* vt, [NativeTypeName("MonoClassField *")] _MonoClassField* field, void* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_field_get_value_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClassField *")] _MonoClassField* field, [NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_property_set_value([NativeTypeName("MonoProperty *")] _MonoProperty* prop, void* obj, void** @params, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_property_get_value([NativeTypeName("MonoProperty *")] _MonoProperty* prop, void* obj, void** @params, [NativeTypeName("MonoObject **")] _MonoObject** exc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_gchandle_new([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("mono_bool")] int pinned);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_gchandle_new_weakref([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("mono_bool")] int track_resurrection);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_gchandle_get_target([NativeTypeName("uint32_t")] uint gchandle);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gchandle_free([NativeTypeName("uint32_t")] uint gchandle);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReferenceQueue *")]
        public static extern _MonoReferenceQueue* mono_gc_reference_queue_new([NativeTypeName("mono_reference_queue_callback")] delegate* unmanaged[Cdecl]<void*, void> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_reference_queue_free([NativeTypeName("MonoReferenceQueue *")] _MonoReferenceQueue* queue);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_gc_reference_queue_add([NativeTypeName("MonoReferenceQueue *")] _MonoReferenceQueue* queue, [NativeTypeName("MonoObject *")] _MonoObject* obj, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_set_field([NativeTypeName("MonoObject *")] _MonoObject* obj, void* field_ptr, [NativeTypeName("MonoObject *")] _MonoObject* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_set_arrayref([NativeTypeName("MonoArray *")] _MonoArray* arr, void* slot_ptr, [NativeTypeName("MonoObject *")] _MonoObject* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_arrayref_copy(void* dest_ptr, void* src_ptr, int count);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_generic_store(void* ptr, [NativeTypeName("MonoObject *")] _MonoObject* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_generic_store_atomic(void* ptr, [NativeTypeName("MonoObject *")] _MonoObject* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_generic_nostore(void* ptr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_value_copy(void* dest, void* src, int count, [NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wbarrier_object_copy([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoObject *")] _MonoObject* src);

        public const int MONO_FLOW_NEXT = 0;
        public const int MONO_FLOW_BRANCH = 1;
        public const int MONO_FLOW_COND_BRANCH = 2;
        public const int MONO_FLOW_ERROR = 3;
        public const int MONO_FLOW_CALL = 4;
        public const int MONO_FLOW_RETURN = 5;
        public const int MONO_FLOW_META = 6;

        public const int MonoInlineNone = 0;
        public const int MonoInlineType = 1;
        public const int MonoInlineField = 2;
        public const int MonoInlineMethod = 3;
        public const int MonoInlineTok = 4;
        public const int MonoInlineString = 5;
        public const int MonoInlineSig = 6;
        public const int MonoInlineVar = 7;
        public const int MonoShortInlineVar = 8;
        public const int MonoInlineBrTarget = 9;
        public const int MonoShortInlineBrTarget = 10;
        public const int MonoInlineSwitch = 11;
        public const int MonoInlineR = 12;
        public const int MonoShortInlineR = 13;
        public const int MonoInlineI = 14;
        public const int MonoShortInlineI = 15;
        public const int MonoInlineI8 = 16;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_opcode_name(int opcode);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoOpcodeEnum mono_opcode_value([NativeTypeName("const mono_byte **")] byte** ip, [NativeTypeName("const mono_byte *")] byte* end);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_profiler_load([NativeTypeName("const char *")] sbyte* desc);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoProfilerHandle")]
        public static extern _MonoProfilerDesc* mono_profiler_create([NativeTypeName("MonoProfiler *")] _MonoProfiler* prof);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_profiler_set_cleanup_callback([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, [NativeTypeName("MonoProfilerCleanupCallback")] delegate* unmanaged[Cdecl]<_MonoProfiler*, void> cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_enable_coverage();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_profiler_set_coverage_filter_callback([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, [NativeTypeName("MonoProfilerCoverageFilterCallback")] delegate* unmanaged[Cdecl]<_MonoProfiler*, _MonoMethod*, int> cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_get_coverage_data([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, [NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoProfilerCoverageCallback")] delegate* unmanaged[Cdecl]<_MonoProfiler*, MonoProfilerCoverageData*, void> cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_enable_sampling([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_set_sample_mode([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, MonoProfilerSampleMode mode, [NativeTypeName("uint32_t")] uint freq);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_get_sample_mode([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, MonoProfilerSampleMode* mode, [NativeTypeName("uint32_t *")] uint* freq);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_enable_allocations();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_enable_clauses();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_profiler_set_call_instrumentation_filter_callback([NativeTypeName("MonoProfilerHandle")] _MonoProfilerDesc* handle, [NativeTypeName("MonoProfilerCallInstrumentationFilterCallback")] delegate* unmanaged[Cdecl]<_MonoProfiler*, _MonoMethod*, MonoProfilerCallInstrumentationFlags> cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_profiler_enable_call_context_introspection();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_profiler_call_context_get_this([NativeTypeName("MonoProfilerCallContext *")] _MonoProfilerCallContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_profiler_call_context_get_argument([NativeTypeName("MonoProfilerCallContext *")] _MonoProfilerCallContext* context, [NativeTypeName("uint32_t")] uint position);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_profiler_call_context_get_local([NativeTypeName("MonoProfilerCallContext *")] _MonoProfilerCallContext* context, [NativeTypeName("uint32_t")] uint position);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* mono_profiler_call_context_get_result([NativeTypeName("MonoProfilerCallContext *")] _MonoProfilerCallContext* context);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_profiler_call_context_free_buffer(void* buffer);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_reflection_parse_type([NativeTypeName("char *")] sbyte* name, MonoTypeNameParse* info);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_reflection_get_type([NativeTypeName("MonoImage *")] _MonoImage* image, MonoTypeNameParse* info, [NativeTypeName("mono_bool")] int ignorecase, [NativeTypeName("mono_bool *")] int* type_resolve);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_reflection_free_type_info(MonoTypeNameParse* info);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_reflection_type_from_name([NativeTypeName("char *")] sbyte* name, [NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_reflection_get_token([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionAssembly *")]
        public static extern _MonoReflectionAssembly* mono_assembly_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionModule *")]
        public static extern _MonoReflectionModule* mono_module_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoImage *")] _MonoImage* image);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionModule *")]
        public static extern _MonoReflectionModule* mono_module_file_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoImage *")] _MonoImage* image, int table_index);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionType *")]
        public static extern _MonoReflectionType* mono_type_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoType *")] _MonoType* type);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionMethod *")]
        public static extern _MonoReflectionMethod* mono_method_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("MonoClass *")] _MonoClass* refclass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionField *")]
        public static extern _MonoReflectionField* mono_field_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionProperty *")]
        public static extern _MonoReflectionProperty* mono_property_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoProperty *")] _MonoProperty* property);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionEvent *")]
        public static extern _MonoReflectionEvent* mono_event_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_param_get_objects([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoReflectionMethodBody *")]
        public static extern _MonoReflectionMethodBody* mono_method_body_get_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain, [NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_get_dbnull_object([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_reflection_get_custom_attrs_by_type([NativeTypeName("MonoObject *")] _MonoObject* obj, [NativeTypeName("MonoClass *")] _MonoClass* attr_klass, [NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_reflection_get_custom_attrs([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_reflection_get_custom_attrs_data([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_reflection_get_custom_attrs_blob([NativeTypeName("MonoReflectionAssembly *")] _MonoReflectionAssembly* assembly, [NativeTypeName("MonoObject *")] _MonoObject* ctor, [NativeTypeName("MonoArray *")] _MonoArray* ctorArgs, [NativeTypeName("MonoArray *")] _MonoArray* properties, [NativeTypeName("MonoArray *")] _MonoArray* porpValues, [NativeTypeName("MonoArray *")] _MonoArray* fields, [NativeTypeName("MonoArray *")] _MonoArray* fieldValues);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_reflection_get_custom_attrs_info([NativeTypeName("MonoObject *")] _MonoObject* obj);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoArray *")]
        public static extern _MonoArray* mono_custom_attrs_construct(MonoCustomAttrInfo* cinfo);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_index([NativeTypeName("MonoImage *")] _MonoImage* image, [NativeTypeName("uint32_t")] uint idx);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_class([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_assembly([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_property([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoProperty *")] _MonoProperty* property);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_event([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoEvent *")] _MonoEvent* @event);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_field([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("MonoClassField *")] _MonoClassField* field);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoCustomAttrInfo* mono_custom_attrs_from_param([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("uint32_t")] uint param1);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_custom_attrs_has_attr(MonoCustomAttrInfo* ainfo, [NativeTypeName("MonoClass *")] _MonoClass* attr_klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoObject *")]
        public static extern _MonoObject* mono_custom_attrs_get_attr(MonoCustomAttrInfo* ainfo, [NativeTypeName("MonoClass *")] _MonoClass* attr_klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_custom_attrs_free(MonoCustomAttrInfo* ainfo);

        public const int MONO_DECLSEC_FLAG_REQUEST = 0x00000001;
        public const int MONO_DECLSEC_FLAG_DEMAND = 0x00000002;
        public const int MONO_DECLSEC_FLAG_ASSERT = 0x00000004;
        public const int MONO_DECLSEC_FLAG_DENY = 0x00000008;
        public const int MONO_DECLSEC_FLAG_PERMITONLY = 0x00000010;
        public const int MONO_DECLSEC_FLAG_LINKDEMAND = 0x00000020;
        public const int MONO_DECLSEC_FLAG_INHERITANCEDEMAND = 0x00000040;
        public const int MONO_DECLSEC_FLAG_REQUEST_MINIMUM = 0x00000080;
        public const int MONO_DECLSEC_FLAG_REQUEST_OPTIONAL = 0x00000100;
        public const int MONO_DECLSEC_FLAG_REQUEST_REFUSE = 0x00000200;
        public const int MONO_DECLSEC_FLAG_PREJIT_GRANT = 0x00000400;
        public const int MONO_DECLSEC_FLAG_PREJIT_DENY = 0x00000800;
        public const int MONO_DECLSEC_FLAG_NONCAS_DEMAND = 0x00001000;
        public const int MONO_DECLSEC_FLAG_NONCAS_LINKDEMAND = 0x00002000;
        public const int MONO_DECLSEC_FLAG_NONCAS_INHERITANCEDEMAND = 0x00004000;
        public const int MONO_DECLSEC_FLAG_LINKDEMAND_CHOICE = 0x00008000;
        public const int MONO_DECLSEC_FLAG_INHERITANCEDEMAND_CHOICE = 0x00010000;
        public const int MONO_DECLSEC_FLAG_DEMAND_CHOICE = 0x00020000;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_declsec_flags_from_method([NativeTypeName("MonoMethod *")] _MonoMethod* method);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_declsec_flags_from_class([NativeTypeName("MonoClass *")] _MonoClass* klass);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_declsec_flags_from_assembly([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_demands([NativeTypeName("MonoMethod *")] _MonoMethod* callee, MonoDeclSecurityActions* demands);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_linkdemands([NativeTypeName("MonoMethod *")] _MonoMethod* callee, MonoDeclSecurityActions* klass, MonoDeclSecurityActions* cmethod);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_inheritdemands_class([NativeTypeName("MonoClass *")] _MonoClass* klass, MonoDeclSecurityActions* demands);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_inheritdemands_method([NativeTypeName("MonoMethod *")] _MonoMethod* callee, MonoDeclSecurityActions* demands);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_method_action([NativeTypeName("MonoMethod *")] _MonoMethod* method, [NativeTypeName("uint32_t")] uint action, MonoDeclSecurityEntry* entry);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_class_action([NativeTypeName("MonoClass *")] _MonoClass* klass, [NativeTypeName("uint32_t")] uint action, MonoDeclSecurityEntry* entry);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoBoolean")]
        public static extern byte mono_declsec_get_assembly_action([NativeTypeName("MonoAssembly *")] _MonoAssembly* assembly, [NativeTypeName("uint32_t")] uint action, MonoDeclSecurityEntry* entry);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoType *")]
        public static extern _MonoType* mono_reflection_type_get_type([NativeTypeName("MonoReflectionType *")] _MonoReflectionType* reftype);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoAssembly *")]
        public static extern _MonoAssembly* mono_reflection_assembly_get_assembly([NativeTypeName("MonoReflectionAssembly *")] _MonoReflectionAssembly* refassembly);

        public const int MONO_ASSEMBLY_HASH_ALG = 0;
        public const int MONO_ASSEMBLY_MAJOR_VERSION = 1;
        public const int MONO_ASSEMBLY_MINOR_VERSION = 2;
        public const int MONO_ASSEMBLY_BUILD_NUMBER = 3;
        public const int MONO_ASSEMBLY_REV_NUMBER = 4;
        public const int MONO_ASSEMBLY_FLAGS = 5;
        public const int MONO_ASSEMBLY_PUBLIC_KEY = 6;
        public const int MONO_ASSEMBLY_NAME = 7;
        public const int MONO_ASSEMBLY_CULTURE = 8;
        public const int MONO_ASSEMBLY_SIZE = 9;

        public const int MONO_ASSEMBLYOS_PLATFORM = 0;
        public const int MONO_ASSEMBLYOS_MAJOR_VERSION = 1;
        public const int MONO_ASSEMBLYOS_MINOR_VERSION = 2;
        public const int MONO_ASSEMBLYOS_SIZE = 3;

        public const int MONO_ASSEMBLY_PROCESSOR = 0;
        public const int MONO_ASSEMBLY_PROCESSOR_SIZE = 1;

        public const int MONO_ASSEMBLYREF_MAJOR_VERSION = 0;
        public const int MONO_ASSEMBLYREF_MINOR_VERSION = 1;
        public const int MONO_ASSEMBLYREF_BUILD_NUMBER = 2;
        public const int MONO_ASSEMBLYREF_REV_NUMBER = 3;
        public const int MONO_ASSEMBLYREF_FLAGS = 4;
        public const int MONO_ASSEMBLYREF_PUBLIC_KEY = 5;
        public const int MONO_ASSEMBLYREF_NAME = 6;
        public const int MONO_ASSEMBLYREF_CULTURE = 7;
        public const int MONO_ASSEMBLYREF_HASH_VALUE = 8;
        public const int MONO_ASSEMBLYREF_SIZE = 9;

        public const int MONO_ASSEMBLYREFOS_PLATFORM = 0;
        public const int MONO_ASSEMBLYREFOS_MAJOR_VERSION = 1;
        public const int MONO_ASSEMBLYREFOS_MINOR_VERSION = 2;
        public const int MONO_ASSEMBLYREFOS_ASSEMBLYREF = 3;
        public const int MONO_ASSEMBLYREFOS_SIZE = 4;

        public const int MONO_ASSEMBLYREFPROC_PROCESSOR = 0;
        public const int MONO_ASSEMBLYREFPROC_ASSEMBLYREF = 1;
        public const int MONO_ASSEMBLYREFPROC_SIZE = 2;

        public const int MONO_CLASS_LAYOUT_PACKING_SIZE = 0;
        public const int MONO_CLASS_LAYOUT_CLASS_SIZE = 1;
        public const int MONO_CLASS_LAYOUT_PARENT = 2;
        public const int MONO_CLASS_LAYOUT_SIZE = 3;

        public const int MONO_CONSTANT_TYPE = 0;
        public const int MONO_CONSTANT_PADDING = 1;
        public const int MONO_CONSTANT_PARENT = 2;
        public const int MONO_CONSTANT_VALUE = 3;
        public const int MONO_CONSTANT_SIZE = 4;

        public const int MONO_CUSTOM_ATTR_PARENT = 0;
        public const int MONO_CUSTOM_ATTR_TYPE = 1;
        public const int MONO_CUSTOM_ATTR_VALUE = 2;
        public const int MONO_CUSTOM_ATTR_SIZE = 3;

        public const int MONO_DECL_SECURITY_ACTION = 0;
        public const int MONO_DECL_SECURITY_PARENT = 1;
        public const int MONO_DECL_SECURITY_PERMISSIONSET = 2;
        public const int MONO_DECL_SECURITY_SIZE = 3;

        public const int MONO_EVENT_MAP_PARENT = 0;
        public const int MONO_EVENT_MAP_EVENTLIST = 1;
        public const int MONO_EVENT_MAP_SIZE = 2;

        public const int MONO_EVENT_FLAGS = 0;
        public const int MONO_EVENT_NAME = 1;
        public const int MONO_EVENT_TYPE = 2;
        public const int MONO_EVENT_SIZE = 3;

        public const int MONO_EVENT_POINTER_EVENT = 0;
        public const int MONO_EVENT_POINTER_SIZE = 1;

        public const int MONO_EXP_TYPE_FLAGS = 0;
        public const int MONO_EXP_TYPE_TYPEDEF = 1;
        public const int MONO_EXP_TYPE_NAME = 2;
        public const int MONO_EXP_TYPE_NAMESPACE = 3;
        public const int MONO_EXP_TYPE_IMPLEMENTATION = 4;
        public const int MONO_EXP_TYPE_SIZE = 5;

        public const int MONO_FIELD_FLAGS = 0;
        public const int MONO_FIELD_NAME = 1;
        public const int MONO_FIELD_SIGNATURE = 2;
        public const int MONO_FIELD_SIZE = 3;

        public const int MONO_FIELD_LAYOUT_OFFSET = 0;
        public const int MONO_FIELD_LAYOUT_FIELD = 1;
        public const int MONO_FIELD_LAYOUT_SIZE = 2;

        public const int MONO_FIELD_MARSHAL_PARENT = 0;
        public const int MONO_FIELD_MARSHAL_NATIVE_TYPE = 1;
        public const int MONO_FIELD_MARSHAL_SIZE = 2;

        public const int MONO_FIELD_POINTER_FIELD = 0;
        public const int MONO_FIELD_POINTER_SIZE = 1;

        public const int MONO_FIELD_RVA_RVA = 0;
        public const int MONO_FIELD_RVA_FIELD = 1;
        public const int MONO_FIELD_RVA_SIZE = 2;

        public const int MONO_FILE_FLAGS = 0;
        public const int MONO_FILE_NAME = 1;
        public const int MONO_FILE_HASH_VALUE = 2;
        public const int MONO_FILE_SIZE = 3;

        public const int MONO_IMPLMAP_FLAGS = 0;
        public const int MONO_IMPLMAP_MEMBER = 1;
        public const int MONO_IMPLMAP_NAME = 2;
        public const int MONO_IMPLMAP_SCOPE = 3;
        public const int MONO_IMPLMAP_SIZE = 4;

        public const int MONO_INTERFACEIMPL_CLASS = 0;
        public const int MONO_INTERFACEIMPL_INTERFACE = 1;
        public const int MONO_INTERFACEIMPL_SIZE = 2;

        public const int MONO_MANIFEST_OFFSET = 0;
        public const int MONO_MANIFEST_FLAGS = 1;
        public const int MONO_MANIFEST_NAME = 2;
        public const int MONO_MANIFEST_IMPLEMENTATION = 3;
        public const int MONO_MANIFEST_SIZE = 4;

        public const int MONO_MEMBERREF_CLASS = 0;
        public const int MONO_MEMBERREF_NAME = 1;
        public const int MONO_MEMBERREF_SIGNATURE = 2;
        public const int MONO_MEMBERREF_SIZE = 3;

        public const int MONO_METHOD_RVA = 0;
        public const int MONO_METHOD_IMPLFLAGS = 1;
        public const int MONO_METHOD_FLAGS = 2;
        public const int MONO_METHOD_NAME = 3;
        public const int MONO_METHOD_SIGNATURE = 4;
        public const int MONO_METHOD_PARAMLIST = 5;
        public const int MONO_METHOD_SIZE = 6;

        public const int MONO_METHODIMPL_CLASS = 0;
        public const int MONO_METHODIMPL_BODY = 1;
        public const int MONO_METHODIMPL_DECLARATION = 2;
        public const int MONO_METHODIMPL_SIZE = 3;

        public const int MONO_METHOD_POINTER_METHOD = 0;
        public const int MONO_METHOD_POINTER_SIZE = 1;

        public const int MONO_METHOD_SEMA_SEMANTICS = 0;
        public const int MONO_METHOD_SEMA_METHOD = 1;
        public const int MONO_METHOD_SEMA_ASSOCIATION = 2;
        public const int MONO_METHOD_SEMA_SIZE = 3;

        public const int MONO_MODULE_GENERATION = 0;
        public const int MONO_MODULE_NAME = 1;
        public const int MONO_MODULE_MVID = 2;
        public const int MONO_MODULE_ENC = 3;
        public const int MONO_MODULE_ENCBASE = 4;
        public const int MONO_MODULE_SIZE = 5;

        public const int MONO_MODULEREF_NAME = 0;
        public const int MONO_MODULEREF_SIZE = 1;

        public const int MONO_NESTED_CLASS_NESTED = 0;
        public const int MONO_NESTED_CLASS_ENCLOSING = 1;
        public const int MONO_NESTED_CLASS_SIZE = 2;

        public const int MONO_PARAM_FLAGS = 0;
        public const int MONO_PARAM_SEQUENCE = 1;
        public const int MONO_PARAM_NAME = 2;
        public const int MONO_PARAM_SIZE = 3;

        public const int MONO_PARAM_POINTER_PARAM = 0;
        public const int MONO_PARAM_POINTER_SIZE = 1;

        public const int MONO_PROPERTY_FLAGS = 0;
        public const int MONO_PROPERTY_NAME = 1;
        public const int MONO_PROPERTY_TYPE = 2;
        public const int MONO_PROPERTY_SIZE = 3;

        public const int MONO_PROPERTY_POINTER_PROPERTY = 0;
        public const int MONO_PROPERTY_POINTER_SIZE = 1;

        public const int MONO_PROPERTY_MAP_PARENT = 0;
        public const int MONO_PROPERTY_MAP_PROPERTY_LIST = 1;
        public const int MONO_PROPERTY_MAP_SIZE = 2;

        public const int MONO_STAND_ALONE_SIGNATURE = 0;
        public const int MONO_STAND_ALONE_SIGNATURE_SIZE = 1;

        public const int MONO_TYPEDEF_FLAGS = 0;
        public const int MONO_TYPEDEF_NAME = 1;
        public const int MONO_TYPEDEF_NAMESPACE = 2;
        public const int MONO_TYPEDEF_EXTENDS = 3;
        public const int MONO_TYPEDEF_FIELD_LIST = 4;
        public const int MONO_TYPEDEF_METHOD_LIST = 5;
        public const int MONO_TYPEDEF_SIZE = 6;

        public const int MONO_TYPEREF_SCOPE = 0;
        public const int MONO_TYPEREF_NAME = 1;
        public const int MONO_TYPEREF_NAMESPACE = 2;
        public const int MONO_TYPEREF_SIZE = 3;

        public const int MONO_TYPESPEC_SIGNATURE = 0;
        public const int MONO_TYPESPEC_SIZE = 1;

        public const int MONO_GENERICPARAM_NUMBER = 0;
        public const int MONO_GENERICPARAM_FLAGS = 1;
        public const int MONO_GENERICPARAM_OWNER = 2;
        public const int MONO_GENERICPARAM_NAME = 3;
        public const int MONO_GENERICPARAM_SIZE = 4;

        public const int MONO_METHODSPEC_METHOD = 0;
        public const int MONO_METHODSPEC_SIGNATURE = 1;
        public const int MONO_METHODSPEC_SIZE = 2;

        public const int MONO_GENPARCONSTRAINT_GENERICPAR = 0;
        public const int MONO_GENPARCONSTRAINT_CONSTRAINT = 1;
        public const int MONO_GENPARCONSTRAINT_SIZE = 2;

        public const int MONO_DOCUMENT_NAME = 0;
        public const int MONO_DOCUMENT_HASHALG = 1;
        public const int MONO_DOCUMENT_HASH = 2;
        public const int MONO_DOCUMENT_LANGUAGE = 3;
        public const int MONO_DOCUMENT_SIZE = 4;

        public const int MONO_METHODBODY_DOCUMENT = 0;
        public const int MONO_METHODBODY_SEQ_POINTS = 1;
        public const int MONO_METHODBODY_SIZE = 2;

        public const int MONO_LOCALSCOPE_METHOD = 0;
        public const int MONO_LOCALSCOPE_IMPORTSCOPE = 1;
        public const int MONO_LOCALSCOPE_VARIABLELIST = 2;
        public const int MONO_LOCALSCOPE_CONSTANTLIST = 3;
        public const int MONO_LOCALSCOPE_STARTOFFSET = 4;
        public const int MONO_LOCALSCOPE_LENGTH = 5;
        public const int MONO_LOCALSCOPE_SIZE = 6;

        public const int MONO_LOCALVARIABLE_ATTRIBUTES = 0;
        public const int MONO_LOCALVARIABLE_INDEX = 1;
        public const int MONO_LOCALVARIABLE_NAME = 2;
        public const int MONO_LOCALVARIABLE_SIZE = 3;

        public const int MONO_CUSTOMDEBUGINFORMATION_PARENT = 0;
        public const int MONO_CUSTOMDEBUGINFORMATION_KIND = 1;
        public const int MONO_CUSTOMDEBUGINFORMATION_VALUE = 2;
        public const int MONO_CUSTOMDEBUGINFORMATION_SIZE = 3;

        public const int MONO_TYPEDEFORREF_TYPEDEF = 0;
        public const int MONO_TYPEDEFORREF_TYPEREF = 1;
        public const int MONO_TYPEDEFORREF_TYPESPEC = 2;
        public const int MONO_TYPEDEFORREF_BITS = 2;
        public const int MONO_TYPEDEFORREF_MASK = 3;

        public const int MONO_HASCONSTANT_FIEDDEF = 0;
        public const int MONO_HASCONSTANT_PARAM = 1;
        public const int MONO_HASCONSTANT_PROPERTY = 2;
        public const int MONO_HASCONSTANT_BITS = 2;
        public const int MONO_HASCONSTANT_MASK = 3;

        public const int MONO_CUSTOM_ATTR_METHODDEF = 0;
        public const int MONO_CUSTOM_ATTR_FIELDDEF = 1;
        public const int MONO_CUSTOM_ATTR_TYPEREF = 2;
        public const int MONO_CUSTOM_ATTR_TYPEDEF = 3;
        public const int MONO_CUSTOM_ATTR_PARAMDEF = 4;
        public const int MONO_CUSTOM_ATTR_INTERFACE = 5;
        public const int MONO_CUSTOM_ATTR_MEMBERREF = 6;
        public const int MONO_CUSTOM_ATTR_MODULE = 7;
        public const int MONO_CUSTOM_ATTR_PERMISSION = 8;
        public const int MONO_CUSTOM_ATTR_PROPERTY = 9;
        public const int MONO_CUSTOM_ATTR_EVENT = 10;
        public const int MONO_CUSTOM_ATTR_SIGNATURE = 11;
        public const int MONO_CUSTOM_ATTR_MODULEREF = 12;
        public const int MONO_CUSTOM_ATTR_TYPESPEC = 13;
        public const int MONO_CUSTOM_ATTR_ASSEMBLY = 14;
        public const int MONO_CUSTOM_ATTR_ASSEMBLYREF = 15;
        public const int MONO_CUSTOM_ATTR_FILE = 16;
        public const int MONO_CUSTOM_ATTR_EXP_TYPE = 17;
        public const int MONO_CUSTOM_ATTR_MANIFEST = 18;
        public const int MONO_CUSTOM_ATTR_GENERICPAR = 19;
        public const int MONO_CUSTOM_ATTR_GENERICPARAMCONSTRAINT = 20;
        public const int MONO_CUSTOM_ATTR_BITS = 5;
        public const int MONO_CUSTOM_ATTR_MASK = 0x1F;

        public const int MONO_HAS_FIELD_MARSHAL_FIELDSREF = 0;
        public const int MONO_HAS_FIELD_MARSHAL_PARAMDEF = 1;
        public const int MONO_HAS_FIELD_MARSHAL_BITS = 1;
        public const int MONO_HAS_FIELD_MARSHAL_MASK = 1;

        public const int MONO_HAS_DECL_SECURITY_TYPEDEF = 0;
        public const int MONO_HAS_DECL_SECURITY_METHODDEF = 1;
        public const int MONO_HAS_DECL_SECURITY_ASSEMBLY = 2;
        public const int MONO_HAS_DECL_SECURITY_BITS = 2;
        public const int MONO_HAS_DECL_SECURITY_MASK = 3;

        public const int MONO_MEMBERREF_PARENT_TYPEDEF = 0;
        public const int MONO_MEMBERREF_PARENT_TYPEREF = 1;
        public const int MONO_MEMBERREF_PARENT_MODULEREF = 2;
        public const int MONO_MEMBERREF_PARENT_METHODDEF = 3;
        public const int MONO_MEMBERREF_PARENT_TYPESPEC = 4;
        public const int MONO_MEMBERREF_PARENT_BITS = 3;
        public const int MONO_MEMBERREF_PARENT_MASK = 7;

        public const int MONO_HAS_SEMANTICS_EVENT = 0;
        public const int MONO_HAS_SEMANTICS_PROPERTY = 1;
        public const int MONO_HAS_SEMANTICS_BITS = 1;
        public const int MONO_HAS_SEMANTICS_MASK = 1;

        public const int MONO_METHODDEFORREF_METHODDEF = 0;
        public const int MONO_METHODDEFORREF_METHODREF = 1;
        public const int MONO_METHODDEFORREF_BITS = 1;
        public const int MONO_METHODDEFORREF_MASK = 1;

        public const int MONO_MEMBERFORWD_FIELDDEF = 0;
        public const int MONO_MEMBERFORWD_METHODDEF = 1;
        public const int MONO_MEMBERFORWD_BITS = 1;
        public const int MONO_MEMBERFORWD_MASK = 1;

        public const int MONO_IMPLEMENTATION_FILE = 0;
        public const int MONO_IMPLEMENTATION_ASSEMBLYREF = 1;
        public const int MONO_IMPLEMENTATION_EXP_TYPE = 2;
        public const int MONO_IMPLEMENTATION_BITS = 2;
        public const int MONO_IMPLEMENTATION_MASK = 3;

        public const int MONO_CUSTOM_ATTR_TYPE_TYPEREF = 0;
        public const int MONO_CUSTOM_ATTR_TYPE_TYPEDEF = 1;
        public const int MONO_CUSTOM_ATTR_TYPE_METHODDEF = 2;
        public const int MONO_CUSTOM_ATTR_TYPE_MEMBERREF = 3;
        public const int MONO_CUSTOM_ATTR_TYPE_STRING = 4;
        public const int MONO_CUSTOM_ATTR_TYPE_BITS = 3;
        public const int MONO_CUSTOM_ATTR_TYPE_MASK = 7;

        public const int MONO_RESOLUTION_SCOPE_MODULE = 0;
        public const int MONO_RESOLUTION_SCOPE_MODULEREF = 1;
        public const int MONO_RESOLUTION_SCOPE_ASSEMBLYREF = 2;
        public const int MONO_RESOLUTION_SCOPE_TYPEREF = 3;
        public const int MONO_RESOLUTION_SCOPE_BITS = 2;
        public const int MONO_RESOLUTION_SCOPE_MASK = 3;

        public const int MONO_RESOLTION_SCOPE_MODULE = 0;
        public const int MONO_RESOLTION_SCOPE_MODULEREF = 1;
        public const int MONO_RESOLTION_SCOPE_ASSEMBLYREF = 2;
        public const int MONO_RESOLTION_SCOPE_TYPEREF = 3;
        public const int MONO_RESOLTION_SCOPE_BITS = 2;
        public const int MONO_RESOLTION_SCOPE_MASK = 3;

        public const int MONO_TYPEORMETHOD_TYPE = 0;
        public const int MONO_TYPEORMETHOD_METHOD = 1;
        public const int MONO_TYPEORMETHOD_BITS = 1;
        public const int MONO_TYPEORMETHOD_MASK = 1;

        public const int SGEN_BRIDGE_VERSION = 5;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_register_bridge_callbacks(MonoGCBridgeCallbacks* callbacks);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_gc_wait_for_bridge_processing();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_init([NativeTypeName("MonoThreadStartCB")] delegate* unmanaged[Cdecl]<nint, void*, void*, void> start_cb, [NativeTypeName("MonoThreadAttachCB")] delegate* unmanaged[Cdecl]<nint, void*, void> attach_cb);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_manage();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoThread *")]
        public static extern _MonoThread* mono_thread_current();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_set_main([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoThread *")]
        public static extern _MonoThread* mono_thread_get_main();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_stop([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_new_init([NativeTypeName("intptr_t")] nint tid, void* stack_start, void* func);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_create([NativeTypeName("MonoDomain *")] _MonoDomain* domain, void* func, void* arg);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("MonoThread *")]
        public static extern _MonoThread* mono_thread_attach([NativeTypeName("MonoDomain *")] _MonoDomain* domain);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_detach([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_exit();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_threads_attach_tools_thread();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_thread_get_name_utf8([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int mono_thread_get_managed_id([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_thread_set_manage_callback([NativeTypeName("MonoThread *")] _MonoThread* thread, [NativeTypeName("MonoThreadManageCallback")] delegate* unmanaged[Cdecl]<_MonoThread*, int> func);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_threads_set_default_stacksize([NativeTypeName("uint32_t")] uint stacksize);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint mono_threads_get_default_stacksize();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_threads_request_thread_dump();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_thread_is_foreign([NativeTypeName("MonoThread *")] _MonoThread* thread);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_thread_detach_if_exiting();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("GSList *")]
        public static extern void* mono_method_verify([NativeTypeName("MonoMethod *")] _MonoMethod* method, int level);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_free_verify_list([NativeTypeName("GSList *")] void* list);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* mono_verify_corlib();

        public const int MONO_COUNTER_INT = 0;
        public const int MONO_COUNTER_UINT = 1;
        public const int MONO_COUNTER_WORD = 2;
        public const int MONO_COUNTER_LONG = 3;
        public const int MONO_COUNTER_ULONG = 4;
        public const int MONO_COUNTER_DOUBLE = 5;
        public const int MONO_COUNTER_STRING = 6;
        public const int MONO_COUNTER_TIME_INTERVAL = 7;
        public const int MONO_COUNTER_TYPE_MASK = 0xf;
        public const int MONO_COUNTER_CALLBACK = 128;
        public const int MONO_COUNTER_SECTION_MASK = 0x00ffff00;
        public const int MONO_COUNTER_JIT = 1 << 8;
        public const int MONO_COUNTER_GC = 1 << 9;
        public const int MONO_COUNTER_METADATA = 1 << 10;
        public const int MONO_COUNTER_GENERICS = 1 << 11;
        public const int MONO_COUNTER_SECURITY = 1 << 12;
        public const int MONO_COUNTER_RUNTIME = 1 << 13;
        public const int MONO_COUNTER_SYSTEM = 1 << 14;
        public const int MONO_COUNTER_PERFCOUNTERS = 1 << 15;
        public const int MONO_COUNTER_PROFILER = 1 << 16;
        public const int MONO_COUNTER_INTERP = 1 << 17;
        public const int MONO_COUNTER_TIERED = 1 << 18;
        public const int MONO_COUNTER_LAST_SECTION = 262145;
        public const int MONO_COUNTER_UNIT_SHIFT = 24;
        public const int MONO_COUNTER_UNIT_MASK = 0xF << MONO_COUNTER_UNIT_SHIFT;
        public const int MONO_COUNTER_RAW = 0;
        public const int MONO_COUNTER_BYTES = 1 << 24;
        public const int MONO_COUNTER_TIME = 2 << 24;
        public const int MONO_COUNTER_COUNT = 3 << 24;
        public const int MONO_COUNTER_PERCENTAGE = 4 << 24;
        public const int MONO_COUNTER_VARIANCE_SHIFT = 28;
        public const int MONO_COUNTER_VARIANCE_MASK = unchecked((int)(0xFU << MONO_COUNTER_VARIANCE_SHIFT));
        public const int MONO_COUNTER_MONOTONIC = 1 << 28;
        public const int MONO_COUNTER_CONSTANT = 1 << 29;
        public const int MONO_COUNTER_VARIABLE = 1 << 30;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_enable(int section_mask);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_init();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_register([NativeTypeName("const char *")] sbyte* descr, int type, void* addr);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_register_with_size([NativeTypeName("const char *")] sbyte* name, int type, void* addr, int size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_on_register([NativeTypeName("MonoCounterRegisterCallback")] delegate* unmanaged[Cdecl]<_MonoCounter*, void> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_dump(int section_mask, [NativeTypeName("FILE *")] void* outfile);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_cleanup();

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_counters_foreach([NativeTypeName("CountersEnumCallback")] delegate* unmanaged[Cdecl]<_MonoCounter*, void*, int> cb, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_counters_sample([NativeTypeName("MonoCounter *")] _MonoCounter* counter, void* buffer, int buffer_size);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_counter_get_name([NativeTypeName("MonoCounter *")] _MonoCounter* name);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_counter_get_type([NativeTypeName("MonoCounter *")] _MonoCounter* counter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_counter_get_section([NativeTypeName("MonoCounter *")] _MonoCounter* counter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_counter_get_unit([NativeTypeName("MonoCounter *")] _MonoCounter* counter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_counter_get_variance([NativeTypeName("MonoCounter *")] _MonoCounter* counter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint mono_counter_get_size([NativeTypeName("MonoCounter *")] _MonoCounter* counter);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int mono_runtime_resource_limit(int resource_type, [NativeTypeName("uintptr_t")] nuint soft_limit, [NativeTypeName("uintptr_t")] nuint hard_limit);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_resource_set_callback([NativeTypeName("MonoResourceCallback")] delegate* unmanaged[Cdecl]<int, nuint, int, void> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_runtime_resource_check_limit(int resource_type, [NativeTypeName("uintptr_t")] nuint value);

        public const int MONO_DL_EAGER = 0;
        public const int MONO_DL_LAZY = 1;
        public const int MONO_DL_LOCAL = 2;
        public const int MONO_DL_MASK = 3;
        public const int MONO_DL_GLOBAL = 4;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MonoDlFallbackHandler* mono_dl_fallback_register([NativeTypeName("MonoDlFallbackLoad")] delegate* unmanaged[Cdecl]<sbyte*, int, sbyte**, void*, void*> load_func, [NativeTypeName("MonoDlFallbackSymbol")] delegate* unmanaged[Cdecl]<void*, sbyte*, sbyte**, void*, void*> symbol_func, [NativeTypeName("MonoDlFallbackClose")] delegate* unmanaged[Cdecl]<void*, void*, void*> close_func, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_dl_fallback_unregister(MonoDlFallbackHandler* handler);

        public const int MONO_ERROR_FREE_STRINGS = 0x0001;
        public const int MONO_ERROR_INCOMPLETE = 0x0002;
        public const int MONO_ERROR_MEMPOOL_BOXED = 0x0004;

        public const int MONO_ERROR_NONE = 0;
        public const int MONO_ERROR_MISSING_METHOD = 1;
        public const int MONO_ERROR_MISSING_FIELD = 2;
        public const int MONO_ERROR_TYPE_LOAD = 3;
        public const int MONO_ERROR_FILE_NOT_FOUND = 4;
        public const int MONO_ERROR_BAD_IMAGE = 5;
        public const int MONO_ERROR_OUT_OF_MEMORY = 6;
        public const int MONO_ERROR_ARGUMENT = 7;
        public const int MONO_ERROR_ARGUMENT_NULL = 11;
        public const int MONO_ERROR_ARGUMENT_OUT_OF_RANGE = 14;
        public const int MONO_ERROR_NOT_VERIFIABLE = 8;
        public const int MONO_ERROR_INVALID_PROGRAM = 12;
        public const int MONO_ERROR_MEMBER_ACCESS = 13;
        public const int MONO_ERROR_GENERIC = 9;
        public const int MONO_ERROR_EXCEPTION_INSTANCE = 10;
        public const int MONO_ERROR_CLEANUP_CALLED_SENTINEL = 0xffff;

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_error_init([NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_error_init_flags([NativeTypeName("MonoError *")] _MonoError* error, [NativeTypeName("unsigned short")] ushort flags);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_error_cleanup([NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_error_ok([NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned short")]
        public static extern ushort mono_error_get_error_code([NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* mono_error_get_message([NativeTypeName("MonoError *")] _MonoError* error);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_trace_set_level_string([NativeTypeName("const char *")] sbyte* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_trace_set_mask_string([NativeTypeName("const char *")] sbyte* value);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_trace_set_log_handler([NativeTypeName("MonoLogCallback")] delegate* unmanaged[Cdecl]<sbyte*, sbyte*, sbyte*, int, void*, void> callback, void* user_data);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_trace_set_print_handler([NativeTypeName("MonoPrintCallback")] delegate* unmanaged[Cdecl]<sbyte*, int, void> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_trace_set_printerr_handler([NativeTypeName("MonoPrintCallback")] delegate* unmanaged[Cdecl]<sbyte*, int, void> callback);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void mono_free(void* param0);

        [DllImport("MONO_LIBRARY_NAME", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("mono_bool")]
        public static extern int mono_set_allocator_vtable(MonoAllocatorVTable* vtable);
    }
}

[Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
internal sealed class NativeTypeNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}