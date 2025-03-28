using System;
using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A instance of a stage in a flow
/// </summary>
public interface IStage
{
    /// <summary>
    /// The stages connected to the inputs of this stage
    /// </summary>
    public ReadOnlySpan<IStage> Inputs { get; }

    /// <summary>
    /// The stages connected to the outputs of this stage
    /// </summary>
    public ReadOnlySpan<(IStage Stage, int Index)> Outputs { get; }

    /// <summary>
    /// Connect a stage to the output of this stage and tag it with the given index
    /// </summary>
    void ConnectOutput(IStage stage, int index);

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
    public void AcceptChange<T>(int inputIndex, in ChangeSet<T> delta) where T : notnull;
}


public interface IStage<T> : IStage where T : notnull
{
    public void WriteCurrentValues(ref ChangeSetWriter<T> writer);
}
