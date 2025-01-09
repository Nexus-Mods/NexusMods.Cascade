namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// Some stages may be able to recreate their state efficiently based on internal data, this interface allows for
/// that state to be spooled into the outputs of the stage
/// </summary>
public interface IHasSnapshot
{
    /// <summary>
    /// Spools the snapshot of the state into the outputs of the stage
    /// </summary>
    void OutputSnapshot();
}
