using System.Runtime.InteropServices;

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

public static class JniNative
{
    // ===== 引用管理 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_new_global_ref")]
    public static extern nint NewGlobalRef(nint obj);
    [DllImport("modmanager", EntryPoint = "jnihelper_new_local_ref")]
    public static extern nint NewLocalRef(nint obj);
    [DllImport("modmanager", EntryPoint = "jnihelper_delete_local_ref")]
    public static extern void DeleteLocalRef(nint obj);
    [DllImport("modmanager", EntryPoint = "jnihelper_delete_global_ref")]
    public static extern void DeleteGlobalRef(nint obj);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_object_class")]
    public static extern nint GetObjectClass(nint obj);

    // ===== 异常 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_check_exception")]
    public static extern bool CheckException();
    [DllImport("modmanager", EntryPoint = "jnihelper_clear_exception")]
    public static extern void ClearException();

    // ===== 类/方法/字段查找 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_find_class")]
    public static extern nint FindClass(string className);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_method_id")]
    public static extern nint GetMethodID(nint clazz, string methodName, string signature);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_method_id")]
    public static extern nint GetStaticMethodID(nint clazz, string methodName, string signature);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_field_id")]
    public static extern nint GetFieldID(nint clazz, string fieldName, string signature);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_field_id")]
    public static extern nint GetStaticFieldID(nint clazz, string fieldName, string signature);

    // ===== 字符串 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_new_string")]
    public static extern nint NewString(string str);
    [DllImport("modmanager", EntryPoint = "jnihelper_new_string_utf")]
    public static extern nint NewStringUtf(string utf);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_string_utf_chars")]
    public static extern nint GetStringUtfChars(nint jstr);
    [DllImport("modmanager", EntryPoint = "jnihelper_release_string_utf_chars")]
    public static extern void ReleaseStringUtfChars(nint jstr, nint utf);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_string_length")]
    public static extern int GetStringLength(nint jstr);

