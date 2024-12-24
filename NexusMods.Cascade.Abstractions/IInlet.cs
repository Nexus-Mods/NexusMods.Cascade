namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A data inlet stage in the flow
/// </summary>
public interface IInlet : IStage
{

}

public interface IInlet<T> : IInlet, IStage<T>
{

}
