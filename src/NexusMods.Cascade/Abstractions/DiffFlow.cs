namespace NexusMods.Cascade.Abstractions;

public readonly record struct DiffFlow<T> : IDiffFlow<T> where T : notnull
{
    private readonly FlowDescription _description = new();

    public DiffFlow(FlowDescription description)
    {
        Description = description;
    }

    public FlowDescription Description
    {
        init => _description = value;
    }

    public FlowDescription AsFlow() => _description;

    public static implicit operator DiffFlow<T>(FlowDescription description)
    {
        return new DiffFlow<T>(description);
    }
}
