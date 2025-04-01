using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Implementation;

/// <summary>
/// Represents an abstract base class for a stage in a flow. The class defines the core behavior
/// and connections of a stage for handling inputs, outputs, and definition compliance.
/// </summary>
/// <typeparam name="TResult">
/// The type of the result that the stage produces. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TDefinition">
/// The type of the definition that describes this stage. Must implement <see cref="IStageDefinition{TResult}"/>.
/// </typeparam>
public abstract class AStage<TResult, TDefinition> : IStage<TResult>
    where TResult : IComparable<TResult>
    where TDefinition : IStageDefinition<TResult>
{
    protected readonly Ref<ImmutableArray<(IStage, int)>> _outputs = new(ImmutableArray<(IStage, int)>.Empty);
    protected readonly TDefinition _definition;
    internal readonly Flow _flow;

    protected AStage(TDefinition definition, IFlow flow)
    {
        _flow = (Flow)flow;
        _definition = definition;
        _flow.AddStageInstance(definition, this);
    }

    public void ConnectOutput(IStage stage, int index)
        => _outputs.Value = _outputs.Value.Add((stage, index));

    public IStageDefinition Definition => _definition;
    public IFlow Flow => _flow;

    public abstract void WriteCurrentValues(ref ChangeSetWriter<TResult> writer);

    public abstract ReadOnlySpan<IStage> Inputs { get; }

    public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();
    public abstract void AcceptChange<T>(int inputIndex, in ChangeSet<T> delta) where T : IComparable<T>;
    public void Complete(int inputIndex)
    {
        Debug.Assert(inputIndex == 0);
        foreach (var (stage, index) in _outputs.Value.AsSpan())
        {
            stage.Complete(index);
        }
    }
}
