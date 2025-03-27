using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Omega;

public class ValueInlet<T> : IValueQuery<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var castedFlow = (Flow)flow;
        var instance = new Stage(this, castedFlow);
        castedFlow.AddStageInstance(this, instance);
        return instance;
    }

    private sealed class Stage(ValueInlet<T> definition, Flow flow) : IStage<Value<T>>, IInlet<Value<T>>
    {
        private readonly Ref<ImmutableArray<(IStage Stage, int Index)>> _outputs = new(ImmutableArray<(IStage, int)>.Empty);
        public ReadOnlySpan<IStage> Inputs => ReadOnlySpan<IStage>.Empty;

        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();

        public void ConnectOutput(IStage stage, int index)
            => _outputs.Value = _outputs.Value.Add((stage, index));

        public IStageDefinition Definition => definition;
        private readonly Ref<T> _value = new(default!);

        public IFlow Flow => flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            throw new InvalidOperationException("ValueInlet does not accept changes.");
        }

        public void Push(in Value<T> value)
        {
            _value.Value = value.V;
            foreach (var (stage, idx) in _outputs.Value.AsSpan())
                stage.AcceptChange(idx, value);
        }

        public Value<T> CurrentValue => new(_value.Value);
    }
}
