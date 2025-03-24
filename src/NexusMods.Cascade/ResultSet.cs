using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// Readonly result set from an outlet stage.
/// </summary>
public class ResultSet<T> : IReadOnlyCollection<T>
    where T : notnull
{
    private readonly ImmutableDictionary<T,int> _results;

    public ResultSet(ImmutableDictionary<T, int> results)
    {
        _results = results;
    }

    public ResultSet()
    {
        _results = ImmutableDictionary<T, int>.Empty;
    }

    /// <summary>
    /// Return a new result set with the changes applied.
    /// </summary>
    public ResultSet<T> With(ChangeSet<T> changeSet)
    {
        var newResults = _results.ToBuilder();

        foreach (var (key, delta) in changeSet)
        {
            if (newResults.TryGetValue(key, out var current))
            {
                var newDelta = current + delta;

                if (newDelta == 0)
                {
                    newResults.Remove(key);
                }
                else
                {
                    newResults[key] = newDelta;
                }
            }
            else
            {
                newResults[key] = delta;
            }
        }

        return new ResultSet<T>(newResults.ToImmutable());
    }

    /// <summary>
    /// Returns true if the result set contains the specified item.
    /// </summary>
    public bool Contains(T item) => _results.ContainsKey(item);

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        // Flattens the dictionary into a sequence of keys, and repeats each key according to the delta value.
        foreach (var (key, value) in _results)
        {
            for (var i = 0; i < value; i++)
            {
                yield return key;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// The number of items in the result set, when duplicates are counted.
    /// </summary>
    public int Count => _results.Values.Sum(static x => x);
}
