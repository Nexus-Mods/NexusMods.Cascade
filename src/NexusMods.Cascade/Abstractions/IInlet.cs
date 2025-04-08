namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// An inlet is an inbound data source
/// </summary>
public interface IInlet<T> : ISource<T> where T : allows ref struct
{
    /// <summary>
    /// Get or set the value of the inlet, setting it will cause the connected sinks to be notified.
    /// </summary>
    public T Value { get; set; }
}
