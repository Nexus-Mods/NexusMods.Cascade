using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;
using ObservableCollections;

namespace NexusMods.Cascade.Outlets;

/// <summary>
/// A outlet that simply lists all the items as a unsorted bag of items
/// </summary>
public sealed class CollectionResult<T>(UpstreamConnection upstreamConnection) : Outlet<T>(upstreamConnection), IOutletDefinition<T>
    where T : notnull
{
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    public new static IOutletDefinition<T> Create(UpstreamConnection upstreamConnection)
    {
        return new CollectionResult<T>(upstreamConnection);
    }

    public class Stage(IFlowImpl flow, CollectionResult<T> definition) : Outlet<T>.Stage(flow, definition)
    {
        private ResultSet<T> _results = new();
        protected override void ReleaseChanges(ChangeSet<T> changeSet)
        {
            _results = _results.With(changeSet);
        }

        public ResultSet<T> Results => _results;
    }

}
