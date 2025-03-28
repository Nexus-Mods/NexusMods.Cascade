using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Omega;

public sealed class CollectionOutlet<T>(IStageDefinition<T> upstream) : IStageDefinition<T> where T : notnull
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new Stage(this, (IStage<T>)upstreamInstance, (Flow)flow);
    }

    private sealed class Stage : ICollectionOutlet<T>
    {
        private readonly Ref<ImmutableDictionary<T, int>> _values;
        private readonly CollectionOutlet<T> _definition;
        private readonly IStage<T> _upstream;
        private readonly Flow _flow;

        public Stage(CollectionOutlet<T> definition, IStage<T> upstream, Flow flow)
        {
            flow.AddStageInstance(definition, this);
            _definition = definition;
            _upstream = upstream;
            _flow = flow;
            upstream.ConnectOutput(this, 0);
            var writer = ChangeSetWriter<T>.Create();
            upstream.WriteCurrentValues(ref writer);
            _values = new Ref<ImmutableDictionary<T, int>>(writer.ToImmutableDictionary());
            writer.Dispose();
        }

        public ImmutableDictionary<T, int> Values => _values.Value;

        public ReadOnlySpan<IStage> Inputs => new([_upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => ReadOnlySpan<(IStage Stage, int Index)>.Empty;


        public void ConnectOutput(IStage stage, int index)
        {
            throw new NotImplementedException();
        }

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> changes) where T1 : notnull
        {
            var builder = _values.Value.ToBuilder();

            foreach (var (change, delta) in changes.Changes)
            {
                var casted = (T)(object)change;
                if (builder.TryGetValue(casted, out var value))
                {
                    if (value + delta == 0)
                    {
                        builder.Remove(casted);
                    }
                    else
                    {
                        builder[casted] = value + delta;
                    }
                }
                else
                {
                    builder[casted] = delta;
                }
            }

            _values.Value = builder.ToImmutable();
        }

        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            throw new NotImplementedException();
        }
    }
}
