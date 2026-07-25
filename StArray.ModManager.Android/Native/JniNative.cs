using System.Runtime.InteropServices;
using StArray.ModManager;

namespace StArray.ModManager.Android.Native;

[StructLayout(LayoutKind.Explicit)]
public struct JValue
{
    [FieldOffset(0)] public byte Z;
    [FieldOffset(0)] public sbyte B;
    [FieldOffset(0)] public char C;
    [FieldOffset(0)] public short S;
    [FieldOffset(0)] public int I;
    [FieldOffset(0)] public long J;
    [FieldOffset(0)] public float F;
    [FieldOffset(0)] public double D;
    [FieldOffset(0)] public nint L;
}

[NativeImport("modmanager")]
public static partial class JniNative
{
    // ===== 引用管理 =====
    [NativeImport(EntryPoint = "jnihelper_new_global_ref")]
    public static partial nint NewGlobalRef(nint obj);
    [NativeImport(EntryPoint = "jnihelper_new_local_ref")]
    public static partial nint NewLocalRef(nint obj);
    [NativeImport(EntryPoint = "jnihelper_delete_local_ref")]
    public static partial void DeleteLocalRef(nint obj);
    [NativeImport(EntryPoint = "jnihelper_delete_global_ref")]
    public static partial void DeleteGlobalRef(nint obj);
    [NativeImport(EntryPoint = "jnihelper_get_object_class")]
    public static partial nint GetObjectClass(nint obj);

    // ===== 异常 =====
    [NativeImport(EntryPoint = "jnihelper_check_exception")]
    public static partial bool CheckException();
    [NativeImport(EntryPoint = "jnihelper_clear_exception")]
    public static partial void ClearException();

    // ===== 类/方法/字段查找 =====
    [NativeImport(EntryPoint = "jnihelper_find_class")]
    public static partial nint FindClass(string className);
    [NativeImport(EntryPoint = "jnihelper_get_method_id")]
    public static partial nint GetMethodID(nint clazz, string methodName, string signature);
    [NativeImport(EntryPoint = "jnihelper_get_static_method_id")]
    public static partial nint GetStaticMethodID(nint clazz, string methodName, string signature);
    [NativeImport(EntryPoint = "jnihelper_get_field_id")]
    public static partial nint GetFieldID(nint clazz, string fieldName, string signature);
    [NativeImport(EntryPoint = "jnihelper_get_static_field_id")]
    public static partial nint GetStaticFieldID(nint clazz, string fieldName, string signature);

    // ===== 字符串 =====
    [NativeImport(EntryPoint = "jnihelper_new_string")]
    public static partial nint NewString(string str);
    [NativeImport(EntryPoint = "jnihelper_new_string_utf")]
    public static partial nint NewStringUtf(string utf);
    [NativeImport(EntryPoint = "jnihelper_get_string_utf_chars")]
    public static partial nint GetStringUtfChars(nint jstr);
    [NativeImport(EntryPoint = "jnihelper_release_string_utf_chars")]
    public static partial void ReleaseStringUtfChars(nint jstr, nint utf);
    [NativeImport(EntryPoint = "jnihelper_get_string_length")]
    public static partial int GetStringLength(nint jstr);

