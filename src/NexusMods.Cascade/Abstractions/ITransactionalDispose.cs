namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A disposable object that must be called inside a transaction.
/// </summary>
public interface ITransactionalDispose
{
    /// <summary>
    /// Dispose of the given resource.
    /// </summary>
    public void Dispose();
}
