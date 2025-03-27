using System;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public class ValueOutlet<T>(IStageDefinition<Value<T>> upstream) : IStageDefinition<T>
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new Stage(this, (IStage<Value<T>>)upstreamInstance, (Flow)flow);
    }

    private sealed class Stage : IValueOutlet<T>
    {
        private readonly Ref<T> _value;
        private readonly ValueOutlet<T> _definition;
        private readonly IStage<Value<T>> _upstream;
        private readonly Flow _flow;

        public Stage(ValueOutlet<T> definition, IStage<Value<T>> upstream, Flow flow)
        {
            flow.AddStageInstance(definition, this);
            _definition = definition;
            _upstream = upstream;
            _flow = flow;
            upstream.ConnectOutput(this, 0);
            var initialValue = upstream.CurrentValue;
            _value = new Ref<T>(initialValue.V);
        }

        public T Value => _value.Value;

        public ReadOnlySpan<IStage> Inputs => new([_upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => ReadOnlySpan<(IStage Stage, int Index)>.Empty;

        public void ConnectInput(IStage stage, int index)
        {
            throw new NotImplementedException();
        }

        public void ConnectOutput(IStage stage, int index)
        {
            throw new NotImplementedException();
        }

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            if (inputIndex != 0)
                throw new InvalidOperationException();

            _value.Value = ((Value<T>)(object)delta!).V;
        }
    }
}
