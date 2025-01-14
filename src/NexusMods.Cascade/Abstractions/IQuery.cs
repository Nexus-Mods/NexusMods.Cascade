using System.Linq;

namespace NexusMods.Cascade.Abstractions;

public interface IQuery : IStageDefinition
{

}

public interface IQuery<T> : IQuery
    where T : notnull
{
    public IOutputDefinition<T> Output { get; }

    /// <summary>
    /// Create a upstream connection to this query
    /// </summary>
    /// <returns></returns>
    public UpstreamConnection ToUpstreamConnection() => new(this, Output);
}
