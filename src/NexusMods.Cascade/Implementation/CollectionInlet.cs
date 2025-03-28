using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public class CollectionInlet<T> : IValueQuery<T> where T : notnull
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
        private readonly Ref<ImmutableDictionary<T, int>> _state = new(ImmutableDictionary<T, int>.Empty);

        public IFlow Flow => flow;
        public void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> delta) where T1 : notnull
        {
            throw new NotImplementedException();
        }

        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            throw new InvalidOperationException("ValueInlet does not accept changes.");
        }

        public T CurrentValue => throw new NotImplementedException();
        public void Push(in ChangeSet<T> value)
        {
            throw new NotImplementedException();
        }

        public void Add(params T[] values)
        {
            LockingTransaction.RunInTransaction(() =>
            {
                var writer = new ChangeSetWriter<T>();
                writer.Add(1, values.AsSpan());

                var builder = _state.Value.ToBuilder();
                foreach (var change in writer.AsSpan())
                {
                    if (builder.TryGetValue(change.Value, out var count))
                    {
                        var newDelta = count + change.Delta;
                        if (newDelta == 0)
                            builder.Remove(change.Value);
                        else
                            builder[change.Value] = newDelta;
                    }
                    else
                        builder[change.Value] = change.Delta;
                }
                _state.Value = builder.ToImmutable();
                writer.ForwardAll(_outputs.Value.AsSpan());
                return 0;
            });
        }

        public void Remove(params T[] values)
        {
            LockingTransaction.RunInTransaction(() =>
            {
                var writer = new ChangeSetWriter<T>();
                writer.Add(-1, values.AsSpan());

                var builder = _state.Value.ToBuilder();
                foreach (var change in writer.AsSpan())
                {
                    if (builder.TryGetValue(change.Value, out var count))
                    {
                        var newDelta = count + change.Delta;
                        if (newDelta == 0)
                            builder.Remove(change.Value);
                        else
                            builder[change.Value] = newDelta;
                    }
                    else
                        builder[change.Value] = change.Delta;
                }
                _state.Value = builder.ToImmutable();
                writer.ForwardAll(_outputs.Value.AsSpan());
                return 0;
            });
        }
    }
}
