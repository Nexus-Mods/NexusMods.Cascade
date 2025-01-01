using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IOutletDefinition : IStageDefinition
{
}

public interface IOutletDefinition<T> : IOutletDefinition
where T : notnull
{
}

public interface IOutlet : IStage
{
}

public interface IOutlet<T> : IOutlet
where T : notnull
{
}
