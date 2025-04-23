namespace NexusMods.Cascade;

public record DebugInfo
{
    public string Name { get; init; } = string.Empty;
    public string Expression { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }

    /// <summary>
    /// A shape used when visualizing the flow in a graph.
    /// </summary>
    public Shape FlowShape { get; init; } = Shape.Rect;

    public enum Shape
    {
        Rect,
        Processes,
        Circle,
        Dbl_Circ,
        Trap_T,
        Trap_B,
        Document,
        Lean_R,
    }
}
