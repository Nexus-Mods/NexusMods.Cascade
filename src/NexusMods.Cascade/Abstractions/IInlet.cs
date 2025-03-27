namespace NexusMods.Cascade.Abstractions;

public interface IInlet<T>
{
    /// <summary>
    /// Push a value into the inlet.
    /// </summary>
    public void Push(in T value);
}
