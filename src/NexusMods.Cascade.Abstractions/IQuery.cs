using System.Linq;

namespace NexusMods.Cascade.Abstractions;

public interface IQuery : IStageDefinition
{

}

public interface IQuery<T> : IQuery
    where T : notnull
{
    public IOutputDefinition<T> Output { get; }
}
