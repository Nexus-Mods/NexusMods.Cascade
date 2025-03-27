using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

internal sealed class Flow : IFlow
{
    private readonly Ref<ImmutableDictionary<IStageDefinition, IStage>> _stages = new(ImmutableDictionary<IStageDefinition, IStage>.Empty);
    private readonly Ref<ImmutableDictionary<IStage, ImmutableList<(IStage Stage, int idx)>>> _connections = new(ImmutableDictionary<IStage, ImmutableList<(IStage Stage, int idx)>>.Empty);
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

        var definition = new ValueOutlet<T>();
        var instance = (IValueOutlet<T>)definition.CreateInstance(this);
        _outlets.Value = _outlets.Value.Add(stage, instance);
        Connect(stage, instance, 0);
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

    public void ForwardChange<TDelta>(IStage stage, TDelta delta)
    {
        if (!_connections.Value.TryGetValue(stage, out var connections))
            return;

        foreach (var (downstream, inputIndex) in connections)
        {
            downstream.AcceptChange(inputIndex, delta);
        }
    }

    internal void AddStageInstance(IStageDefinition definition, IStage stage)
    {
        _stages.Value = _stages.Value.Add(definition, stage);
    }

    internal void Connect(IStage upstream, IStage downstream, int inputIndex)
    {
        if (!_connections.Value.TryGetValue(upstream, out var connections))
            connections = ImmutableList<(IStage Stage, int idx)>.Empty;

        _connections.Value = _connections.Value.Add(upstream, connections.Add((downstream, inputIndex)));
    }
}
