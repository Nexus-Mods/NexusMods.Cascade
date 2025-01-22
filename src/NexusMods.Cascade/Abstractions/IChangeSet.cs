using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;


/// <summary>
/// An interface for temporary storage of output values from a stage
/// </summary>
public interface IChangeSet
{
    /// <summary>
    /// Clear the output set
    /// </summary>
    void Reset();
}

/// <summary>
/// A result set for a stage.
/// </summary>
public interface IChangeSet<T> : IChangeSet, IReadOnlyCollection<Change<T>>
where T : notnull
{
    /// <summary>
    /// Add a change to the output set
    /// </summary>
    /// <param name="change"></param>
    void Add(Change<T> change);
}
