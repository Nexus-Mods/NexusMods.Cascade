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
                .GetConstructor([typeof(IStageDefinition), typeof(string), typeof(int)])?
                .Invoke([this, outputs[i].Name, i])!;
        }

    }

    public IOutputDefinition[] Outputs { get; set; }

    public IInputDefinition[] Inputs { get; set; }

    public IOutputDefinition[] UpstreamInputs { get; set; }

    public abstract IStage CreateInstance(IFlow flow);


    public abstract class Stage : IStage
    {
        public Stage(IFlow flow, IStageDefinition definition)
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
        public IFlow Flow { get; }

        /// <inheritdoc />
        public IOutputSet[] OutputSets { get; }

        /// <inheritdoc />
        public abstract void AddData(IOutputSet outputSet, int inputIndex);

        public void ResetAllOutputs()
        {
            foreach (var outputSet in OutputSets)
                outputSet.Reset();
        }
    }
}


public record Input<T> : IInputDefinition
{
    public string Name { get; }
    public Type Type => typeof(T);
    public int Index { get; }

    public Input(string name, int index)
    {
        Name = name;
        Index = index;
    }
}

public record Output<T> : IOutputDefinition<T>
    where T : notnull
{
    public string Name { get; }
    public Type Type => typeof(T);
    public int Index { get; }

    public IStageDefinition Stage { get; }

    public Output(IStageDefinition stage, string name, int index)
    {
        Stage = stage;
        Name = name;
        Index = index;
    }
}
