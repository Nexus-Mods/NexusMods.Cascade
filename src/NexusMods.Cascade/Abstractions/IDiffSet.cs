using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IDiffSet<T> : IEnumerable<Diff<T>>;
