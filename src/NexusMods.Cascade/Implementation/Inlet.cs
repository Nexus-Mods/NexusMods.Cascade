using System;
using Clarp;
using Clarp.Concurrency;
using Clarp.Utils;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public class Inlet<T> : IFlow<T>
{
    public ISource<T> ConstructIn(ITopology topology)
    {
        return new InletSource();
    }

    private class InletSource : ASource<T>, IInlet<T>
    {
        private readonly Ref<T> _value = new(default!);
        public override T Current => _value.Value;

        public T Value
        {
            get => _value.Value;

            set
            {
                Runtime.DoSync(static t =>
                {
                    var (self, value) = t;
                    self._value.Value = value;
                    self.Forward(value);
                }, RefTuple.Create(this, value));
            }
        }
    }
}
