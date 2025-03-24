using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;
using ObservableCollections;

namespace NexusMods.Cascade.Outlets;

public sealed class ActiveRecordObservableCollectionResult<TKey, TActive, TBase>(UpstreamConnection upstreamConnection) : Outlet<TBase>(upstreamConnection), IOutletDefinition<TBase>
    where TBase : IRowDefinition<TKey>
    where TActive : IActiveRow<TBase, TKey>
    where TKey : notnull
{
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    public new static IOutletDefinition<TBase> Create(UpstreamConnection upstreamConnection)
    {
        return new ActiveRecordObservableCollectionResult<TKey, TActive, TBase>(upstreamConnection);
    }

    public new class Stage(IFlowImpl flow,ActiveRecordObservableCollectionResult<TKey, TActive, TBase> definition) : Outlet<TBase>.Stage(flow, definition)
    {
        private readonly ObservableDictionary<TKey, TActive> _results = new();
        private readonly Dictionary<TKey, int> _changes = new();

        protected override void ReleaseChanges(ChangeSet<TBase> changeSet)
        {
            lock (_results.SyncRoot)
            {
                foreach (var (item, delta) in changeSet)
                {
                    ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_changes, item.RowId, out var exists);
                    found += delta;

                    var id = item.RowId;
                    if (delta < 0)
                    {
                        continue;
                    }

                    if (_results.TryGetValue(id, out var activeRow))
                    {
                        activeRow.MergeIn(item);
                    }
                    else
                    {
                        var newActiveRow = (TActive)TActive.Create(item);
                        _results.Add(id, newActiveRow);
                    }
                }

                // Remove any items with a negative delta
                foreach (var (key, delta) in _changes)
                {
                    if (delta < 0)
                    {
                        var record = _results[key];
                        record.Dispose();
                        _results.Remove(key);
                    }
                }
                _changes.Clear();
            }
        }

        public ObservableDictionary<TKey, TActive> Results => _results;
    }

}
