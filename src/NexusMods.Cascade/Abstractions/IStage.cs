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
    public IFlowImpl Flow { get; }

    /// <summary>
    /// The output sets of this stage
    /// </summary>
    public IChangeSet[] ChangeSets { get; }

    /// <summary>
    /// Flow data into the stage from a previous stage into the given input index
    /// </summary>
    public void AcceptChanges<T>(ChangeSet<T> outputSet, int inputIndex) where T : notnull;

    /// <summary>
    /// Resets the temporary state of the outputs of the stage
    /// </summary>
    void ResetAllOutputs();
}
