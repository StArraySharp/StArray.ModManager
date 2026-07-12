namespace StArray.ModManager.Il2Cpp;

/// <summary>UnityEngine.Object</summary>
public class UnityObject : Il2CppObject
{
    public UnityObject(nint ptr) : base(ptr) { }

    public Il2CppString? GetName()
    {
        var k = Il2CppFunctions.il2cpp_class_from_name(
            Il2CppFunctions.il2cpp_assembly_get_image(
                Il2CppFunctions.il2cpp_domain_assembly_open(
                    Il2CppFunctions.il2cpp_domain_get(), 0)),
            "UnityEngine", "Object");
        if (k == 0) return null;
        var m = Il2CppFunctions.il2cpp_class_get_method_from_name(k, "get_name", 0);
        return m != 0 ? GetInvoked<Il2CppString>(m) : null;
    }

    protected static Il2CppClass? ResolveClass(string asm, string ns, string name)
    {
        var domain = Il2CppFunctions.il2cpp_domain_get();
        if (domain == 0) return null;
        var a = Il2CppAssembly.Get(asm);
        return a?.GetClass(ns, name);
    }
}

/// <summary>UnityEngine.Component</summary>
public class Component : UnityObject
{
    public Component(nint ptr) : base(ptr) { }

    public Transform? GetTransform()
        => Call<Transform>("UnityEngine", "Component", "get_transform");

    public GameObject? GetGameObject()
        => Call<GameObject>("UnityEngine", "Component", "get_gameObject");

    protected T? Call<T>(string ns, string cls, string method) where T : Il2CppObject
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", ns, cls);
        var m = k?.GetMethod(method, 0);
        return m != null ? GetInvoked<T>(m.Ptr) : null;
    }
}

/// <summary>UnityEngine.Transform</summary>
public class Transform : Component
{
    public Transform(nint ptr) : base(ptr) { }

    public unsafe Vector3 GetPosition()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Transform");
        var m = k?.GetMethod("get_position_Injected", 0);
        if (m == null) return default;
        Vector3 v = default;
        m.Invoke(Ptr, [(nint)(&v)]);
        return v;
    }

    public unsafe void SetPosition(Vector3 value)
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Transform");
        var m = k?.GetMethod("set_position_Injected", 0);
        Vector3 copy = value;
        m?.Invoke(Ptr, [(nint)(&copy)]);
    }

    public int GetChildCount()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Transform");
        var m = k?.GetMethod("get_childCount", 0);
        return m != null ? (int)m.Invoke(Ptr) : 0;
    }

    public Transform? GetChild(int index)
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Transform");
        var m = k?.GetMethod("GetChild", 1);
        if (m == null) return null;
        var r = m.Invoke(Ptr, [index]);
        return r != 0 ? new Transform(r) : null;
    }
}

/// <summary>UnityEngine.GameObject</summary>
public class GameObject : UnityObject
{
    public GameObject(nint ptr) : base(ptr) { }

    public Transform? GetTransform()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "GameObject");
        var m = k?.GetMethod("get_transform", 0);
        var r = m?.Invoke(Ptr);
        return r != null && r.Value != 0 ? new Transform(r.Value) : null;
    }

    public bool GetActive()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "GameObject");
        var m = k?.GetMethod("get_active", 0);
        return m != null && m.Invoke(Ptr) != 0;
    }

    public void SetActive(bool value)
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "GameObject");
        var m = k?.GetMethod("set_active", "System.Boolean");
        m?.Invoke(Ptr, [value ? 1 : 0]);
    }

    public static GameObject? Find(string name)
    {
        var k = Il2CppAssembly.Get("UnityEngine.CoreModule.dll")?.GetClass("UnityEngine", "GameObject");
        var m = k?.GetMethod("Find", "System.String");
        if (m == null) return null;
        var s = Il2CppString.New(name);
        var r = m.Invoke(0, [s.Ptr]);
        return r != 0 ? new GameObject(r) : null;
    }

    /// <summary>动态挂载脚本：AddComponent(Type)。传入脚本类的 Type 对象指针。</summary>
    public nint AddComponent(nint typeObject)
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "GameObject");
        var m = k?.GetMethod("AddComponent", "System.Type");
        return m?.Invoke(Ptr, [typeObject]) ?? 0;
    }

    /// <summary>按类名动态挂载脚本。</summary>
    public nint AddComponent(string assemblyName, string namespaze, string className)
    {
        var k = Il2CppAssembly.Get(assemblyName)?.GetClass(namespaze, className);
        if (k == null) return 0;
        var typeObj = k.GetTypeObject();
        return typeObj != 0 ? AddComponent(typeObj) : 0;
    }

    /// <summary>按类型获取组件。</summary>
    public T? GetComponent<T>(nint typeObject) where T : Il2CppObject
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "GameObject");
        var m = k?.GetMethod("GetComponent", "System.Type");
        var r = m?.Invoke(Ptr, [typeObject]) ?? 0;
        return r != 0 ? (T)Activator.CreateInstance(typeof(T), r)! : null;
    }
}

/// <summary>UnityEngine.Camera</summary>
public class Camera : Component
{
    public Camera(nint ptr) : base(ptr) { }

    public static Camera? GetMain()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Camera");
        var m = k?.GetMethod("get_main", 0);
        var r = m?.InvokeStatic();
        return r != null && r.Value != 0 ? new Camera(r.Value) : null;
    }

    public float GetFieldOfView()
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Camera");
        var m = k?.GetMethod("get_fieldOfView", 0);
        return m != null ? m.InvokeUnbox<float>(Ptr) : 0f;
    }

    public unsafe void SetFieldOfView(float fov)
    {
        var k = ResolveClass("UnityEngine.CoreModule.dll", "UnityEngine", "Camera");
        var m = k?.GetMethod("set_fieldOfView", "System.Single");
        float copy = fov;
        m?.Invoke(Ptr, [(nint)(&copy)]);
    }
}
