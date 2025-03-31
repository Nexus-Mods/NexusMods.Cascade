namespace NexusMods.Cascade.Abstractions;

public interface IValueOutlet<T> : IOutlet where T : notnull
{
    public T Value { get; }
}
