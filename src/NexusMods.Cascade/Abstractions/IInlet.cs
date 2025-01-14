using System;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// An Inlet is a stage that accepts data, it is the entry point of a flow.
/// </summary>
public interface IInlet : IStage;

/// <summary>
/// A typed inlet that accepts data of type T.
/// </summary>
public interface IInlet<T> : IInlet
where T : notnull
{
    /// <summary>
    /// Add new data to the inlet, assuming that every input as a delta of the given value.
    /// </summary>
    public void Add(ReadOnlySpan<T> input, int delta = 1);

    /// <summary>
    /// Add new set of changes to the inlet.
    /// </summary>
    public void Add(ReadOnlySpan<Change<T>> input);
}

/// <summary>
/// The definition of an Inlet
/// </summary>
public interface IInletDefinition : IStageDefinition
{

}

/// <summary>
/// A typed Inlet definition
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IInletDefinition<T> : IInletDefinition
where T : notnull
{

}
