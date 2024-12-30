namespace NexusMods.Cascade.Abstractions;

public interface ISingleOutputStage
{

}

public interface ISingleOutputStage<T> : ISingleOutputStage
    where T : notnull
{
    public IOutput<T> Output { get; }
}
