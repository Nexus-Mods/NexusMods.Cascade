using NexusMods.Cascade.Implementation.Delta;

namespace NexusMods.Cascade.Abstractions;

public interface ISetInlet<T> : IInlet<ChangeSet<T>>
{

}
