using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Delta;

public sealed class SetInlet<T> : IDeltaQuery<T>
{
    public IStage CreateInstance(IFlow flow)
        => new Stage(this, (Flow)flow);

    private sealed class Stage : IStage<ChangeSet<T>>, ISetInlet<T>
    {
        private readonly Ref<ImmutableHashSet<T>> _value = new(ImmutableHashSet<T>.Empty);
        private readonly Ref<ImmutableArray<(IStage Stage, int Index)>> _outputs = new(ImmutableArray<(IStage Stage, int Index)>.Empty);
        private readonly SetInlet<T> _definition;
        private readonly Flow _flow;

        public Stage(SetInlet<T> definition, Flow flow)
        {
            _definition = definition;
            _flow = flow;
            _flow.AddStageInstance(definition, this);
        }

        public ReadOnlySpan<IStage> Inputs => ReadOnlySpan<IStage>.Empty;
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();
        public void ConnectOutput(IStage stage, int index)
        {
            _outputs.Value = _outputs.Value.Add((stage, index));
        }

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            throw new NotSupportedException("SetInlet does not accept changes");
        }

        public ChangeSet<T> CurrentValue => new(_value.Value);
        public void Push(in ChangeSet<T> changes)
        {
            var newValue = _value.Value.ToBuilder();
            foreach (var (item, delta) in changes.Changes)
            {
                if (delta > 0)
                    newValue.Add(item);
                else
                    newValue.Remove(item);
            }
            _value.Value = newValue.ToImmutable();

            foreach (var (stage, idx) in _outputs.Value.AsSpan())
                stage.AcceptChange(idx, changes);
        }
    }
}
