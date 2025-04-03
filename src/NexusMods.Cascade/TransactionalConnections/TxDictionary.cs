using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Clarp.Concurrency;

namespace NexusMods.Cascade.TransactionalConnections;

/// <summary>
/// A dictionary that can be used in a transactional connection, internally it's a <see cref="ImmutableDictionary{TKey,TValue}"/> coupled
/// with a <see cref="Ref{T}"/> to allow for atomic updates.
/// </summary>
public class TxDictionary<TKey, TValue>() : Ref<ImmutableDictionary<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.Empty),
    IDictionary<TKey, TValue> where TKey : notnull
{
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<TKey, TValue>>)Value).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Value = Value.Add(item.Key, item.Value);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Value = Value.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return Value.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)Value).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var newVal = Value.Remove(item.Key);
        Value = newVal;
        return !ReferenceEquals(newVal, Value);
    }

    /// <inheritdoc />
    public int Count => Value.Count;

    /// <inheritdoc />
    public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)Value).IsReadOnly;

    /// <inheritdoc />
    public void Add(TKey key, TValue value) => Value = Value.Add(key, value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => Value.ContainsKey(key);

    /// <inheritdoc />
    public bool Remove(TKey key)
    {
        var newVal = Value.Remove(key);
        Value = newVal;
        return !ReferenceEquals(newVal, Value);
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Value.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get => Value[key];
        set => Value = Value.SetItem(key, value);
    }

    /// <inheritdoc />
    public ICollection<TKey> Keys => (ICollection<TKey>)Value.Keys;

    /// <inheritdoc />
    public ICollection<TValue> Values => (ICollection<TValue>)Value.Values;
}
