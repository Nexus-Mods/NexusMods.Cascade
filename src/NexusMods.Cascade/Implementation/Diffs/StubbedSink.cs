using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class StubbedSink<T> : ASource<DiffSet<T>>, IDiffSink<T>, IDiffSource<T>, IDiffFlow<T>
{
    public override DiffSet<T> Current => DiffSet<T>.Empty;
    public void OnNext(in DiffSet<T> value)
    {
        Forward(value);
    }

    public void OnCompleted()
    {
        throw new NotSupportedException();
    }

    public ISource<DiffSet<T>> ConstructIn(ITopology topology)
    {
        return this;
    }
}
