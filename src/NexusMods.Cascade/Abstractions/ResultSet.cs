using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace NexusMods.Cascade.Abstractions;

public class ResultSet<T> : IDiffSet<T>
    where T : notnull
{
    private ImmutableDictionary<T, int> _state;

    public ResultSet()
    {
        _state = ImmutableDictionary<T, int>.Empty;
    }

    private ResultSet(ImmutableDictionary<T, int> state)
    {
        _state = state;
    }

    public ResultSet(DiffSet<T> changeSet)
    {
        var builder = ImmutableDictionary.CreateBuilder<T, int>();
        foreach (var (value, delta) in changeSet)
        {
            if (!builder.TryGetValue(value, out var currentDelta))
            {
                builder[value] = delta;
            }
            else
            {
                var resultDelta = currentDelta + delta;
                if (resultDelta == 0)
                    builder.Remove(value);
                else
                    builder[value] = currentDelta + delta;
            }
        }
        _state = builder.ToImmutable();
    }

    public static ResultSet<T> Empty = new();
    public IEnumerable<T> Values => _state.Keys;

    /// <summary>
    /// Merges the changeset into the current state, duplicate items will have their deltas summed, and
    /// any values resulting in a delta of 0 will be removed.
    /// </summary>
    public ResultSet<T> MergeIn(DiffSet<T> other)
    {
        var builder = ImmutableDictionary.CreateBuilder<T, int>();
        foreach (var (value, delta) in other)
        {
            if (_state.TryGetValue(value, out var currentDelta))
            {
                var newDelta = currentDelta + delta;
                if (newDelta != 0)
                    builder[value] = newDelta;
                else
                    builder.Remove(value);
            }
            else
            {
                builder[value] = delta;
            }
        }
        return new ResultSet<T>(builder.ToImmutable());
    }


    public IEnumerator<Diff<T>> GetEnumerator()
    {
        foreach (var (value, delta) in _state)
        {
            yield return new Diff<T>(value, delta);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IImmutableDictionary<T, int> Dictionary => _state;

}
