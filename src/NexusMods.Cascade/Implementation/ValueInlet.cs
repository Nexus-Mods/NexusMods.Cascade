using System;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public class ValueInlet<T> : IValueQuery<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var castedFlow = (Flow)flow;
        var instance = new Stage(this, castedFlow);
        castedFlow.AddStageInstance(this, instance);
        return instance;
    }

    private sealed class Stage(ValueInlet<T> definition, Flow flow) : IStage<T>, IInlet<Value<T>>
    {
        public IStageDefinition Definition => definition;
        private readonly Ref<T> _value = new(default!);

        public IFlow Flow => flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            throw new InvalidOperationException("ValueInlet does not accept changes.");
        }

        public void Push(in Value<T> value)
        {
            _value.Value = value.V;
            flow.ForwardChange(this, value);
        }

        public T CurrentValue => _value.Value;
    }
}
