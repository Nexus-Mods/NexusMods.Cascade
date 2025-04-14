namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// Return data for the result of an add operation to a result set (or keyed result set).
/// </summary>
public enum OpResult
{
    /// <summary>
    /// A new entry was added to the result set.
    /// </summary>
    Added,

    /// <summary>
    /// An entry was removed from the result set.
    /// </summary>
    Removed,

    /// <summary>
    /// The delta for an entry was updated in the result set.
    /// </summary>
    Updated,
}
