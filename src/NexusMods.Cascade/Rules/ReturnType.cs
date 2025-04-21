namespace NexusMods.Cascade.Rules;

public struct ReturnType<T>
{
    public LVar LVar { get; init; }

    public AggregateOp AggregateOp { get; init; }
}
