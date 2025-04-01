namespace NexusMods.Cascade.Collections;

public readonly record struct Change<T>(T Value, int Delta)
{
    public void Deconstruct(out T value, out int change)
    {
        value = Value;
        change = Delta;
    }
}
