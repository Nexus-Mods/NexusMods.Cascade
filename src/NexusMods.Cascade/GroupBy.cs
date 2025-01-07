using System;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class GroupBy<TKey, TItem> : AUnaryStageDefinition<TItem, Reduction<TKey, TItem>>
    where TItem : notnull
    where TKey : notnull
{
    private readonly Func<TItem,TKey> _keySelector;

    public GroupBy(Func<TItem, TKey> keySelector, IOutputDefinition<TItem> upstream) : base(upstream)
    {
        _keySelector = keySelector;

    }

    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }

    private class Stage : AUnaryStageDefinition<TItem, Reduction<TKey, TItem>>.Stage
    {
        private readonly GroupBy<TKey,TItem> _definition;

        public Stage(IFlow flow, GroupBy<TKey, TItem> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void Process(IOutputSet<TItem> input, IOutputSet<Reduction<TKey, TItem>> output)
        {
            foreach (var (item, delta) in input.GetResults())
            {
                output.Add(new Reduction<TKey, TItem>(_definition._keySelector(item), item), delta);
            }
        }
    }
}
