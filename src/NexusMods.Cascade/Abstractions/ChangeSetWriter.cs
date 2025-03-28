using System;
using System.Buffers;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Reloaded.Memory.Extensions;

namespace NexusMods.Cascade.Abstractions;

public ref struct ChangeSetWriter<T> : IDisposable where T : notnull
{
    private const int InitialSize = 16;

    [MustDisposeResource]
    public static ChangeSetWriter<T> Create() => new();

    public ChangeSetWriter()
    {
        _overflowBuffer = MemoryPool<Change<T>>.Shared.Rent(InitialSize);
        _bufferSpan = _overflowBuffer.Memory.Span;
        _count = 0;
    }

    private Span<Change<T>> _bufferSpan;
    private IMemoryOwner<Change<T>>? _overflowBuffer = null;

    private int _count;

    public void Write(in T Value, int delta)
    {
        if (_count >= _bufferSpan.Length)
        {
            if (_overflowBuffer == null)
            {
                _overflowBuffer = MemoryPool<Change<T>>.Shared.Rent(_bufferSpan.Length * 2);
                _bufferSpan.CopyTo(_overflowBuffer.Memory.Span);
                _bufferSpan = _overflowBuffer.Memory.Span;
            }
            else
            {
                var oldBuffer = _overflowBuffer;
                _overflowBuffer = MemoryPool<Change<T>>.Shared.Rent(oldBuffer.Memory.Length * 2);
                _bufferSpan.CopyTo(_overflowBuffer.Memory.Span);
                _bufferSpan = _overflowBuffer.Memory.Span;
                oldBuffer.Dispose();
            }
        }
        _bufferSpan[_count++] = new Change<T>(Value, delta);
    }

    public void ForwardAll(ReadOnlySpan<(IStage Stage, int Port)> receivers)
    {
        var changeSet = new ChangeSet<T>(_bufferSpan.SliceFast(0, _count));
        foreach (var (stage, port) in receivers)
            stage.AcceptChange(port, changeSet);
    }

    public void Dispose()
    {
        _overflowBuffer?.Dispose();
    }

    public void Add(int delta, ReadOnlySpan<T> values)
    {
        foreach (var value in values)
            Write(value, delta);
    }

    public ReadOnlySpan<Change<T>> AsSpan()
    {
        return _bufferSpan.SliceFast(0, _count);
    }

    public void Add(ImmutableDictionary<T,int> stateValue)
    {
        foreach (var (key, value) in stateValue)
            Write(key, value);
    }

    public ImmutableDictionary<T,int> ToImmutableDictionary()
    {
        if (_count == 0)
            return ImmutableDictionary<T, int>.Empty;

        var builder = ImmutableDictionary.CreateBuilder<T, int>();
        foreach (var (value, delta) in AsSpan())
        {
            if (builder.TryGetValue(value, out var current))
            {
                if (current + delta == 0)
                    builder.Remove(value);
                else
                    builder[value] = current + delta;
            }
            else
                builder.Add(value, delta);
        }
        return builder.ToImmutable();
    }

    public ChangeSet<T> ToChangeSet()
    {
        return new ChangeSet<T>(AsSpan());
    }
}
