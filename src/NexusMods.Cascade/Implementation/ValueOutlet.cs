using System;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public class ValueOutlet<T> : IStageDefinition<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var castedFlow = (Flow)flow;
        var instance = new Stage(this, castedFlow);
        castedFlow.AddStageInstance(this, instance);
        return instance;
    }

    private sealed class Stage(ValueOutlet<T> definition, Flow flow) : IValueOutlet<T>
    {
        private readonly Ref<T> _value = new(default!);

        public T Value => _value.Value;

        public IStageDefinition Definition => definition;
        public IFlow Flow => flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            if (inputIndex != 0)
                throw new InvalidOperationException();

            _value.Value = ((Value<T>)(object)delta!).V;
        }

    }
}
