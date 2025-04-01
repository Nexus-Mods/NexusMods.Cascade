using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Abstractions;

public ref struct ChangeSetWriter<T> where T : notnull, IComparable<T>
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

    public void Add(ResultSet<T> stateValue)
    {
        foreach (var (key, value) in stateValue.Changes)
            Write(key, value);
    }

    public void Add(T value, int delta)
    {
        Write(value, delta);
    }

    public ChangeSet<T> ToChangeSet()
    {
        return new ChangeSet<T>(AsSpan());
    }

    public ResultSet<T> ToResultSet()
    {
        return new ResultSet<T>(AsSpan());
    }
}
