using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StArray.ModManager.Analyzer;

/// <summary>
/// 带值相等语义的不可变数组。
///
/// 增量生成器靠模型的 <c>Equals</c> 判断下游要不要重算，而 <see cref="ImmutableArray{T}"/>
/// 比较的是底层数组引用 —— 直接把它放进模型会让缓存永远 miss，
/// <c>IIncrementalGenerator</c> 退化成每次全量重跑。
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(T[]? items) => _items = items;

    public EquatableArray(ImmutableArray<T> items) => _items = items.IsDefault ? null : items.ToArray();

    public static EquatableArray<T> Empty => new(Array.Empty<T>());

    public int Count => _items?.Length ?? 0;

    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_items is null || other._items is null) return _items is null && other._items is null;
        return _items.AsSpan().SequenceEqual(other._items.AsSpan());
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> a && Equals(a);

    public override int GetHashCode()
    {
        if (_items is null) return 0;
        var hash = 17;
        foreach (var item in _items)
            hash = hash * 31 + (item?.GetHashCode() ?? 0);
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator EquatableArray<T>(T[] items) => new(items);
    public static implicit operator EquatableArray<T>(ImmutableArray<T> items) => new(items);
}

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatable<T>(this IEnumerable<T> source) where T : IEquatable<T>
        => new(source.ToArray());
}

/// <summary>参数的类型与名称。用 record struct 以获得值相等。</summary>
internal readonly record struct ParamInfo(string Type, string Name);

/// <summary>特性的具名参数。</summary>
internal readonly record struct NamedArg(string Key, string Value);


