using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class ObservableResultSet<T> : IObservableResultSet<T>
    where T : notnull
{
    private ImmutableDictionary<T, int> _results = ImmutableDictionary<T, int>.Empty;

    public void Update(in KeyValuePair<T, int> valueAndDelta)
    {
        if (_results.TryGetValue(valueAndDelta.Key, out var current))
        {
            var newDelta = current + valueAndDelta.Value;

            if (newDelta == 0)
            {
                _results = _results.Remove(valueAndDelta.Key);
            }
            else
            {
                _results = _results.SetItem(valueAndDelta.Key, newDelta);
            }
        }
        else
        {
            _results = _results.Add(valueAndDelta.Key, valueAndDelta.Value);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _results.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IReadOnlyCollection<T> GetResults()
    {
        // This allocates, we should fix that at some point.
        return _results.Keys.ToFrozenSet();
    }

    public void Update(IEnumerable<KeyValuePair<T, int>> valueAndDelta)
    {
        var newResults = _results.ToBuilder();

        foreach (var (key, delta) in valueAndDelta)
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
                newResults.Add(key, delta);
            }
        }

        _results = newResults.ToImmutable();
    }
}
