namespace NexusMods.Cascade.Abstractions;

public readonly record struct Flow<T>
{
    public Flow(FlowDescription description)
    {
        Description = description;
    }
    public FlowDescription Description { get; init; } = new();

    public static implicit operator Flow<T>(FlowDescription description)
    {
        return new Flow<T>(description);
    }
}
