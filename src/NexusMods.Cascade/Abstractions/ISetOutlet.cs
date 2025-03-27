using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

public interface ISetOutlet<T> : IOutlet
{
    public ImmutableHashSet<T> Value { get; }
}
