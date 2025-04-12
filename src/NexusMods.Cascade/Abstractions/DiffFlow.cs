namespace NexusMods.Cascade.Abstractions;

public readonly record struct DiffFlow<T> : IDiffFlow<T> where T : notnull
{
    public DiffFlow(FlowDescription description)
    {
        Description = description;
    }
    public FlowDescription Description { get; init; } = new();

    public static implicit operator DiffFlow<T>(FlowDescription description)
    {
        return new DiffFlow<T>(description);
    }
}
