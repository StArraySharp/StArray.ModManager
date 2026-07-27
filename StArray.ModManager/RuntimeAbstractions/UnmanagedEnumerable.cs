using System.Collections;
using System.Collections.Generic;
using StArray.ModManager.Il2Cpp;
using StArray.ModManager.Mono;

namespace StArray.ModManager.RuntimeAbstractions;

/// <summary>
/// Wraps a managed <c>IEnumerable</c> collection (List&lt;T&gt;, Queue, Stack, HashSet, etc.)
/// from Il2Cpp or Mono, exposing it as an enumerable sequence of <see cref="RuntimeObject"/>.
/// </summary>
public unsafe class UnmanagedEnumerable : IEnumerable<RuntimeObject>
{
    private readonly nint _ptr;

    public nint Ptr => _ptr;
    public bool IsValid => _ptr != 0;

    public UnmanagedEnumerable(nint ptr) => _ptr = ptr;
    public UnmanagedEnumerable(RuntimeObject obj) => _ptr = obj.Ptr;

    public static implicit operator UnmanagedEnumerable(nint ptr) => new(ptr);
    public static implicit operator UnmanagedEnumerable(RuntimeObject obj) => new(obj);
    public static implicit operator nint(UnmanagedEnumerable e) => e._ptr;

    public int Count
    {
        get
        {
            var ret = new RuntimeObject(_ptr).Invoke("get_Count", 0);
            if (ret == 0) return 0;
            if (RuntimeManager.IsIl2Cpp) return *(int*)Il2CppFunctions.il2cpp_object_unbox(ret);
            if (RuntimeManager.IsMono) return *(int*)MonoFunctions.MonoObjectUnbox(ret);
            return 0;
        }
    }

    public Enumerator GetEnumerator() => new(_ptr);

    IEnumerator<RuntimeObject> IEnumerable<RuntimeObject>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public RuntimeObject?[] ToArray()
    {
        var list = new List<RuntimeObject?>();
        foreach (var item in this)
            list.Add(item.IsValid ? item : null);
        return list.ToArray();
    }

    public sealed class Enumerator : IEnumerator<RuntimeObject>
    {
        private readonly RuntimeObject _collection;
        private RuntimeObject _enumerator;
        private RuntimeObject _current;
        private bool _started;

        internal Enumerator(nint ptr)
        {
            _collection = new RuntimeObject(ptr);
            _enumerator = default;
            _current = default;
            _started = false;
        }

        public RuntimeObject Current => _current;
        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            return _started ? MoveNextInner() : MoveFirst();
        }

        private bool MoveFirst()
        {
            _enumerator = _collection.InvokeObject("GetEnumerator", 0) ?? default;
            if (!_enumerator.IsValid)
                return false;
            _started = true;
            return MoveNextInner();
        }

        private bool MoveNextInner()
        {
            var ret = _enumerator.Invoke("MoveNext", 0);
            if (ret == 0) return false;

            bool hasNext;
            if (RuntimeManager.IsIl2Cpp)
                hasNext = *(bool*)Il2CppFunctions.il2cpp_object_unbox(ret);
            else if (RuntimeManager.IsMono)
                hasNext = *(bool*)MonoFunctions.MonoObjectUnbox(ret);
            else
                return false;

            if (!hasNext)
            {
                _current = default;
                return false;
            }

            _current = _enumerator.InvokeObject("get_Current", 0) ?? default;
            return true;
        }

        public void Reset()
        {
            _enumerator = default;
            _current = default;
            _started = false;
        }

        public void Dispose()
        {
            _enumerator = default;
            _current = default;
        }
    }
}

/// <summary>
/// Typed variant — wraps an <c>IEnumerable&lt;T&gt;</c> and returns <typeparamref name="T"/> instances.
/// <typeparamref name="T"/> must inherit <see cref="UnmanagedObject"/> and expose a <c>T(nint ptr)</c> constructor.
/// </summary>
public unsafe class UnmanagedEnumerable<T> : IEnumerable<T> where T : UnmanagedObject
{
    private readonly nint _ptr;

    public nint Ptr => _ptr;
    public bool IsValid => _ptr != 0;

    public UnmanagedEnumerable(nint ptr) => _ptr = ptr;
    public UnmanagedEnumerable(RuntimeObject obj) => _ptr = obj.Ptr;
    public UnmanagedEnumerable(UnmanagedEnumerable other) => _ptr = other.Ptr;

    public static implicit operator UnmanagedEnumerable<T>(nint ptr) => new(ptr);
    public static implicit operator UnmanagedEnumerable<T>(RuntimeObject obj) => new(obj);
    public static implicit operator UnmanagedEnumerable<T>(UnmanagedEnumerable other) => new(other);
    public static implicit operator nint(UnmanagedEnumerable<T> e) => e._ptr;
    public static implicit operator UnmanagedEnumerable(UnmanagedEnumerable<T> e) => new(e._ptr);

    public int Count
    {
        get
        {
            var ret = new RuntimeObject(_ptr).Invoke("get_Count", 0);
            if (ret == 0) return 0;
            if (RuntimeManager.IsIl2Cpp) return *(int*)Il2CppFunctions.il2cpp_object_unbox(ret);
            if (RuntimeManager.IsMono) return *(int*)MonoFunctions.MonoObjectUnbox(ret);
            return 0;
        }
    }

    public Enumerator GetEnumerator() => new(_ptr);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T?[] ToArray()
    {
        var list = new List<T?>();
        foreach (var item in new UnmanagedEnumerable(_ptr))
            list.Add(item.IsValid ? (T?)Activator.CreateInstance(typeof(T), item.Ptr) : null);
        return list.ToArray();
    }

    public sealed class Enumerator : IEnumerator<T>
    {
        private readonly RuntimeObject _collection;
        private RuntimeObject _enumerator;
        private RuntimeObject _current;
        private bool _started;

        internal Enumerator(nint ptr)
        {
            _collection = new RuntimeObject(ptr);
            _enumerator = default;
            _current = default;
            _started = false;
        }

        public T Current => (T)Activator.CreateInstance(typeof(T), _current.Ptr)!;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _started ? MoveNextInner() : MoveFirst();
        }

        private bool MoveFirst()
        {
            _enumerator = _collection.InvokeObject("GetEnumerator", 0) ?? default;
            if (!_enumerator.IsValid)
                return false;
            _started = true;
            return MoveNextInner();
        }

        private bool MoveNextInner()
        {
            var ret = _enumerator.Invoke("MoveNext", 0);
            if (ret == 0) return false;

            bool hasNext;
            if (RuntimeManager.IsIl2Cpp)
                hasNext = *(bool*)Il2CppFunctions.il2cpp_object_unbox(ret);
            else if (RuntimeManager.IsMono)
                hasNext = *(bool*)MonoFunctions.MonoObjectUnbox(ret);
            else
                return false;

            if (!hasNext)
            {
                _current = default;
                return false;
            }

            _current = _enumerator.InvokeObject("get_Current", 0) ?? default;
            return true;
        }

        public void Reset()
        {
            _enumerator = default;
            _current = default;
            _started = false;
        }

        public void Dispose()
        {
            _enumerator = default;
            _current = default;
        }
    }
}
