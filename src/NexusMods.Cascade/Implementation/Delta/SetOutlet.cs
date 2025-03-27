using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Implementation.Delta;

public sealed class SetOutlet<T>(IStageDefinition<ChangeSet<T>> upstreamDefinition) : IStageDefinition<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstream = (IStage<ChangeSet<T>>)flow.AddStage(upstreamDefinition);
        return new Stage(this, upstream, (Flow)flow);
    }


    private sealed class Stage : ISetOutlet<T>
    {
        private readonly Ref<ImmutableHashSet<T>> _value;
        private readonly SetOutlet<T> _definition;
        private readonly IStage<ChangeSet<T>> _upstream;
        private readonly Flow _flow;

        public Stage(SetOutlet<T> definition, IStage<ChangeSet<T>> upstream, Flow flow)
        {
            flow.AddStageInstance(definition, this);
            _definition = definition;
            _upstream = upstream;
            _flow = flow;
            upstream.ConnectOutput(this, 0);
            var initialValue = upstream.CurrentValue;
            _value = new Ref<ImmutableHashSet<T>>(initialValue.ToHashSet());
        }

        public ReadOnlySpan<IStage> Inputs => new ([_upstream]);

        public ReadOnlySpan<(IStage Stage, int Index)> Outputs
            => ReadOnlySpan<(IStage Stage, int Index)>.Empty;

        public void ConnectOutput(IStage stage, int index)
            => throw new InvalidOperationException("SetOutlet doesn't have outputs.");

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            if (inputIndex != 0)
                throw new InvalidOperationException("SetOutlet can only have one input.");

            var builder = _value.Value.ToBuilder();
            foreach (var changes in ((ChangeSet<T>)(object)delta!).Changes)
            {
                if (changes.Delta > 0)
                    builder.Add(changes.Value);
                else
                    builder.Remove(changes.Value);
            }
            _value.Value = builder.ToImmutable();
        }

        public ImmutableHashSet<T> Value => _value.Value;
    }
}
