using System;

namespace NexusMods.Cascade.Rules;

public abstract class LVar
{
    public abstract Type Type { get; }

    public string Name { get; protected set; } = "<unnamed>";
}

public class LVar<T> : LVar
{
    public LVar(string? name = null)
    {
        Name = name ?? "<unnamed>";
    }

    public override Type Type => typeof(T);

}
