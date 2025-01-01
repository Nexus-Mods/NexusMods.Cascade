using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IOutlet : IStageDefinition
{
}

public interface IOutlet<T> : IStageDefinition
where T : notnull
{
}
