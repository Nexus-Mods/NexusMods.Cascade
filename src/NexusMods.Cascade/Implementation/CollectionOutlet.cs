using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using ObservableCollections;

namespace NexusMods.Cascade.Implementation;

public sealed class CollectionOutlet<T>(IStageDefinition<T> upstream) : IStageDefinition<T> where T : notnull, IComparable<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new Stage(this, (IStage<T>)upstreamInstance, (Flow)flow);
    }

    private sealed class Stage : ICollectionOutlet<T>
    {
        private readonly Ref<ResultSet<T>> _values;
        private readonly ObservableList<T> _observableList = [];
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
            var resultSet = writer.ToResultSet();
            _values = new Ref<ResultSet<T>>(resultSet);
            _observableList.AddRange(resultSet);
        }

        #region Cascade Code

        public ResultSet<T> Values => _values.Value;
        public ObservableList<T> Observable => _observableList;

        public ReadOnlySpan<IStage> Inputs => new([_upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => ReadOnlySpan<(IStage Stage, int Index)>.Empty;


        public void ConnectOutput(IStage stage, int index)
        {
            throw new NotImplementedException();
        }

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> changes) where T1 : IComparable<T1>
        {
            _values.Value = _values.Value.Merge(changes, out var netChanges);

            _flow.EnqueueEffect(static state =>
            {
                var (self, changes) = state;
                foreach (var (key, delta) in changes)
                {
                    if (delta > 0)
                    {
                        self._observableList.Add(key);
                    }
                    else
                    {
                        self._observableList.Remove(key);
                    }
                }
            }, (this, netChanges));
        }

        public void Complete(int inputIndex)
        {
            // This is a no-op for CollectionOutlet
        }


        #endregion

    }
}
