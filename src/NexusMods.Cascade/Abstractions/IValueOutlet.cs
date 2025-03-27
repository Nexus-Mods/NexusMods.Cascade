using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Abstractions;

public interface IValueOutlet<T> : IOutlet
{
    /// <summary>
    /// The value of the outlet.
    /// </summary>
    public T Value { get; }
}
