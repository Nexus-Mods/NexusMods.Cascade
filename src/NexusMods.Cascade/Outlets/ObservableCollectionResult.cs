using NexusMods.Cascade.Abstractions;
using ObservableCollections;

namespace NexusMods.Cascade.Outlets;

/// <summary>
/// A outlet that keeps all the results in an R3 observable collection.
/// </summary>
public sealed class ObservableCollectionResult<T>(UpstreamConnection upstreamConnection) : Outlet<T>(upstreamConnection), IOutletDefinition<T>
    where T : notnull
{
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    public new static IOutletDefinition<T> Create(UpstreamConnection upstreamConnection)
    {
        return new ObservableCollectionResult<T>(upstreamConnection);
    }

    public new class Stage(IFlowImpl flow, ObservableCollectionResult<T> definition) : Outlet<T>.Stage(flow, definition)
    {
        private readonly ObservableDictionary<T, int> _results = new();
        protected override void ReleaseChanges(ChangeSet<T> changeSet)
        {
            lock (_results.SyncRoot)
            {
                foreach (var (item, delta) in changeSet)
                {
                    if (delta > 0)
                    {
                        if (_results.TryGetValue(item, out var count))
                            _results[item] = count + delta;
                        else
                            _results[item] = delta;
                    }
                    else
                    {
                        var count = _results[item];
                        var newCount = count + delta;
                        if (newCount == 0)
                            _results.Remove(item);
                        else
                            _results[item] = newCount;
                    }
                }
            }
        }

        public ObservableDictionary<T, int> Results => _results;
    }

}
