using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IObservableResultSet<T> : IEnumerable<T>
    where T : notnull
{
}