    // ===== 实例方法调用 (A 变体) =====
    [NativeImport(EntryPoint = "jnihelper_call_object_method_a")]
    public static partial nint CallObjectMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_boolean_method_a")]
    public static partial bool CallBooleanMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_byte_method_a")]
    public static partial sbyte CallByteMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_char_method_a")]
    public static partial char CallCharMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_short_method_a")]
    public static partial short CallShortMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_int_method_a")]
    public static partial int CallIntMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_long_method_a")]
    public static partial long CallLongMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_float_method_a")]
    public static partial float CallFloatMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_double_method_a")]
    public static partial double CallDoubleMethodA(nint obj, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_void_method_a")]
    public static partial void CallVoidMethodA(nint obj, nint methodID, nint args);

    // ===== 实例方法调用 (无参) =====
    [NativeImport(EntryPoint = "jnihelper_call_object_method")]
    public static partial nint CallObjectMethod(nint obj, nint methodID);

    // ===== 静态方法调用 (A 变体) =====
    [NativeImport(EntryPoint = "jnihelper_call_static_object_method_a")]
    public static partial nint CallStaticObjectMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_boolean_method_a")]
    public static partial bool CallStaticBooleanMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_byte_method_a")]
    public static partial sbyte CallStaticByteMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_char_method_a")]
    public static partial char CallStaticCharMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_short_method_a")]
    public static partial short CallStaticShortMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_int_method_a")]
    public static partial int CallStaticIntMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_long_method_a")]
    public static partial long CallStaticLongMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_float_method_a")]
    public static partial float CallStaticFloatMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_double_method_a")]
    public static partial double CallStaticDoubleMethodA(nint clazz, nint methodID, nint args);
    [NativeImport(EntryPoint = "jnihelper_call_static_void_method_a")]
    public static partial void CallStaticVoidMethodA(nint clazz, nint methodID, nint args);

    // ===== 静态方法调用 (无参) =====
    [NativeImport(EntryPoint = "jnihelper_call_static_object_method")]
    public static partial nint CallStaticObjectMethod(nint clazz, nint methodID);

    // ===== 实例字段 Get =====
    [NativeImport(EntryPoint = "jnihelper_get_object_field")]
    public static partial nint GetObjectField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_boolean_field")]
    public static partial bool GetBooleanField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_byte_field")]
    public static partial sbyte GetByteField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_char_field")]
    public static partial char GetCharField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_short_field")]
    public static partial short GetShortField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_int_field")]
    public static partial int GetIntField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_long_field")]
    public static partial long GetLongField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_float_field")]
    public static partial float GetFloatField(nint obj, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_double_field")]
    public static partial double GetDoubleField(nint obj, nint fieldID);

    // ===== 实例字段 Set =====
    [NativeImport(EntryPoint = "jnihelper_set_object_field")]
    public static partial void SetObjectField(nint obj, nint fieldID, nint value);
    [NativeImport(EntryPoint = "jnihelper_set_boolean_field")]
    public static partial void SetBooleanField(nint obj, nint fieldID, bool value);
    [NativeImport(EntryPoint = "jnihelper_set_byte_field")]
    public static partial void SetByteField(nint obj, nint fieldID, sbyte value);
    [NativeImport(EntryPoint = "jnihelper_set_char_field")]
    public static partial void SetCharField(nint obj, nint fieldID, char value);
    [NativeImport(EntryPoint = "jnihelper_set_short_field")]
    public static partial void SetShortField(nint obj, nint fieldID, short value);
    [NativeImport(EntryPoint = "jnihelper_set_int_field")]
    public static partial void SetIntField(nint obj, nint fieldID, int value);
    [NativeImport(EntryPoint = "jnihelper_set_long_field")]
    public static partial void SetLongField(nint obj, nint fieldID, long value);
    [NativeImport(EntryPoint = "jnihelper_set_float_field")]
    public static partial void SetFloatField(nint obj, nint fieldID, float value);
    [NativeImport(EntryPoint = "jnihelper_set_double_field")]
    public static partial void SetDoubleField(nint obj, nint fieldID, double value);

    // ===== 静态字段 Get =====
    [NativeImport(EntryPoint = "jnihelper_get_static_object_field")]
    public static partial nint GetStaticObjectField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_boolean_field")]
    public static partial bool GetStaticBooleanField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_byte_field")]
    public static partial sbyte GetStaticByteField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_char_field")]
    public static partial char GetStaticCharField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_short_field")]
    public static partial short GetStaticShortField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_int_field")]
    public static partial int GetStaticIntField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_long_field")]
    public static partial long GetStaticLongField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_float_field")]
    public static partial float GetStaticFloatField(nint clazz, nint fieldID);
    [NativeImport(EntryPoint = "jnihelper_get_static_double_field")]
    public static partial double GetStaticDoubleField(nint clazz, nint fieldID);

    // ===== 静态字段 Set =====
    [NativeImport(EntryPoint = "jnihelper_set_static_object_field")]
    public static partial void SetStaticObjectField(nint clazz, nint fieldID, nint value);
    [NativeImport(EntryPoint = "jnihelper_set_static_boolean_field")]
    public static partial void SetStaticBooleanField(nint clazz, nint fieldID, bool value);
    [NativeImport(EntryPoint = "jnihelper_set_static_byte_field")]
    public static partial void SetStaticByteField(nint clazz, nint fieldID, sbyte value);
    [NativeImport(EntryPoint = "jnihelper_set_static_char_field")]
    public static partial void SetStaticCharField(nint clazz, nint fieldID, char value);
    [NativeImport(EntryPoint = "jnihelper_set_static_short_field")]
    public static partial void SetStaticShortField(nint clazz, nint fieldID, short value);
    [NativeImport(EntryPoint = "jnihelper_set_static_int_field")]
    public static partial void SetStaticIntField(nint clazz, nint fieldID, int value);
    [NativeImport(EntryPoint = "jnihelper_set_static_long_field")]
    public static partial void SetStaticLongField(nint clazz, nint fieldID, long value);
    [NativeImport(EntryPoint = "jnihelper_set_static_float_field")]
    public static partial void SetStaticFloatField(nint clazz, nint fieldID, float value);
    [NativeImport(EntryPoint = "jnihelper_set_static_double_field")]
    public static partial void SetStaticDoubleField(nint clazz, nint fieldID, double value);

    // ===== 数组 =====
    [NativeImport(EntryPoint = "jnihelper_get_array_length")]
    public static partial int GetArrayLength(nint array);
    [NativeImport(EntryPoint = "jnihelper_get_object_array_element")]
    public static partial nint GetObjectArrayElement(nint array, int index);
    [NativeImport(EntryPoint = "jnihelper_set_object_array_element")]
    public static partial void SetObjectArrayElement(nint array, int index, nint value);

    // ===== Unity 集成 =====
    [NativeImport(EntryPoint = "jnihelper_get_current_activity")]
    public static partial nint GetCurrentActivity();
    [NativeImport(EntryPoint = "jnihelper_get_unity_surface")]
    public static partial nint GetUnitySurface();
    [NativeImport(EntryPoint = "jnihelper_surface_to_native_window")]
    public static partial nint SurfaceToNativeWindow(nint surface);
    [NativeImport(EntryPoint = "jnihelper_keyevent_get_unicode")]
    public static partial uint KeyEventGetUnicode(nint keyEvent);

    // ===== Data 通道 =====
    [NativeImport(EntryPoint = "jnihelper_set_data")]
    public static partial void SetData(string key, nint data, int len);
    [NativeImport(EntryPoint = "jnihelper_get_data_len")]
    public static partial int GetDataLength(string key);
    [NativeImport(EntryPoint = "jnihelper_get_data_buf")]
    public static partial nint GetDataBuffer(string key);
}
