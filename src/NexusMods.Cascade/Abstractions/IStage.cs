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
    public IOutputSet[] OutputSets { get; }

    /// <summary>
    /// Flow data into the stage from a previous stage into the given input index
    /// </summary>
    public void AddData(IOutputSet outputSet, int inputIndex);

    /// <summary>
    /// Resets the temporary state of the outputs of the stage
    /// </summary>
    void ResetAllOutputs();
}
