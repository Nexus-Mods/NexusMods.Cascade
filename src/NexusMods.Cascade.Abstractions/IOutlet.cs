using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IOutletDefinition : IStageDefinition, ISingleOutputStageDefinition
{
}

public interface IOutletDefinition<T> : IOutletDefinition, ISingleOutputStageDefinition<T>
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
