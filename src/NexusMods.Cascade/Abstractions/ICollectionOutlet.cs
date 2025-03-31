using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

public interface ICollectionOutlet<T> : IOutlet where T : notnull
{
    /// <summary>
    /// The value of the outlet.
    /// </summary>
    public ImmutableDictionary<T, int> Values { get; }
}
