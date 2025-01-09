namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A tuple-like struct for a stage and its output
/// </summary>
public readonly record struct StageOutputMarker<T>(IOutlet<T> outLet, int outputIndex)
    where T : notnull
{

}
