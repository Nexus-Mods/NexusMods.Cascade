using System.Threading;

namespace NexusMods.Cascade.Rules;

/// <summary>
/// An abstract logic variable that can be used to mark parts of a pattern
/// </summary>
public class LVar
{
    private static int _nextId = 0;
    internal static int NextId() => Interlocked.Increment(ref _nextId);

    protected readonly int Id = NextId();

    /// <summary>
    /// An abstract logic variable that can be used to mark parts of a pattern
    /// </summary>
    public LVar(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public override string ToString()
    {
        return !string.IsNullOrEmpty(Name) ? $"?{Name}({Id}" : $"?({Id})";
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is LVar other)
        {
            return Id == other.Id;
        }
        return false;
    }
}

public class LVar<T> : LVar
{
    public LVar(string name) : base(name)
    {
    }
}
