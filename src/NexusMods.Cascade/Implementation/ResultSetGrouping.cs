using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public class ResultSetGrouping<TKey, TValue> : IGrouping<TKey, TValue> where TValue : notnull
{
    private readonly ResultSet<TValue> _resultSet;

    public ResultSetGrouping(TKey key, ResultSet<TValue> value)
    {
        Key = key;
        _resultSet = value;
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        return _resultSet.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TKey Key { get; }
}
