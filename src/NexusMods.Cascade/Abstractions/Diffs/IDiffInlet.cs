using System;

namespace NexusMods.Cascade.Abstractions.Diffs;

public interface IDiffInlet<T> : IInlet<DiffSet<T>>
{
    /// <summary>
    /// A rather inefficient way to get the values from the diff set, but exists mostly for testing and convenience.
    /// </summary>
    public T[] Values
    {
        get
        {
            var values = Value.AsSpan();
            var result = GC.AllocateUninitializedArray<T>(values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i].Value;
            }
            return result;
        }
        set
        {
            var values = new DiffSet<T>(value);
            Value = values;
        }
    }

    /// <summary>
    /// Adds new values to the inlet, assumes a delta of 1 if not specified.
    /// </summary>
    void Update(ReadOnlySpan<T> values, int delta = 1);

    void Update(params ReadOnlySpan<(T, int)> values)
    {
        var set = GC.AllocateUninitializedArray<Diff<T>>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            set[i] = new Diff<T>(values[i].Item1, values[i].Item2);
        }
        Update(set);
    }

    void Update(ReadOnlySpan<Diff<T>> diffs);
}
