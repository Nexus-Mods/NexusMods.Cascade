using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class StubbedSource<T> : IDiffSource<T>, IDiffSink<T>
{
    public ResultSet<T> ResultSet { get; } = new();

    public IDisposable Connect(ISink<DiffSet<T>> sink)
    {
        throw new NotSupportedException("This is a stubbed source and does not support connecting sinks.");
    }

    public DiffSet<T> Current => ResultSet.AsDiffSet();
    public void OnNext(in DiffSet<T> value)
    {
        ResultSet.Merge(value);
    }

    public void OnCompleted()
    {
        // No-op for stubbed source
    }
}
