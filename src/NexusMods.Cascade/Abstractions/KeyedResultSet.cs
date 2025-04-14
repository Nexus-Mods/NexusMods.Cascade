using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

public class KeyedResultSet<TKey, TValue>
    where TValue : notnull
    where TKey : notnull
{
    private readonly ImmutableDictionary<TKey, ResultSet<TValue>> _state = ImmutableDictionary<TKey, ResultSet<TValue>>.Empty;

    public KeyedResultSet()
    {
    }

    private KeyedResultSet(ImmutableDictionary<TKey, ResultSet<TValue>> state)
    {
        _state = state;
    }

    public KeyedResultSet<TKey, TValue> Add(TKey key, TValue value, int delta)
    {
        if (!_state.TryGetValue(key, out var resultSet))
        {
            resultSet = new ResultSet<TValue>();
            resultSet = resultSet.Add(value, delta);
            return new KeyedResultSet<TKey, TValue>(_state.Add(key, resultSet));
        }
        else
        {
            resultSet = resultSet.Add(value, delta);
            if (resultSet.IsEmpty)
            {
                return new KeyedResultSet<TKey, TValue>(_state.Remove(key));
            }
            return new KeyedResultSet<TKey, TValue>(_state.SetItem(key, resultSet));
        }
    }

    public KeyedResultSet<TKey, TValue> Add(TKey key, TValue value, int delta, out OpResult opResult)
    {
        if (!_state.TryGetValue(key, out var resultSet))
        {
            resultSet = new ResultSet<TValue>();
            resultSet = resultSet.Add(value, delta);
            opResult = OpResult.Added;
            return new KeyedResultSet<TKey, TValue>(_state.Add(key, resultSet));
        }
        else
        {
            resultSet = resultSet.Add(value, delta, out opResult);
            if (resultSet.IsEmpty)
            {
                return new KeyedResultSet<TKey, TValue>(_state.Remove(key));
            }
            return new KeyedResultSet<TKey, TValue>(_state.SetItem(key, resultSet));
        }
    }

    public ResultSet<TValue> this[TKey leftKey]
    {
        get
        {
            if (_state.TryGetValue(leftKey, out var resultSet))
            {
                return resultSet;
            }
            return ResultSet<TValue>.Empty;
        }
    }

    public IEnumerator<KeyValuePair<TKey, ResultSet<TValue>>> GetEnumerator()
    {
        return _state.GetEnumerator();
    }
}
