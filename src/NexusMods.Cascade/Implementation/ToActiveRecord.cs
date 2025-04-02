using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Implementation;

public class ToActiveRecord<TBase, TActive, TKey> : AUnaryStageDefinition<TBase, TActive, NoState>
    where TKey : IComparable<TKey>
    where TBase : IRowDefinition<TKey>
    where TActive : IActiveRow<TBase, TKey>
{
     private readonly IStageDefinition<TBase> _upstream;

    public ToActiveRecord(IStageDefinition<TBase> upstream) : base(upstream)
    {
        _upstream = upstream;
    }

    public override IStage CreateInstance(IFlow flow)
    {
        var upstream = (IStage<TBase>)_upstream.CreateInstance(flow);
        return new ToActiveRecordStage(this, upstream, flow, new NoState());
    }

    protected override void AcceptChange(TBase input, int delta, ref ChangeSetWriter<TActive> writer, NoState state)
    {
        throw new NotSupportedException();
    }

    private sealed class ToActiveRecordStage : Stage
    {
        Ref<ImmutableDictionary<TKey, TActive>> _state = new(ImmutableDictionary<TKey, TActive>.Empty);

        internal ToActiveRecordStage(AUnaryStageDefinition<TBase, TActive, NoState> definition, IStage<TBase> upstream, IFlow flow, NoState state) : base(definition, upstream, flow, state)
        {
            var writer = new ChangeSetWriter<TBase>();
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
                var change = (TBase)(object)uncasted;
                var key = change.RowId;
                if (newState.TryGetValue(key, out var foundRow))
                {
                    modified.Add(key);
                    foundRow.MergeIn(change, deltaValue);
                }
                else
                {
                    modified.Add(key);
                    var newRow = TActive.Create(change, deltaValue);
                    newState = newState.SetItem(key, (TActive)newRow);
                }
            }

            _state.Value = newState;
            var writer = new ChangeSetWriter<TActive>();
            foreach (var key in modified)
            {
                if (oldState.TryGetValue(key, out var prev))
                {
                    if (prev.DeltaCount == 0)
                        writer.Add(prev, -1);
                    continue;
                }
                if (newState.TryGetValue(key, out var newValue))
                {
                    if (newValue.DeltaCount > 0)
                        writer.Add(newValue, 1);
                }
            }

            writer.ForwardAll(this);
        }

        public override void WriteCurrentValues(ref ChangeSetWriter<TActive> writer)
        {
            foreach (var (_, activeRow) in _state.Value)
            {
                writer.Add(activeRow, 1);
            }
        }
    }
}
