using System;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A instance of a stage in a flow
/// </summary>
public interface IStage
{
    /// <summary>
    /// The definition stage this stage is based on.
    /// </summary>
    public IStageDefinition Definition { get; }

    /// <summary>
    /// The flow this stage is part of
    /// </summary>
    public IFlow Flow { get; }

    /// <summary>
    /// Accept a change from the input index
    /// </summary>
    public void AcceptChange<TDelta>(int inputIndex, TDelta delta);
}


public interface IStage<out T> : IStage
{
    public T CurrentValue { get; }
}
