using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IOutlet : IStage
{

}

public interface IOutlet<T> : IStage
where T : notnull
{
}
