using System.Runtime.InteropServices;

namespace StArray.ModManager.Native;

/// <summary>
/// Java 类封装 — 持有一个 jclass 全局引用，提供方法/字段查找和静态方法调用
/// </summary>
public sealed class JavaClass : IDisposable
{
    public readonly IntPtr Handle; // jclass global ref
    private bool _disposed;

    /// <summary>通过 ClassLoader.loadClass 查找类</summary>
    public JavaClass(string className)
    {
        var env = Env();
        var localRef = FindViaClassLoader(env, className);
        if (localRef == IntPtr.Zero)
            throw new Exception($"JavaClass: '{className}' not found");
        Handle = NewGlobalRef(env, localRef);
        JniHelperNative.DeleteLocalRef(localRef);
    }

    /// <summary>包装已有的 jclass（自动转全局引用）</summary>
    public JavaClass(IntPtr clazz)
    {
        Handle = NewGlobalRef(Env(), clazz);
    }


    public IntPtr GetMethodID(string name, string sig)
        => JniHelperNative.GetMethodID(Handle, name, sig);

    public IntPtr GetStaticMethodID(string name, string sig)
        => JniHelperNative.GetStaticMethodID(Handle, name, sig);


    public IntPtr GetFieldID(string name, string sig)
        => JniHelperNative.GetFieldID(Handle, name, sig);

    public IntPtr GetStaticFieldID(string name, string sig)
        => JniHelperNative.GetStaticFieldID(Handle, name, sig);

    public IntPtr GetStaticObjectField(IntPtr fieldID)
        => JniHelperNative.GetStaticObjectField(Handle, fieldID);


    public IntPtr CallStaticObjectMethod0(nint m)
        => VTable<Obj0>(Env(), 111)(Env(), Handle, m);

    public IntPtr CallStaticObjectMethod1(nint m, nint a1)
        => VTable<Obj1>(Env(), 111)(Env(), Handle, m, a1);

    public IntPtr CallStaticObjectMethod2(nint m, nint a1, nint a2)
        => VTable<Obj2>(Env(), 111)(Env(), Handle, m, a1, a2);

    public IntPtr CallStaticObjectMethod3(nint m, nint a1, nint a2, nint a3)
        => VTable<Obj3>(Env(), 111)(Env(), Handle, m, a1, a2, a3);

    public void CallStaticVoidMethod0(nint m)
        => VTable<Void0>(Env(), 120)(Env(), Handle, m);

    public void CallStaticVoidMethod1(nint m, nint a1)
        => VTable<Void1>(Env(), 120)(Env(), Handle, m, a1);

    public int CallStaticIntMethod0(nint m)
    {
        // arm64: delegate 返回 nint 再截低 32 位，避免 int 返回值 marshalling 损坏
        nint v = VTable<IntRet>(Env(), 116)(Env(), Handle, m);
        return (int)(v & 0xFFFFFFFFL);
    }

    /// <summary>创建 Java String</summary>
    public nint NewString(string s) => JniHelperNative.NewString(s);

    public void Dispose()
    {
        if (!_disposed) { JniHelperNative.DeleteGlobalRef(Handle); _disposed = true; }
    }


    private static nint Env() => JniHelperNative.GetJNIEnv();


    private static IntPtr FindViaClassLoader(IntPtr env, string name)
    {
        var atClass = JniHelperNative.FindClass("android/app/ActivityThread");
        if (atClass == IntPtr.Zero) return IntPtr.Zero;
        var curAt = JniHelperNative.GetStaticMethodID(atClass, "currentActivityThread", "()Landroid/app/ActivityThread;");
        var at = VTable<Obj0>(env, 34)(env, atClass, curAt);
        var getApp = JniHelperNative.GetMethodID(atClass, "getApplication", "()Landroid/app/Application;");
        var app = VTable<Obj0>(env, 34)(env, at, getApp);
        var appCls = JniHelperNative.GetObjectClass(app);
        var getCl = JniHelperNative.GetMethodID(appCls, "getClassLoader", "()Ljava/lang/ClassLoader;");
        var cl = VTable<Obj0>(env, 34)(env, app, getCl);
        var clCls = JniHelperNative.GetObjectClass(cl);
        var loadClass = JniHelperNative.GetMethodID(clCls, "loadClass", "(Ljava/lang/String;)Ljava/lang/Class;");
        var jName = JniHelperNative.NewString(name);
        var result = VTable<Obj1>(env, 34)(env, cl, loadClass, jName);
        JniHelperNative.DeleteLocalRef(jName);
        return result;
    }


