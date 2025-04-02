using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Implementation;

public class GroupBy<TInput, TKey> : AUnaryStageDefinition<TInput, KeyedResultSet<TKey, TInput>, NoState>
    where TKey : notnull where TInput : notnull
{
    private readonly Func<TInput,TKey> _keyFn;
    private readonly IStageDefinition<TInput> _upstream;

    public GroupBy(IStageDefinition<TInput> upstream, Func<TInput, TKey> keyFn) : base(upstream)
    {
        _upstream = upstream;
        _keyFn = keyFn;
    }

    public override IStage CreateInstance(IFlow flow)
    {
        var upstream = (IStage<TInput>)_upstream.CreateInstance(flow);
        return new GroupByStage(this, upstream, flow, new NoState());
    }

    protected override void AcceptChange(TInput input, int delta, ref ChangeSetWriter<KeyedResultSet<TKey, TInput>> writer, NoState state)
    {
        throw new NotSupportedException();
    }

    private sealed class GroupByStage : Stage
    {
        Ref<ImmutableDictionary<TKey, ResultSet<TInput>>> _state = new(ImmutableDictionary<TKey, ResultSet<TInput>>.Empty);

        internal GroupByStage(AUnaryStageDefinition<TInput, KeyedResultSet<TKey, TInput>, NoState> definition, IStage<TInput> upstream, IFlow flow, NoState state) : base(definition, upstream, flow, state)
        {
            var writer = new ChangeSetWriter<TInput>();
            upstream.WriteCurrentValues(ref writer);
            AcceptChange(0, writer.ToChangeSet());
        }

        public override void AcceptChange<TDelta>(int inputIndex, in ChangeSet<TDelta> delta)
        {
            var modified = new HashSet<TKey>();
            var newState = _state.Value;
            var oldState = newState;

            foreach (var (uncasted, deltaValue) in delta.Changes)
            {
                var change = (TInput)(object)uncasted;
                var key = ((GroupBy<TInput, TKey>)_definition)._keyFn(change);
                if (newState.TryGetValue(key, out var resultSet))
                {
                    modified.Add(key);
                    newState = newState.SetItem(key, resultSet.Add(change, deltaValue));
                }
                else
                {
                    modified.Add(key);
                    var newResultSet = new ResultSet<TInput>();
                    newState = newState.SetItem(key, newResultSet.Add(change, deltaValue));
                }
            }

            _state.Value = newState;
            var writer = new ChangeSetWriter<KeyedResultSet<TKey, TInput>>();
            foreach (var key in modified)
            {
                if (oldState.TryGetValue(key, out var prev))
                {
                    writer.Add(new KeyedResultSet<TKey, TInput>(key, prev), -1);
                }
                if (newState.TryGetValue(key, out var newValue))
                {
                    writer.Add(new KeyedResultSet<TKey, TInput>(key, newValue), 1);
                }
            }

            writer.ForwardAll(this);
        }

        public override void WriteCurrentValues(ref ChangeSetWriter<KeyedResultSet<TKey, TInput>> writer)
        {
            foreach (var (key, resultSet) in _state.Value)
            {
                writer.Add(new KeyedResultSet<TKey, TInput>(key, resultSet), 1);
            }
        }
    }
}
