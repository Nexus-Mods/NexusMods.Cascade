using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class ObservableResultSet<T> : IObservableResultSet<T>
    where T : notnull
{
    private ImmutableDictionary<T, int> _results = ImmutableDictionary<T, int>.Empty;

    public void Update(Change<T> change)
    {
        if (_results.TryGetValue(change.Value, out var current))
        {
            var newDelta = current + change.Delta;

            if (newDelta == 0)
            {
                _results = _results.Remove(change.Value);
            }
            else
            {
                _results = _results.SetItem(change.Value, newDelta);
            }
        }
        else
        {
            _results = _results.Add(change.Value, change.Delta);
        }
    }

    /// <inheritdoc />
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
        return _results.Keys.ToArray();
    }

    public void Update(ChangeSet<T> valueAndDelta)
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
