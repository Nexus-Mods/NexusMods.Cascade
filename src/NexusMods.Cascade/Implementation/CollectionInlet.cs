using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using Clarp;
using Clarp.Concurrency;
using Clarp.Utils;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Implementation;

public class CollectionInlet<T> : IQuery<T> where T : notnull
{
    public IStage CreateInstance(IFlow flow)
    {
        var castedFlow = (Flow)flow;
        var instance = new Stage(this, castedFlow);
        castedFlow.AddStageInstance(this, instance);
        return instance;
    }

    private sealed class Stage(CollectionInlet<T> definition, Flow flow) : IStage<T>, IInlet<T>
    {
        private readonly Ref<ImmutableArray<(IStage Stage, int Index)>> _outputs = new(ImmutableArray<(IStage, int)>.Empty);
        public void WriteCurrentValues(ref ChangeSetWriter<T> writer)
        {
            writer.Add(_state.Value);
        }

        public ReadOnlySpan<IStage> Inputs => ReadOnlySpan<IStage>.Empty;

        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();

        public void ConnectOutput(IStage stage, int index)
            => _outputs.Value = _outputs.Value.Add((stage, index));

        public IStageDefinition Definition => definition;
        private readonly Ref<ResultSet<T>> _state = new(new ResultSet<T>());

        public IFlow Flow => flow;
        public void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> delta) where T1 : notnull
        {
            throw new NotSupportedException();
        }

        public void Complete(int inputIndex)
        {
            Debug.Assert(inputIndex == 0);
            foreach (var (stage, index) in _outputs.Value.AsSpan())
            {
                stage.Complete(index);
            }
        }

        public void AddChanges(ReadOnlySpan<T> values, int delta)
        {
            Runtime.DoSync(static state =>
            {
                var (selfAndDelta, values) = state;
                var (self, delta) = selfAndDelta;

                var writer = new ChangeSetWriter<T>();
                writer.Add(delta, values);
                self._state.Value = self._state.Value.Merge(writer.ToChangeSet());
                writer.ForwardAll(self);
                return 0;
            }, RefTuple.Create((this, delta), values));
        }
    }
}
