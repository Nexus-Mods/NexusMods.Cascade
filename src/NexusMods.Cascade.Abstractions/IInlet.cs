using System;

namespace NexusMods.Cascade.Abstractions;

public interface IInlet : IStage
{

}

public interface IInlet<T> : IInlet
where T : notnull
{
    public void AddData(ReadOnlySpan<T> input);
}

public interface IInletDefinition : IStageDefinition
{

}

public interface IInletDefinition<T> : IInletDefinition
where T : notnull
{

}
