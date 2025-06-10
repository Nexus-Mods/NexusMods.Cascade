using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
            var tcs = new TaskCompletionSource<IDisposable>();
            var view = new OutletNodeView<TValue>(topology, flow);
            topology.PrimaryRunner.Enqueue(() =>
            {
                try
                {
                    var disposable = Disposable.Create(() =>
                    {
                        view.OutputChanged -= UpdateFn;
                        view.Dispose();
                    });

                    view.OutputChanged += UpdateFn;

                    topology.QueryCore(flow, view, true);

                    tcs.SetResult(disposable);
                    return;

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
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            await view.Initialized;
            return await tcs.Task;
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
            .Transform(f =>
            {
                var cell = cellSelector(f);
                var observable = cell.AsSystemObservable().StartWith(cell.Value);
                return observable;
            })
            .Where(changes => changes.Any(c => c.Reason is ListChangeReason.Add or ListChangeReason.AddRange))
            .Select(changes =>
            {
                foreach (var change in changes)
                {
                    if (change.Reason == ListChangeReason.Add)
                        return change.Item.Current;
                    if (change.Reason == ListChangeReason.AddRange)
                        return change.Range.First();
                }
                throw new InvalidOperationException("No added changes found, this should not happen");
            })
            .Switch();
    }

}
