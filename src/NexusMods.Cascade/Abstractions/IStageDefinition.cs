using System;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A definition of a stage that may later be instantiated
/// </summary>
public interface IStageDefinition
{
    /// <summary>
    /// Creates a new instance of the stage that will be attached to the flow, must be called
    /// inside a transaction
    /// </summary>
    public IStage CreateInstance(IFlow flow);
}

/// <summary>
/// A typed stage definition that produces values of the given type
/// </summary>
public interface IStageDefinition<T> : IStageDefinition
{

}
