using System;
using System.Buffers;
using NexusMods.Cascade.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.Cascade.Collections
{
    /// <summary>
    /// A builder for a change set. This is a mutable data structure.
    /// Items are collected unsorted then, on finalization via ToSpan(),
    /// they are sorted by Value and duplicate changes are collapsed (the delta values summed).
    /// If the sum is zero, that item is omitted.
    /// </summary>
    public struct ChangeSetBuilder<T> : IDisposable where T : notnull
    {
        private const int DefaultCapacity = 32;
        private IMemoryOwner<Change<T>>? _owner;
        private int _count;

        /// <summary>
        /// Adds a change with the specified value and delta.
        /// Changes are simply appended; they will be sorted
        /// and collapsed when ToSpan() is called.
        /// </summary>
        /// <param name="value">The value associated with the change.</param>
        /// <param name="delta">The delta (change amount) for this value.</param>
        public void Add(T value, int delta)
        {
            if (_owner is null)
            {
                _owner = MemoryPool<Change<T>>.Shared.Rent(DefaultCapacity);
            }

            var span = _owner.Memory.Span;
            if (_count == span.Length)
            {
                // Double the capacity.
                var newCapacity = span.Length * 2;
                var newOwner = MemoryPool<Change<T>>.Shared.Rent(newCapacity);
                var newSpan = newOwner.Memory.Span;
                span.SliceFast(0, _count).CopyTo(newSpan);
                _owner.Dispose();
                _owner = newOwner;
                span = newSpan;
            }

            span[_count] = new Change<T>(value, delta);
            _count++;
        }

        /// <summary>
        /// Finalizes and returns a read-only span of changes.
        /// This method sorts the changes by their Value,
        /// then collapses duplicate entries (by summing their deltas).
        /// If the resulting delta is zero, that value is omitted.
        /// </summary>
        /// <returns>A ReadOnlySpan of collapsed changes.</returns>
        public ReadOnlySpan<Change<T>> ToSpan()
        {
            if (_owner is null)
                return ReadOnlySpan<Change<T>>.Empty;

            var span = _owner.Memory.Span.Slice(0, _count);

            // Batch sort the changes by Value.
            span.Sort();

            // Collapse duplicate entries by summing their deltas.
            int writeIndex = 0;
            int readIndex = 0;
            while (readIndex < _count)
            {
                var currentValue = span[readIndex].Value;
                int combinedDelta = 0;
                // Sum all deltas for entries with the same Value.
                while (readIndex < _count && span[readIndex].Value.Equals(currentValue))
                {
                    combinedDelta += span[readIndex].Delta;
                    readIndex++;
                }

                // Only keep the item if the combined delta is non-zero.
                if (combinedDelta != 0)
                {
                    span[writeIndex] = new Change<T>(currentValue, combinedDelta);
                    writeIndex++;
                }
            }

            // Update the internal count to reflect the collapsed items.
            _count = writeIndex;
            return span.SliceFast(0, writeIndex);
        }

        /// <summary>
        /// Returns the rented memory to the MemoryPool.
        /// </summary>
        public void Dispose()
        {
            _owner?.Dispose();
            _owner = null;
            _count = 0;
        }
    }
}
