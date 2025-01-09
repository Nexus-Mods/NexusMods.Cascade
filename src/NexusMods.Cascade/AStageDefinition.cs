using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class AStageDefinition : IStageDefinition
{
    public AStageDefinition(ReadOnlySpan<(Type Type, string Name)> inputs, ReadOnlySpan<(Type Type, string Name)> outputs, ReadOnlySpan<IOutputDefinition> upstreamInputs)
    {
        Inputs = GC.AllocateUninitializedArray<IInputDefinition>(inputs.Length);
        UpstreamInputs = upstreamInputs.ToArray();

        for (var i = 0; i < inputs.Length; i++)
        {
            Inputs[i] = (IInputDefinition)typeof(Input<>).MakeGenericType(inputs[i].Type)
                .GetConstructor([typeof(IStageDefinition), typeof(string), typeof(int)])?
                .Invoke([this, inputs[i].Name, i])!;
        }

        Outputs = GC.AllocateUninitializedArray<IOutputDefinition>(outputs.Length);

        for (var i = 0; i < outputs.Length; i++)
        {
            Outputs[i] = (IOutputDefinition)typeof(Output<>).MakeGenericType(outputs[i].Type)
                .GetConstructor([typeof(IStageDefinition), typeof(string), typeof(int)])!
                .Invoke([this, outputs[i].Name, i])!;
        }

    }

    /// <inheritdoc />
    public IOutputDefinition[] Outputs { get; }

    /// <inheritdoc />
    public IInputDefinition[] Inputs { get; }

    /// <inheritdoc />
    public IOutputDefinition[] UpstreamInputs { get; set; }

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

            OutputSets = GC.AllocateUninitializedArray<IOutputSet>(definition.Outputs.Length);

            for (var i = 0; i < definition.Outputs.Length; i++)
            {
                var type = typeof(DeduppingOutputSet<>).MakeGenericType(definition.Outputs[i].Type);
                OutputSets[i] = (IOutputSet)Activator.CreateInstance(type)!;
            }
        }

        /// <inheritdoc />
        public IStageDefinition Definition { get; }

        /// <inheritdoc />
        public IFlowImpl Flow { get; }

        /// <inheritdoc />
        public IOutputSet[] OutputSets { get; }

        /// <inheritdoc />
        public abstract void AddData(IOutputSet outputSet, int inputIndex);

        /// <summary>
        /// Resets all outputs
        /// </summary>
        public void ResetAllOutputs()
        {
            foreach (var outputSet in OutputSets)
                outputSet.Reset();
        }
    }
}


/// <summary>
/// A typed input definition
/// </summary>
public record Input<T> : IInputDefinition
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <inheritdoc />
    public int Index { get; }

    /// <summary>
    /// The primary constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    internal Input(string name, int index)
    {
        Name = name;
        Index = index;
    }
}

/// <summary>
/// A typed output definition
/// </summary>
/// <typeparam name="T"></typeparam>
public record Output<T> : IOutputDefinition<T>
    where T : notnull
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Type Type => typeof(T);

    /// <inheritdoc />
    public int Index { get; }

    /// <inheritdoc />
    public IStageDefinition Stage { get; }

    /// <summary>
    /// The primary constructor
    /// </summary>
    public Output(IStageDefinition stage, string name, int index)
    {
        Stage = stage;
        Name = name;
        Index = index;
    }
}
