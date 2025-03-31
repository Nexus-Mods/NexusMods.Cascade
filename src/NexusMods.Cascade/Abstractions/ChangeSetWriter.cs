using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace NexusMods.Cascade.Abstractions;

public ref struct ChangeSetWriter<T> where T : notnull
{
    private readonly List<Change<T>> _list;

    public static ChangeSetWriter<T> Create() => new();

    public ChangeSetWriter()
    {
        _list = [];
    }

    public void Write(in T value, int delta)
    {
        _list.Add(new Change<T>(value, delta));
    }

    public void ForwardAll(IStage<T> receivers)
    {
        var changeSet = new ChangeSet<T>(CollectionsMarshal.AsSpan(_list));
        foreach (var (stage, port) in receivers.Outputs)
            stage.AcceptChange(port, changeSet);
    }

    public void Add(int delta, ReadOnlySpan<T> values)
    {
        foreach (var value in values)
            Write(value, delta);
    }

    public ReadOnlySpan<Change<T>> AsSpan()
    {
        return CollectionsMarshal.AsSpan(_list);
    }

    public void Add(ImmutableDictionary<T,int> stateValue)
    {
        foreach (var (key, value) in stateValue)
            Write(key, value);
    }

    public void Add(T value, int delta)
    {
        Write(value, delta);
    }

    public ImmutableDictionary<T,int> ToImmutableDictionary()
    {
        if (_list.Count == 0)
            return ImmutableDictionary<T, int>.Empty;

        var builder = ImmutableDictionary.CreateBuilder<T, int>();
        foreach (var (value, delta) in AsSpan())
        {
            if (builder.TryGetValue(value, out var current))
            {
                if (current + delta == 0)
                    builder.Remove(value);
                else
                    builder[value] = current + delta;
            }
            else
                builder.Add(value, delta);
        }
        return builder.ToImmutable();
    }

    public ChangeSet<T> ToChangeSet()
    {
        return new ChangeSet<T>(AsSpan());
    }
}
