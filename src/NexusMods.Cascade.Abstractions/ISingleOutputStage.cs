using System.Linq;

namespace NexusMods.Cascade.Abstractions;

public interface ISingleOutputStageDefinition : IStageDefinition
{

}

public interface ISingleOutputStageDefinition<T> : ISingleOutputStageDefinition
    where T : notnull
{
    public IOutputDefinition<T> Output { get; }
}
