namespace NexusMods.Cascade.Abstractions;

public interface IOutlet : IStage
{

}

public interface IOutlet<T> : IOutlet
{
    public T Value { get; set; }
}
