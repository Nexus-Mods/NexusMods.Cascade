using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Implementation;

public class GroupBy<TBase, TActive, TKey> : AUnaryStageDefinition<TBase, TActive, NoState>
    where TKey : notnull
    where TBase : IRowDefinition<TKey>
    where TActive : IRowDefinition<TKey, TBase>
{
     private readonly IStageDefinition<TBase> _upstream;

    public GroupBy(IStageDefinition<TBase> upstream) : base(upstream)
    {
        _upstream = upstream;
    }

    public override IStage CreateInstance(IFlow flow)
    {
        var upstream = (IStage<TBase>)_upstream.CreateInstance(flow);
        return new GroupByStage(this, upstream, flow, new NoState());
    }

    protected override void AcceptChange(TBase input, int delta, ref ChangeSetWriter<TActive> writer, NoState state)
    {
        throw new NotSupportedException();
    }

    private sealed class GroupByStage : Stage
    {
        Ref<ImmutableDictionary<TKey, TActive>> _state = new(ImmutableDictionary<TKey, TActive>.Empty);

        internal GroupByStage(AUnaryStageDefinition<TBase, TActive, NoState> definition, IStage<TBase> upstream, IFlow flow, NoState state) : base(definition, upstream, flow, state)
        {
            var writer = new ChangeSetWriter<TBase>();
            upstream.WriteCurrentValues(ref writer);
            AcceptChange(0, writer.ToChangeSet());
        }

        public override void AcceptChange<TDelta>(int inputIndex, in ChangeSet<TDelta> delta)
        {
            var set = delta.Set;

        }

        public override void WriteCurrentValues(ref ChangeSetWriter<TActive> writer)
        {
            foreach (var (key, resultSet) in _state.Value)
            {
                writer.Add(resultSet, 1);
            }
        }
    }
}
