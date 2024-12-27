using System;

namespace NexusMods.Cascade.Abstractions;

public interface IInlet : IStage
{

}

public interface IInlet<T> : IStage
where T : notnull
{
    public void AddData<TOutput>(ReadOnlySpan<T> input, TOutput output)
        where TOutput : IOutputSet<T>;
}
