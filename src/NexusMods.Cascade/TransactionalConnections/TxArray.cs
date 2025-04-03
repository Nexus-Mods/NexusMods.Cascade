using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Clarp.Concurrency;

namespace NexusMods.Cascade.TransactionalConnections;

/// <summary>
/// An array that combines STM and an immutable Array to create a transaction aware array.
/// </summary>
public class TxArray<T>() : Ref<ImmutableArray<T>>(ImmutableArray<T>.Empty), IList<T>
{
    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)Value).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public void Add(T item)
    {
        Value = Value.Add(item);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Value = Value.Clear();
    }

    /// <inheritdoc />
    public bool Contains(T item)
    {
        return Value.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        Value.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        Value = Value.Remove(item);
        return true;
    }

    /// <inheritdoc />
    public int Count => Value.Length;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int IndexOf(T item) => Value.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, T item)
        => Value = Value.Insert(index, item);

    public void RemoveAt(int index)
        => Value = Value.RemoveAt(index);

    /// <inheritdoc />
    public T this[int index]
    {
        get => Value[index];
        set => Value = Value.SetItem(index, value);
    }
}