    private static IntPtr NewGlobalRef(IntPtr env, IntPtr obj)
        => VTable<ObjRef2>(env, 21)(env, obj);

    private static T VTable<T>(IntPtr env, int idx) where T : Delegate
    {
        var tbl = Marshal.ReadIntPtr(env);
        return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(tbl + idx * IntPtr.Size));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr ObjRef2(IntPtr e, IntPtr o);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj0(IntPtr e, IntPtr o, IntPtr m);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj1(IntPtr e, IntPtr o, IntPtr m, nint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj2(IntPtr e, IntPtr o, IntPtr m, nint a1, nint a2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj3(IntPtr e, IntPtr o, IntPtr m, nint a1, nint a2, nint a3);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Void0(IntPtr e, IntPtr o, IntPtr m);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Void1(IntPtr e, IntPtr o, IntPtr m, nint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate nint IntRet(IntPtr e, IntPtr o, IntPtr m);
}

/// <summary>
/// Java 对象封装 — 持有一个 jobject 本地引用，提供实例方法调用和字段读取
/// </summary>
public sealed class JavaObject : IDisposable
{
    public readonly IntPtr Handle;
    private bool _disposed;

    public JavaObject(IntPtr obj) => Handle = obj;


    public IntPtr CallObjectMethod0(IntPtr m)
        => VTable<Obj0>(Env(), 34)(Env(), Handle, m);

    public IntPtr CallObjectMethod1(IntPtr m, IntPtr a1)
        => VTable<Obj1>(Env(), 34)(Env(), Handle, m, a1);

    public IntPtr CallObjectMethod2(IntPtr m, IntPtr a1, IntPtr a2)
        => VTable<Obj2>(Env(), 34)(Env(), Handle, m, a1, a2);

    public void CallVoidMethod0(IntPtr m)
        => VTable<Void0>(Env(), 45)(Env(), Handle, m);

    public void CallVoidMethod1(IntPtr m, IntPtr a1)
        => VTable<Void1>(Env(), 45)(Env(), Handle, m, a1);

    public void CallVoidMethod2(IntPtr m, IntPtr a1, IntPtr a2)
        => VTable<Void2>(Env(), 45)(Env(), Handle, m, a1, a2);

    public bool CallBoolMethod2(IntPtr m, IntPtr a1, IntPtr a2)
        => VTable<Bool2>(Env(), 37)(Env(), Handle, m, a1, a2) != 0;


    public IntPtr GetObjectField(IntPtr fieldID)
        => JniHelperNative.GetObjectField(Handle, fieldID);


    public JavaClass GetClass() => new(JniHelperNative.GetObjectClass(Handle));

    public void Dispose()
    {
        if (!_disposed) { JniHelperNative.DeleteLocalRef(Handle); _disposed = true; }
    }


    private static IntPtr Env() => JniHelperNative.GetJNIEnv();

    private static T VTable<T>(IntPtr env, int idx) where T : Delegate
    {
        var tbl = Marshal.ReadIntPtr(env);
        return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(tbl + idx * IntPtr.Size));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj0(IntPtr e, IntPtr o, IntPtr m);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj1(IntPtr e, IntPtr o, IntPtr m, nint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Obj2(IntPtr e, IntPtr o, IntPtr m, nint a1, nint a2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Void0(IntPtr e, IntPtr o, IntPtr m);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Void1(IntPtr e, IntPtr o, IntPtr m, nint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Void2(IntPtr e, IntPtr o, IntPtr m, nint a1, nint a2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate byte Bool2(IntPtr e, IntPtr o, IntPtr m, nint a1, nint a2);
}
public static class NativeFunctions {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptChar(uint codepoint);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptCharCallback")]
    public static extern void SetOnAcceptCharCallback(OnAcceptChar onAcceptChar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptKey(int keyCode);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptKeyCallback")]
    public static extern void SetOnAcceptKeyCallback(OnAcceptKey onAcceptKey);
}