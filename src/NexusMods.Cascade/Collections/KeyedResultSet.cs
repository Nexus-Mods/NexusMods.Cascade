using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusMods.Cascade.Collections;

public readonly struct KeyedResultSet<TKey, TValue> : IGrouping<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly ResultSet<TValue> _results;

    public KeyedResultSet(TKey key, ResultSet<TValue> resultSet )
    {
        _results = resultSet;
        Key = key;
    }

    public TKey Key { get; }

    public int Count => _results.Count;

    public IEnumerator<TValue> GetEnumerator()
    {
        foreach (var result in _results)
        {
            yield return result;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
