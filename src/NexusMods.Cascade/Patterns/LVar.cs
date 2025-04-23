using System;
using System.Threading;

namespace NexusMods.Cascade.Pattern;

/// <summary>
/// An abstract logic variable that can be used to mark parts of a pattern
/// </summary>
public abstract class LVar : IReturnValue
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
        return !string.IsNullOrEmpty(Name) ? $"?{Name}" : $"?({Id})";
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

    public abstract Type Type { get; }
}

public class LVar<T> : LVar, IReturnValue<T>
{
    public LVar(string name) : base(name)
    {
    }

    public static LVar<T> Create(string? name = "")
    {
        var lastSpace = name?.LastIndexOf(' ') ?? -1;
        if (lastSpace != -1)
        {
            name = name![(lastSpace + 1)..];
        }
        return new LVar<T>(name!);
    }

    public override Type Type => typeof(T);

}
