using System;
using System.Collections.Immutable;
using System.ComponentModel;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using R3;

namespace NexusMods.Cascade.Implementation;

public sealed class ValueOutlet<T>(IStageDefinition<T> upstream) : IStageDefinition<T> where T : notnull
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new ValueOutletStage(this, (IStage<T>)upstreamInstance, (Flow)flow);
    }

    private sealed class ValueOutletStage : BindableReactiveProperty<T>, IValueOutlet<T>
    {
        private readonly Ref<T> _value;
        private readonly ValueOutlet<T> _definition;
        private readonly IStage<T> _upstream;
        private readonly Flow _flow;

        public ValueOutletStage(ValueOutlet<T> definition, IStage<T> upstream, Flow flow)
        {
            flow.AddStageInstance(definition, this);
            _definition = definition;
            _upstream = upstream;
            _flow = flow;
            upstream.ConnectOutput(this, 0);
            var writer = ChangeSetWriter<T>.Create();
            upstream.WriteCurrentValues(ref writer);
            var changes = writer.ToChangeSet().Changes;
            _value = changes.Length > 0 ? new Ref<T>(changes[0].Value) : new Ref<T>();
            base.Value = _value.Value;
        }

        public void WriteCurrentValues(ref ChangeSetWriter<T> writer)
            => writer.Add(_value.Value, 1);

        public ReadOnlySpan<IStage> Inputs => new([_upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => ReadOnlySpan<(IStage Stage, int Index)>.Empty;


        public void ConnectOutput(IStage stage, int index)
        {
            throw new NotImplementedException();
        }

        public IStageDefinition Definition => _definition;
        public IFlow Flow => _flow;
        public void AcceptChange<T1>(int inputIndex, in ChangeSet<T1> changes) where T1 : notnull
        {
            foreach (var (value, delta) in changes.Changes)
            {
                if (delta < 0)
                    continue;

                var casted = (T)(object)value;
                _value.Value = casted;
            }
            _flow.EnqueueEffect(static self => self.SyncValues(), this);
        }

        private void SyncValues() => base.Value = _value.Value;

        public void Complete(int inputIndex)
        {
            // This is a no-op for CollectionOutlet
        }

        public T Value => _value.Value;
        public Observable<T> Observable => AsObservable();
    }
}
