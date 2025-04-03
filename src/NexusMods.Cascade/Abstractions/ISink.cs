namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A sink for data, most often handed to a source to receive data.
/// </summary>
public interface ISink
{
    /// <summary>
    /// Called when the source has will no longer emit values.
    /// </summary>
    void OnCompleted();
}

/// <summary>
/// A sink for data, most often handed to a source to receive data.
/// </summary>
public interface ISink<T> : ISink
{
    /// <summary>
    /// Accepts a new value from the source. This method is called when the source has a new value to emit.
    /// </summary>
    void OnNext(in T value);
}
