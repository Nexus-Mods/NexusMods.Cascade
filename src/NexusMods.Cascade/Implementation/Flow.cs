using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Implementation;

internal sealed class Flow : IFlow
{
    private readonly Ref<ImmutableDictionary<IStageDefinition, IStage>> _stages = new(ImmutableDictionary<IStageDefinition, IStage>.Empty);
    private readonly Ref<ImmutableDictionary<IStage, IOutlet>> _outlets = new(ImmutableDictionary<IStage, IOutlet>.Empty);

    public IStage AddStage(IStageDefinition definition)
    {
        return LockingTransaction.RunInTransaction(() =>
        {
            if (_stages.Value.TryGetValue(definition, out var stage))
                return stage;

            return definition.CreateInstance(this);
        });
    }

    public ImmutableDictionary<T, int> QueryAll<T>(IQuery<T> query) where T : notnull
    {
        return LockingTransaction.RunInTransaction(() =>
        {
            var outlet = GetOutlet(query);
            return outlet.Values;
        });
    }

    public void Set<T>(CollectionInlet<T> collectionInlet, T newValue) where T : notnull
    {
        throw new NotImplementedException();
    }

    public void Update<T>(IInlet<T> setInlet, params T[] valueTuple) where T : notnull
    {
        throw new NotImplementedException();
    }

    public IInlet<T> Get<T>(CollectionInlet<T> inlet) where T : notnull
    {
        var inletStage = AddStage(inlet);
        return (IInlet<T>)inletStage;
    }

    private ICollectionOutlet<T> GetOutlet<T>(IQuery<T> query) where T : notnull
    {
        var stage = AddStage(query);
        if (_outlets.Value.TryGetValue(stage, out var outlet))
            return (ICollectionOutlet<T>)outlet;

        var definition = new CollectionOutlet<T>(query);
        var instance = (ICollectionOutlet<T>)definition.CreateInstance(this);
        _outlets.Value = _outlets.Value.Add(stage, instance);
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
