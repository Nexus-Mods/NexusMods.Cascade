using System;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A definition of a stage that may later be instantiated
/// </summary>
public interface IStageDefinition
{
    /// <summary>
    /// The inputs that this stage requires
    /// </summary>
    public IInputDefinition[] Inputs { get; }

    /// <summary>
    /// The outputs that this stage will produce
    /// </summary>
    public IOutputDefinition[] Outputs { get; }

    /// <summary>
    /// The upstream inputs that this stage requires
    /// </summary>
    public UpstreamConnection[] UpstreamInputs { get; }

    /// <summary>
    /// Creates a new instance of the stage that will be attached to the flow
    /// </summary>
    public IStage CreateInstance(IFlowImpl flow);
}

public interface IInputDefinition
{
    /// <summary>
    /// The name of the input
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the input
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The index of the input
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Accept untyped changes into this input
    /// </summary>
    public void AcceptChanges(IStage stage, IChangeSet changes);
}

public interface IInputDefinition<T> : IInputDefinition
    where T : notnull
{

}

public interface IOutputDefinition
{
    public string Name { get; }

    public Type Type { get; }

    public int Index { get; }

    /// <summary>
    /// Create a new ChangeSet for this output definition
    /// </summary>
    /// <returns></returns>
    public IChangeSet CreateChangeSet();

    public void AcceptChanges<T>(IStage stage, int inputIndex, ChangeSet<T> changes) where T : notnull
    {
        stage.Definition.Inputs[inputIndex].AcceptChanges(stage, changes);

    }
}

public interface IOutputDefinition<T> : IOutputDefinition
    where T : notnull
{
}
