using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Flows;

public class ActiveRowFlow<TBase, TKey, TActive> : Flow<TActive>
    where TBase : IRowDefinition<TKey>
    where TKey : notnull
    where TActive : IActiveRow<TBase, TKey>
{
    public override Node CreateNode(Topology topology)
    {
        return new ActiveRowNode(topology, this);
    }

    internal class ActiveRowNode : Node<TActive>
    {
        private ImmutableDictionary<TKey, TActive> _rows;

        public ActiveRowNode(Topology topology, ActiveRowFlow<TBase, TKey, TActive> flow) : base(topology, flow, 1)
        {
            _rows = ImmutableDictionary<TKey, TActive>.Empty;
        }

        public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
        {
            if (idx != 0)
                throw new System.ArgumentOutOfRangeException(nameof(idx), "Unary node only has one inlet.");

            var toCheck = new HashSet<TKey>(_rows.Keys);

            var oldState = _rows;
            var builder = _rows.ToBuilder();
            foreach (var (baseRow, delta) in ((IToDiffSpan<TBase>)diffSet).ToDiffSpan())
            {
                var key = baseRow.RowId;
                toCheck.Add(key);
                if (builder.TryGetValue(key, out var activeRow))
                {
                    activeRow.SetUpdate(baseRow, delta);
                }
                else
                {
                    var newRow = (TActive)TActive.Create(baseRow, delta);
                    builder.Add(key, newRow);
                    Output.Add(newRow, 1);
                }
            }

            foreach (var checkKey in toCheck)
            {
                var row = builder[checkKey];
                if (row.NextDelta == 0)
                {
                    builder.Remove(checkKey);
                    Output.Add(row, -1);
                }
            }

            var newState = builder.ToImmutable();
            _rows = newState;

            Topology.EnqueueEffect(() =>
            {
                foreach (var key in toCheck)
                {
                    if (newState.TryGetValue(key, out var row))
                    {
                        row.ApplyUpdates();
                        continue;
                    }

                    // It's not in the new state, so it must have been deleted
                    var oldRow = oldState[key];
                    oldRow.ApplyUpdates();
                    oldRow.Dispose();
                }
            });

        }

        public override void Prime()
        {
            var upstream = (Node<TBase>)Upstream[0];
            upstream.ResetOutput();
            upstream.Prime();
            Accept(0, upstream.Output);
            upstream.ResetOutput();
        }
    }
}
