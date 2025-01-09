using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;


public interface IOutputSet
{
    void Reset();
}

/// <summary>
/// A result set for a stage.
/// </summary>
public interface IOutputSet<T> : IOutputSet
where T : notnull
{
    /// <summary>
    /// Adds a value to the output set, results will be auto deduplicated
    /// </summary>
    void Add(in T value);

    void Add(in KeyValuePair<T, int> valueAndDelta);

    void Add(in T value, int delta);

    IEnumerable<KeyValuePair<T, int>> GetResults();
}
