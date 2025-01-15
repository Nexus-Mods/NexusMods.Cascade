using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NexusMods.Cascade;

/// <summary>
/// Readonly result set from an outlet stage.
/// </summary>
public sealed class ResultSet<T> : IReadOnlyCollection<T>
    where T : notnull
{
    private readonly ImmutableDictionary<T,int> _results;

    public ResultSet(ImmutableDictionary<T, int> results)
    {
        _results = results;
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
