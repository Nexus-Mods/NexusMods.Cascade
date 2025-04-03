using System;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class ResultSet<T>
{
    private readonly Ref<Diff<T>[]> _value;

    public ResultSet(DiffSet<T> initialValue)
    {
        _value = new(initialValue.AsSpan().ToArray());
    }

    public ResultSet()
    {
        _value = new([]);
    }

    public void Merge(in DiffSet<T> changes)
    {
        var writer = new DiffSetWriter<T>();
        writer.Add(_value.Value);
        writer.Add(changes);

        writer.Build(out var outputSet);
        _value.Value = outputSet.AsSpan().ToArray();
    }

    /// <summary>
    /// Get a span of the current result set.
    /// </summary>
    public ReadOnlySpan<Diff<T>> AsSpan()
    {
        return _value.Value.AsSpan();
    }

    /// <summary>
    /// Get the current result set as a DiffSet.
    /// </summary>
    /// <returns></returns>
    public DiffSet<T> AsDiffSet()
    {
        return new DiffSet<T>(_value.Value);
    }

    public void Reset(DiffSet<T> value)
    {
        _value.Value = value.AsSpan().ToArray();
    }

    public void Clear()
    {
        _value.Value = [];
    }
}
