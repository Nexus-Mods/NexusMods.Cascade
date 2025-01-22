namespace NexusMods.Cascade;

/// <summary>
/// A pairing of a Value with a change delta (positive or negative integer)
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly record struct Change<T>(T Value, int Delta)
    where T : notnull;