    // ===== 实例方法调用 (A 变体) =====
    [DllImport("modmanager", EntryPoint = "jnihelper_call_object_method_a")]
    public static extern nint CallObjectMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_boolean_method_a")]
    public static extern bool CallBooleanMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_byte_method_a")]
    public static extern sbyte CallByteMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_char_method_a")]
    public static extern char CallCharMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_short_method_a")]
    public static extern short CallShortMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_int_method_a")]
    public static extern int CallIntMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_long_method_a")]
    public static extern long CallLongMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_float_method_a")]
    public static extern float CallFloatMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_double_method_a")]
    public static extern double CallDoubleMethodA(nint obj, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_void_method_a")]
    public static extern void CallVoidMethodA(nint obj, nint methodID, nint args);

    // ===== 实例方法调用 (无参) =====
    [DllImport("modmanager", EntryPoint = "jnihelper_call_object_method")]
    public static extern nint CallObjectMethod(nint obj, nint methodID);

    // ===== 静态方法调用 (A 变体) =====
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_object_method_a")]
    public static extern nint CallStaticObjectMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_boolean_method_a")]
    public static extern bool CallStaticBooleanMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_byte_method_a")]
    public static extern sbyte CallStaticByteMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_char_method_a")]
    public static extern char CallStaticCharMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_short_method_a")]
    public static extern short CallStaticShortMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_int_method_a")]
    public static extern int CallStaticIntMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_long_method_a")]
    public static extern long CallStaticLongMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_float_method_a")]
    public static extern float CallStaticFloatMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_double_method_a")]
    public static extern double CallStaticDoubleMethodA(nint clazz, nint methodID, nint args);
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_void_method_a")]
    public static extern void CallStaticVoidMethodA(nint clazz, nint methodID, nint args);

    // ===== 静态方法调用 (无参) =====
    [DllImport("modmanager", EntryPoint = "jnihelper_call_static_object_method")]
    public static extern nint CallStaticObjectMethod(nint clazz, nint methodID);

    // ===== 实例字段 Get =====
    [DllImport("modmanager", EntryPoint = "jnihelper_get_object_field")]
    public static extern nint GetObjectField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_boolean_field")]
    public static extern bool GetBooleanField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_byte_field")]
    public static extern sbyte GetByteField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_char_field")]
    public static extern char GetCharField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_short_field")]
    public static extern short GetShortField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_int_field")]
    public static extern int GetIntField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_long_field")]
    public static extern long GetLongField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_float_field")]
    public static extern float GetFloatField(nint obj, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_double_field")]
    public static extern double GetDoubleField(nint obj, nint fieldID);

    // ===== 实例字段 Set =====
    [DllImport("modmanager", EntryPoint = "jnihelper_set_object_field")]
    public static extern void SetObjectField(nint obj, nint fieldID, nint value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_boolean_field")]
    public static extern void SetBooleanField(nint obj, nint fieldID, bool value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_byte_field")]
    public static extern void SetByteField(nint obj, nint fieldID, sbyte value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_char_field")]
    public static extern void SetCharField(nint obj, nint fieldID, char value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_short_field")]
    public static extern void SetShortField(nint obj, nint fieldID, short value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_int_field")]
    public static extern void SetIntField(nint obj, nint fieldID, int value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_long_field")]
    public static extern void SetLongField(nint obj, nint fieldID, long value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_float_field")]
    public static extern void SetFloatField(nint obj, nint fieldID, float value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_double_field")]
    public static extern void SetDoubleField(nint obj, nint fieldID, double value);

    // ===== 静态字段 Get =====
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_object_field")]
    public static extern nint GetStaticObjectField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_boolean_field")]
    public static extern bool GetStaticBooleanField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_byte_field")]
    public static extern sbyte GetStaticByteField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_char_field")]
    public static extern char GetStaticCharField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_short_field")]
    public static extern short GetStaticShortField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_int_field")]
    public static extern int GetStaticIntField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_long_field")]
    public static extern long GetStaticLongField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_float_field")]
    public static extern float GetStaticFloatField(nint clazz, nint fieldID);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_static_double_field")]
    public static extern double GetStaticDoubleField(nint clazz, nint fieldID);

    // ===== 静态字段 Set =====
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_object_field")]
    public static extern void SetStaticObjectField(nint clazz, nint fieldID, nint value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_boolean_field")]
    public static extern void SetStaticBooleanField(nint clazz, nint fieldID, bool value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_byte_field")]
    public static extern void SetStaticByteField(nint clazz, nint fieldID, sbyte value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_char_field")]
    public static extern void SetStaticCharField(nint clazz, nint fieldID, char value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_short_field")]
    public static extern void SetStaticShortField(nint clazz, nint fieldID, short value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_int_field")]
    public static extern void SetStaticIntField(nint clazz, nint fieldID, int value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_long_field")]
    public static extern void SetStaticLongField(nint clazz, nint fieldID, long value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_float_field")]
    public static extern void SetStaticFloatField(nint clazz, nint fieldID, float value);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_static_double_field")]
    public static extern void SetStaticDoubleField(nint clazz, nint fieldID, double value);

    // ===== 数组 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_get_array_length")]
    public static extern int GetArrayLength(nint array);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_object_array_element")]
    public static extern nint GetObjectArrayElement(nint array, int index);
    [DllImport("modmanager", EntryPoint = "jnihelper_set_object_array_element")]
    public static extern void SetObjectArrayElement(nint array, int index, nint value);

    // ===== Unity 集成 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_get_current_activity")]
    public static extern nint GetCurrentActivity();
    [DllImport("modmanager", EntryPoint = "jnihelper_get_unity_surface")]
    public static extern nint GetUnitySurface();
    [DllImport("modmanager", EntryPoint = "jnihelper_surface_to_native_window")]
    public static extern nint SurfaceToNativeWindow(nint surface);
    [DllImport("modmanager", EntryPoint = "jnihelper_keyevent_get_unicode")]
    public static extern uint KeyEventGetUnicode(nint keyEvent);

    // ===== Data 通道 =====
    [DllImport("modmanager", EntryPoint = "jnihelper_set_data")]
    public static extern void SetData(string key, nint data, int len);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_data_len")]
    public static extern int GetDataLength(string key);
    [DllImport("modmanager", EntryPoint = "jnihelper_get_data_buf")]
    public static extern nint GetDataBuffer(string key);
}
