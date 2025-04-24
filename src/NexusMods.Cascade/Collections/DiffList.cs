using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Collections;

public class DiffList<T> : IToDiffSpan<T> where T : notnull
{

    public ReadOnlySpan<Diff<T>> Values => CollectionsMarshal.AsSpan(_values);

    private List<Diff<T>> _values = [];

    public void Add(Diff<T> value) => _values.Add(value);

    public void Add(DiffSet<T> values)
    {
        foreach (var (value, delta) in values)
        {
            _values.Add((value, delta));
        }
    }

    public void AddInverted(DiffSet<T> values)
    {
        foreach (var (value, delta) in values)
        {
            _values.Add((value, -delta));
        }
    }

    public void Add(T value, int delta) => _values.Add(new Diff<T>(value, delta));

    public void Add(ReadOnlySpan<T> values, int delta)
    {
        foreach (var value in values)
        {
            _values.Add(new Diff<T>(value, delta));
        }
    }

    public int Count => _values.Count;

    public void Clear() => _values.Clear();

    public ReadOnlySpan<Diff<T>> ToDiffSpan() => CollectionsMarshal.AsSpan(_values);
}
