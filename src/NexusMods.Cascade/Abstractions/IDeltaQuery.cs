using NexusMods.Cascade.Implementation.Delta;

namespace NexusMods.Cascade.Abstractions;

public interface IDeltaQuery<T> : IQuery<ChangeSet<T>>
{

}
