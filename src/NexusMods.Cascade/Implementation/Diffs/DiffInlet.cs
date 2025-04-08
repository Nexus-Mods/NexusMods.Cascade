using System;
using Clarp;
using Clarp.Concurrency;
using Clarp.Utils;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class DiffInlet<T> : IDiffFlow<T>
{
    public ISource<DiffSet<T>> ConstructIn(ITopology topology)
    {
        return new InletSource();
    }

    private class InletSource : ASource<DiffSet<T>>, IDiffInlet<T>, IDiffSource<T>
    {
        private readonly ResultSet<T> _value = new();
        public override DiffSet<T> Current => Value;

        public DiffSet<T> Value
        {
            get => _value.AsDiffSet();

            set
            {
                Runtime.DoSync(static t =>
                {
                    var (self, value) = t;
                    var oldValue = self._value.AsDiffSet();
                    var writer = new DiffSetWriter<T>();

                    foreach (var (v, delta) in oldValue.AsSpan())
                    {
                        writer.Add(v, -delta);
                    }

                    writer.Add(value);
                    self._value.Reset(value);

                    if (!writer.Build(out var outputSet))
                        return;
                    self.Forward(outputSet);

                }, RefTuple.Create(this, value));
            }
        }

        public void Update(ReadOnlySpan<T> values, int delta = 1)
        {
            var writer = new DiffSetWriter<T>();
            foreach (var value in values)
                writer.Add(value, delta);
            ApplyUpdate(ref writer);
        }

        private void ApplyUpdate(ref DiffSetWriter<T> writer)
        {
            writer.Build(out var outputSet);

            Runtime.DoSync(static t =>
            {
                var (self, deltas) = t;
                self._value.Merge(deltas);
                self.Forward(deltas);
            }, RefTuple.Create(this, outputSet));
        }

        public void Update(ReadOnlySpan<Diff<T>> diffs)
        {
            var writer = new DiffSetWriter<T>();
            writer.Add(diffs);
            ApplyUpdate(ref writer);
        }
    }
}
