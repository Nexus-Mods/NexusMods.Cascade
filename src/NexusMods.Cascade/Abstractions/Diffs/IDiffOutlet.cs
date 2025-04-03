using System;

namespace NexusMods.Cascade.Abstractions.Diffs;

public interface IDiffOutlet<T> : IOutlet<DiffSet<T>>
{
    /// <summary>
    /// Rather inefficient way to get the values from the diff set, but exists mostly for testing and convenience.
    /// </summary>
    public T[] Values
    {
        get
        {
            var span = Value.AsSpan();
            var result = GC.AllocateUninitializedArray<T>(span.Length);
            for (var i = 0; i < span.Length; i++)
            {
                result[i] = span[i].Value;
            }
            return result;
        }
    }

}
