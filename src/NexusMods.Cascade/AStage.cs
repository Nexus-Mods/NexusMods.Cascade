using System;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class AStage : IStage
{
    public AStage(ReadOnlySpan<(Type Type, string Name)> inputs, ReadOnlySpan<(Type Type, string Name)> outputs)
    {
        Inputs = GC.AllocateUninitializedArray<IInput>(inputs.Length);

        for (var i = 0; i < inputs.Length; i++)
        {
            Inputs[i] = (IInput)typeof(Input<>).MakeGenericType(inputs[i].Type)
                .GetConstructor([typeof(string), typeof(int)])?
                .Invoke([inputs[i].Name, i])!;
        }

        Outputs = GC.AllocateUninitializedArray<IOutput>(outputs.Length);

        for (var i = 0; i < outputs.Length; i++)
        {
            Outputs[i] = (IOutput)typeof(Output<>).MakeGenericType(outputs[i].Type)
                .GetConstructor([typeof(string), typeof(int)])?
                .Invoke([outputs[i].Name, i])!;
        }

    }

    public abstract void AddData(IOutputSet data, int index);

    public IOutput[] Outputs { get; set; }

    public IInput[] Inputs { get; set; }
}


public record Input<T> : IInput
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

public record Output<T> : IOutput
    where T : notnull
{
    public string Name { get; }
    public Type Type => typeof(T);
    public int Index { get; }

    internal IOutputSet<T> OutputSet { get; set; } = new DeduppingOutputSet<T>();

    public Output(string name, int index)
    {
        Name = name;
        Index = index;
    }
}
