using System.Runtime.InteropServices;
using StArray.ModManager.Manager;

namespace StArray.ModManager.Android.Native;

public sealed partial class JavaClass : IDisposable
{
    public readonly IntPtr Handle;
    private bool _disposed;

    public JavaClass(string className)
    {
        var localRef = FindViaClassLoader(className);
        if (localRef == IntPtr.Zero)
            throw new Exception($"JavaClass: '{className}' not found");
        Handle = JniNative.NewGlobalRef(localRef);
        JniNative.DeleteLocalRef(localRef);
    }

    public JavaClass(IntPtr clazz)
    {
        Handle = JniNative.NewGlobalRef(clazz);
    }

    public IntPtr GetMethodID(string name, string sig)
        => JniNative.GetMethodID(Handle, name, sig);

    public IntPtr GetStaticMethodID(string name, string sig)
        => JniNative.GetStaticMethodID(Handle, name, sig);

    public IntPtr GetFieldID(string name, string sig)
        => JniNative.GetFieldID(Handle, name, sig);

    public IntPtr GetStaticFieldID(string name, string sig)
        => JniNative.GetStaticFieldID(Handle, name, sig);

    public IntPtr GetStaticObjectField(IntPtr fieldID)
        => JniNative.GetStaticObjectField(Handle, fieldID);

    public IntPtr CallStaticObjectMethod0(nint m)
        => JniNative.CallStaticObjectMethod(Handle, m);

    public IntPtr CallStaticObjectMethod1(nint m, nint a1)
    {
        JValue[] args = [new() { L = a1 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallStaticObjectMethodA(Handle, m, (nint)p);
        }
    }

    public IntPtr CallStaticObjectMethod2(nint m, nint a1, nint a2)
    {
        JValue[] args = [new() { L = a1 }, new() { L = a2 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallStaticObjectMethodA(Handle, m, (nint)p);
        }
    }

    public IntPtr CallStaticObjectMethod3(nint m, nint a1, nint a2, nint a3)
    {
        JValue[] args = [new() { L = a1 }, new() { L = a2 }, new() { L = a3 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallStaticObjectMethodA(Handle, m, (nint)p);
        }
    }

    public void CallStaticVoidMethod0(nint m)
        => JniNative.CallStaticVoidMethodA(Handle, m, IntPtr.Zero);

    public void CallStaticVoidMethod1(nint m, nint a1)
    {
        JValue[] args = [new() { L = a1 }];
        unsafe
        {
            fixed (JValue* p = args)
                JniNative.CallStaticVoidMethodA(Handle, m, (nint)p);
        }
    }

    public int CallStaticIntMethod0(nint m)
        => JniNative.CallStaticIntMethodA(Handle, m, IntPtr.Zero);

    public nint NewString(string s) => JniNative.NewString(s);

    public void Dispose()
    {
        if (!_disposed) { JniNative.DeleteGlobalRef(Handle); _disposed = true; }
    }

    private static IntPtr FindViaClassLoader(string name)
    {
        var atClass = JniNative.FindClass("android/app/ActivityThread");
        if (atClass == IntPtr.Zero) return IntPtr.Zero;
        var curApp = JniNative.GetStaticMethodID(atClass, "currentApplication", "()Landroid/app/Application;");
        var app = JniNative.CallStaticObjectMethod(atClass, curApp);
        JniNative.DeleteGlobalRef(atClass);
        Logger.Error("CL", "GetStaticMethodID currentApplication");
        var appCls = JniNative.GetObjectClass(app);
        var getCl = JniNative.GetMethodID(appCls, "getClassLoader", "()Ljava/lang/ClassLoader;");
        var cl = JniNative.CallObjectMethod(app, getCl);
        var clCls = JniNative.GetObjectClass(cl);
        Logger.Error("CL", "GetMethodID getClassLoader");
        var loadClass = JniNative.GetMethodID(clCls, "loadClass", "(Ljava/lang/String;)Ljava/lang/Class;");
        var jName = JniNative.NewString(name);
        Logger.Error("CL", "NewString");
        var result = new JavaObject(cl).CallObjectMethod1(loadClass, jName);
        JniNative.DeleteLocalRef(jName);
        return result;
    }
}

public sealed class JavaObject : IDisposable
{
    public readonly IntPtr Handle;
    private bool _disposed;

    public JavaObject(IntPtr obj) => Handle = obj;

    public IntPtr CallObjectMethod0(IntPtr m)
        => JniNative.CallObjectMethod(Handle, m);

    public IntPtr CallObjectMethod1(IntPtr m, IntPtr a1)
    {
        JValue[] args = [new() { L = a1 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallObjectMethodA(Handle, m, (nint)p);
        }
    }

    public IntPtr CallObjectMethod2(IntPtr m, IntPtr a1, IntPtr a2)
    {
        JValue[] args = [new() { L = a1 }, new() { L = a2 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallObjectMethodA(Handle, m, (nint)p);
        }
    }

    public void CallVoidMethod0(IntPtr m)
        => JniNative.CallVoidMethodA(Handle, m, IntPtr.Zero);

    public void CallVoidMethod1(IntPtr m, IntPtr a1)
    {
        JValue[] args = [new() { L = a1 }];
        unsafe
        {
            fixed (JValue* p = args)
                JniNative.CallVoidMethodA(Handle, m, (nint)p);
        }
    }

    public void CallVoidMethod2(IntPtr m, IntPtr a1, IntPtr a2)
    {
        JValue[] args = [new() { L = a1 }, new() { L = a2 }];
        unsafe
        {
            fixed (JValue* p = args)
                JniNative.CallVoidMethodA(Handle, m, (nint)p);
        }
    }

    public bool CallBoolMethod2(IntPtr m, IntPtr a1, IntPtr a2)
    {
        JValue[] args = [new() { L = a1 }, new() { L = a2 }];
        unsafe
        {
            fixed (JValue* p = args)
                return JniNative.CallBooleanMethodA(Handle, m, (nint)p);
        }
    }

    public IntPtr GetObjectField(IntPtr fieldID)
        => JniNative.GetObjectField(Handle, fieldID);

    public JavaClass GetClass() => new(JniNative.GetObjectClass(Handle));

    public void Dispose()
    {
        if (!_disposed) { JniNative.DeleteLocalRef(Handle); _disposed = true; }
    }
}

public static class NativeFunctions
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptChar(uint codepoint);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptCharCallback")]
    public static extern void SetOnAcceptCharCallback(OnAcceptChar onAcceptChar);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void OnAcceptKey(int keyCode);

    [DllImport("modmanager", EntryPoint = "modmanager_set_OnAcceptKeyCallback")]
    public static extern void SetOnAcceptKeyCallback(OnAcceptKey onAcceptKey);
}
