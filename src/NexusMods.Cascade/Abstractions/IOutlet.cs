namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// An outlet for data, most often handed to a source to get data out of a topology.
/// </summary>
public interface IOutlet
{

}

public interface IOutlet<out T> : IOutlet where T : allows ref struct
{
    /// <summary>
    /// Get the current value of the outlet.
    /// </summary>
    public T Value { get; }
}
