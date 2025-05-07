using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.Cascade.Collections;
using R3;
using Disposable = System.Reactive.Disposables.Disposable;
using Observable = System.Reactive.Linq.Observable;

namespace NexusMods.Cascade;

public static class TopologyExtensions
{
    /// <summary>
    /// Return an observable changeset of the given flow. This would be the same as calling `QueryAsync` and then subscribing to the `OutputChanged` event.
    /// </summary>
    public static IObservable<ChangeSet<TValue>> Observe<TValue>(this Topology topology, Flow<TValue> flow)
        where TValue : notnull
    {
        var observable = Observable.Create<ChangeSet<TValue>>(async observer =>
        {
            var outlet = await topology.QueryAsync(flow);

            var disposable = Disposable.Create(() =>
            {
                outlet.OutputChanged -= UpdateFn;
                outlet.Dispose();
            });

            outlet.OutputChanged += UpdateFn;
            return disposable;

            void UpdateFn(IToDiffSpan<TValue> diffSpan)
            {
                var changes = new ChangeSet<TValue>();
                foreach (var (val, diff) in diffSpan.ToDiffSpan())
                {
                    if (diff > 0)
                        changes.Add(new Change<TValue>(ListChangeReason.Add, val));
                    else if (diff < 0)
                        changes.Add(new Change<TValue>(ListChangeReason.Remove, val));
                }
                if (changes.Count > 0)
                {
                    observer.OnNext(changes);
                }
            }
        });
        return observable;
    }

    /// <summary>
    /// Observe a specific cell (property) on a specific row in a flow of active rows
    /// </summary>
    public static IObservable<TValue> ObserveCell<TActive, TRow, TKey, TValue>(this Topology topology, Flow<TActive> activeRows, TKey forKey, Func<TActive, BindableReactiveProperty<TValue>> cellSelector)
        where TActive : IActiveRow<TRow, TKey>
        where TRow : IRowDefinition<TKey>
        where TValue : notnull
        where TKey : IEquatable<TKey>
    {
        return topology
            .Observe(activeRows)
            .Filter(f => f.RowId.Equals(forKey))
            .Transform(f => cellSelector(f).AsSystemObservable())
            .Select(s => s.First(static f => f.Reason == ListChangeReason.Add).Item.Current)
            .Switch();
    }

}
