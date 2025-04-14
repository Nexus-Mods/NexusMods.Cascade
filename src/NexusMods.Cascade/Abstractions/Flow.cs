namespace NexusMods.Cascade.Abstractions;

public readonly record struct Flow<T> : IFlow<T>
{
    private readonly FlowDescription _description = new();

    public Flow(FlowDescription description)
    {
        Description = description;
    }

    public FlowDescription Description
    {
        init => _description = value;
    }

    public FlowDescription AsFlow() => _description;

    public static implicit operator Flow<T>(FlowDescription description)
    {
        return new Flow<T>(description);
    }
}
