using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A grouping stage that groups items by a key
/// </summary>
public class GroupBy<TKey, TItem> : AUnaryStageDefinition<TItem, Reduction<TKey, TItem>>
    where TItem : notnull
    where TKey : notnull
{
    private readonly Func<TItem,TKey> _keySelector;

    /// <summary>
    /// The primary constructor
    /// </summary>
    /// <param name="keySelector"></param>
    /// <param name="upstream"></param>
    public GroupBy(Func<TItem, TKey> keySelector, IOutputDefinition<TItem> upstream) : base(upstream)
    {
        _keySelector = keySelector;

    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private new class Stage(IFlowImpl flow, GroupBy<TKey, TItem> definition)
        : AUnaryStageDefinition<TItem, Reduction<TKey, TItem>>.Stage(flow, definition)
    {
        protected override void Process(IOutputSet<TItem> input, IOutputSet<Reduction<TKey, TItem>> output)
        {
            foreach (var (item, delta) in input.GetResults())
            {
                output.Add(new Reduction<TKey, TItem>(definition._keySelector(item), item), delta);
            }
        }
    }
}
