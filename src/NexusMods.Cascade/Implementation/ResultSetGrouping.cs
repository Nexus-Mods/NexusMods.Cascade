using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public class ResultSetGrouping<TKey, TValue> : IGrouping<TKey, TValue>, IEquatable<ResultSetGrouping<TKey, TValue>>
    where TValue : notnull
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
    public bool Equals(ResultSetGrouping<TKey, TValue>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!this.Key!.Equals(other.Key)) return false;

        if (this._resultSet.Dictionary.Count != other._resultSet.Dictionary.Count)
            return false;

        return _resultSet.Dictionary.Equals(other._resultSet.Dictionary);

    }
}
