using System;
using Clarp;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Implementation;

public class ValueInlet<T> : IQuery<T> where T : IComparable<T>
{
    public IStage CreateInstance(IFlow flow)
        => new ValueInletStage(this, flow);


    private sealed class ValueInletStage : AStage<T, ValueInlet<T>>, IObserver<T>, IValueInlet<T>
    {
        private readonly Ref<T> _value;

        public ValueInletStage(ValueInlet<T> definition, IFlow flow) : base(definition, flow)
        {
            _value = new Ref<T>(default!);
        }

        public override void WriteCurrentValues(ref ChangeSetWriter<T> writer)
        {
            writer.Write(_value.Value, 1);
        }

        public override ReadOnlySpan<IStage> Inputs => ReadOnlySpan<IStage>.Empty;
        public override void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> delta)
            => throw new NotSupportedException("ValueInlet does not accept changes.");

        public void OnCompleted()
        {
            Complete(0);
        }

        public void OnError(Exception error)
        {
            // No-op
        }

        public void OnNext(T value)
        {
            Runtime.DoSync(() =>
            {
                var prevValue = _value.Value;
                _value.Value = value;
                var writer = new ChangeSetWriter<T>();
                writer.Add(prevValue, -1);
                writer.Add(value, 1);
                writer.ForwardAll(this);
            });
        }

        public T Value
        {
            get => _value.Value;
            set => OnNext(value);
        }
    }

}
