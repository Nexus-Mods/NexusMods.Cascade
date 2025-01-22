using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class AStageDefinition : IStageDefinition
{
    protected AStageDefinition(IInputDefinition[] inputs, IOutputDefinition[] outputs, ReadOnlySpan<UpstreamConnection> upstreamInputs)
    {
        Inputs = inputs;
        Outputs = outputs;
        UpstreamInputs = upstreamInputs.ToArray();
    }

    /// <inheritdoc />
    public IOutputDefinition[] Outputs { get; }

    /// <inheritdoc />
    public IInputDefinition[] Inputs { get; }

    /// <inheritdoc />
    public UpstreamConnection[] UpstreamInputs { get; set; }

    /// <inheritdoc />
    public abstract IStage CreateInstance(IFlowImpl flow);


    /// <summary>
    /// The stage implementation
    /// </summary>
    public abstract class Stage : IStage
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        protected Stage(IFlowImpl flow, IStageDefinition definition)
        {
            Flow = flow;
            Definition = definition;

            ChangeSets = GC.AllocateUninitializedArray<IChangeSet>(definition.Outputs.Length);

            for (var i = 0; i < definition.Outputs.Length; i++)
            {
                ChangeSets[i] = definition.Outputs[i].CreateChangeSet();
            }
        }

        /// <inheritdoc />
        public IStageDefinition Definition { get; }

        /// <inheritdoc />
        public IFlowImpl Flow { get; }

        /// <inheritdoc />
        public IChangeSet[] ChangeSets { get; }

        /// <inheritdoc />
        public abstract void AcceptChanges<T>(ChangeSet<T> outputSet, int inputIndex) where T : notnull;

        /// <summary>
        /// Resets all outputs
        /// </summary>
        public void ResetAllOutputs()
        {
            foreach (var outputSet in ChangeSets)
                outputSet.Reset();
        }
    }
}


/// <summary>
/// A typed input definition
/// </summary>
public record InputDefinition<T> : IInputDefinition where T : notnull
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <inheritdoc />
    public int Index { get; }

    /// <summary>
    /// Flow data into the stage from a previous stage via this input
    /// </summary>
    public void AcceptChanges(IStage stage, IChangeSet changes)
    {
        if (changes is not ChangeSet<T> typedChanges)
            throw new ArgumentException("Invalid change set type", nameof(changes));

        stage.AcceptChanges(typedChanges, Index);
    }

    /// <summary>
    /// The primary constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    internal InputDefinition(string name, int index)
    {
        Name = name;
        Index = index;
    }
}

/// <summary>
/// A typed output definition
/// </summary>
/// <typeparam name="T"></typeparam>
public record OutputDefinition<T> : IOutputDefinition<T>
    where T : notnull
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <inheritdoc />
    public int Index { get; }

    /// <inheritdoc />
    public IChangeSet CreateChangeSet() => new ChangeSet<T>();

    /// <summary>
    /// The primary constructor
    /// </summary>
    public OutputDefinition(string name, int index)
    {
        Name = name;
        Index = index;
    }
}
