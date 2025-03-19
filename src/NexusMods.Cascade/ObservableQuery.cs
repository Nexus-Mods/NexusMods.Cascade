using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class ObservableQuery<TActive, TBase, TKey> : IObservableQuery<TActive, TBase, TKey>
    where TKey : notnull
    where TBase : IRowDefinition<TKey>
    where TActive : IActiveRow<TBase, TKey>
{
    private ImmutableDictionary<TKey, TActive> _rows = ImmutableDictionary<TKey, TActive>.Empty;
    private HashSet<TActive> _modifiedRows = [];


    public void Update(ChangeSet<TBase> changes)
    {


    }
}
