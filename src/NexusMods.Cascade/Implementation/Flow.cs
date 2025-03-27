using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

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

    private IValueOutlet<T> GetOutlet<T>(IQuery<Value<T>> query)
    {
        var stage = AddStage(query);
        if (_outlets.Value.TryGetValue(stage, out var outlet))
            return (IValueOutlet<T>)outlet;

        var definition = new ValueOutlet<T>(query);
        var instance = (IValueOutlet<T>)definition.CreateInstance(this);
        _outlets.Value = _outlets.Value.Add(stage, instance);
        return instance;

    }

    public T Query<T>(IQuery<Value<T>> query)
    {
        return LockingTransaction.RunInTransaction(() =>
        {
            var outlet = GetOutlet(query);
            return outlet.Value;
        });
    }

    public void Set<T>(ValueInlet<T> inlet, T newValue)
    {
        LockingTransaction.RunInTransaction(() =>
        {
            var stage = AddStage(inlet);
            ((IInlet<Value<T>>)stage).Push(new Value<T>(newValue));
            return 0;
        });
    }

    internal void AddStageInstance(IStageDefinition definition, IStage stage)
    {
        _stages.Value = _stages.Value.Add(definition, stage);
    }
}
