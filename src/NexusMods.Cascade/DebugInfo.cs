namespace NexusMods.Cascade.Abstractions2;

public record DebugInfo
{
    public string Name { get; init; } = string.Empty;
    public string Expression { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
}
