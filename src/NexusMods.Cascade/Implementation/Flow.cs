using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Clarp;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation.Omega;
using ObservableCollections;
using R3;

namespace NexusMods.Cascade.Implementation;

internal sealed class Flow : IFlow
{
    private readonly Ref<ImmutableDictionary<IStageDefinition, IStage>> _stages = new(ImmutableDictionary<IStageDefinition, IStage>.Empty);
    private readonly Ref<ImmutableDictionary<(IStage Stage, Type Type), IOutlet>> _outlets = new(ImmutableDictionary<(IStage Stage, Type Type), IOutlet>.Empty);
    private readonly Agent<int> _externalUpdates = new(0);

    public IStage AddStage(IStageDefinition definition)
    {
        return Runtime.DoSync(() =>
        {
            if (_stages.Value.TryGetValue(definition, out var stage))
                return stage;

            return definition.CreateInstance(this);
        });
    }

    public ResultSet<T> QueryAll<T>(IQuery<T> query) where T : notnull
    {
        return Runtime.DoSync(() =>
        {
            var outlet = GetCollectionOutlet(query);
            return outlet.Values;
        });
    }

    public T QueryOne<T>(IQuery<T> query) where T : notnull
    {
        return Runtime.DoSync(() =>
        {
            var outlet = GetValueOutlet(query);
            return outlet.Value;
        });
    }

    public IInlet<T> Get<T>(CollectionInlet<T> inlet) where T : notnull
    {
        var inletStage = AddStage(inlet);
        return (IInlet<T>)inletStage;
    }

    public IValueInlet<T> Get<T>(ValueInlet<T> inlet) where T : notnull
    {
        var inletStage = AddStage(inlet);
        return (IValueInlet<T>)inletStage;
    }

    public Observable<T> Observe<T>(IQuery<T> query) where T : notnull
    {
        return Runtime.DoSync(static input =>
        {
            var (query, flow) = input;
            var outlet = flow.GetValueOutlet(query);
            return outlet.Observable;
        }, (query, this));
    }

    public ObservableList<T> ObserveAll<T>(IQuery<T> query) where T : notnull
    {
        return Runtime.DoSync(static input =>
        {
            var (query, flow) = input;
            var outlet = flow.GetCollectionOutlet(query);
            return outlet.Observable;
        }, (query, this));
    }

    public void EnqueueEffect<TState>(Action<TState> effect, TState state) where TState : notnull
    {
        _externalUpdates.Send(s =>
        {
            effect(state);
            return s;
        });
    }

    public Task FlushAsync()
    {
        var tcs = new TaskCompletionSource<int>();
        _externalUpdates.Send(_ =>
        {
            tcs.TrySetResult(0);
            return 0;
        });
        return tcs.Task;
    }

    private ICollectionOutlet<T> GetCollectionOutlet<T>(IQuery<T> query) where T : notnull
    {
        var type = typeof(ICollectionOutlet<T>);
        var stage = AddStage(query);
        if (_outlets.Value.TryGetValue((stage, type), out var outlet))
            return (ICollectionOutlet<T>)outlet;

        var definition = new CollectionOutlet<T>(query);
        var instance = (ICollectionOutlet<T>)definition.CreateInstance(this);
        _outlets.Value = _outlets.Value.Add((stage, type), instance);
        return instance;
    }

    private IValueOutlet<T> GetValueOutlet<T>(IQuery<T> query) where T : notnull
    {
        var type = typeof(IValueOutlet<T>);
        var stage = AddStage(query);
        if (_outlets.Value.TryGetValue((stage, type), out var outlet))
            return (IValueOutlet<T>)outlet;

        var definition = new ValueOutlet<T>(query);
        var instance = (IValueOutlet<T>)definition.CreateInstance(this);
        _outlets.Value = _outlets.Value.Add((stage, type), instance);
        return instance;
    }

/*
    public void Update<T>(Inlet<T> setInlet, params T[] valueTuple)
    {
        LockingTransaction.RunInTransaction(() =>
        {
            var inlet = (ISetInlet<T>)AddStage(setInlet);
            inlet.Push(ChangeSet<T>.AddAll(valueTuple.AsSpan()));
            return 0;
        });
    }
*/


    internal void AddStageInstance(IStageDefinition definition, IStage stage)
    {
        _stages.Value = _stages.Value.Add(definition, stage);
    }
}
