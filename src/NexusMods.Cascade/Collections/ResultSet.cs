﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Collections;

/// <summary>
/// An immutable on-heap set of values and their deltas.
/// </summary>
public readonly struct ResultSet<T> : IReadOnlyCollection<T>
    where T : notnull, IComparable<T>
{
    private readonly ImmutableDictionary<T, int> _values;

    public ResultSet(ImmutableDictionary<T, int> values)
    {
        _values = values;
    }

    public ResultSet(ReadOnlySpan<Change<T>> initialSet)
    {
        var builder = ImmutableDictionary.CreateBuilder<T, int>();
        foreach (var change in initialSet)
        {
            if (builder.TryGetValue(change.Value, out var value))
            {
                if (value + change.Delta == 0)
                    builder.Remove(change.Value);
                else
                    builder[change.Value] = value + change.Delta;
            }
            else
                builder[change.Value] = change.Delta;
        }

        _values = builder.ToImmutable();
    }

    /// <summary>
    /// Merges the given <see cref="ResultSet{T}"/> into the current <see cref="ResultSet{T}"/>.
    /// </summary>
    public ResultSet<T> Merge(ResultSet<T> other)
    {
        var builder = _values.ToBuilder();
        foreach (var change in other._values)
        {
            if (builder.TryGetValue(change.Key, out var value))
            {
                if (value + change.Value == 0)
                    builder.Remove(change.Key);
                else
                    builder[change.Key] = value + change.Value;
            }
            else
                builder[change.Key] = change.Value;
        }

        return new ResultSet<T>(builder.ToImmutable());
    }

    /// <summary>
    /// Merges the given <see cref="ChangeSet{T}"/> into the current <see cref="ResultSet{T}"/>.
    /// </summary>
    public ResultSet<T> Merge(ChangeSet<T> other)
    {
        var builder = _values.ToBuilder();
        foreach (var (value, delta) in other.Changes)
        {
            if (builder.TryGetValue(value, out var existingDelta))
            {
                if (existingDelta + delta == 0)
                    builder.Remove(value);
                else
                    builder[value] = existingDelta + delta;
            }
            else
                builder[value] = delta;
        }

        return new ResultSet<T>(builder.ToImmutable());
    }

    /// <summary>
    /// Merges the given <see cref="ChangeSet{T}"/> into the current <see cref="ResultSet{T}"/>.
    /// </summary>
    public ResultSet<T> Merge<TIn>(ChangeSet<TIn> other, out List<Change<T>> netChanges) where TIn : IComparable<TIn>
    {
        netChanges = [];
        var builder = _values.ToBuilder();
        foreach (var (value, delta) in other.Changes)
        {
            var castedValue = (T)(object)value!;
            if (builder.TryGetValue(castedValue, out var existingDelta))
            {
                if (existingDelta + delta == 0)
                {
                    netChanges.Add(new Change<T>(castedValue, -1));
                    builder.Remove(castedValue);
                }
                else
                    builder[castedValue] = existingDelta + delta;
            }
            else
            {
                netChanges.Add(new Change<T>(castedValue, 1));
                builder[castedValue] = delta;
            }
        }

        return new ResultSet<T>(builder.ToImmutable());
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var (key, value) in _values)
        {
            yield return key;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// The count all the values in the set.
    /// </summary>
    public int Count => _values.Values.Sum();

    /// <summary>
    /// The count of distinct values in the set.
    /// </summary>
    public int CountDistinct => _values.Count;
}
