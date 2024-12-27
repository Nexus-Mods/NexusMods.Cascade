﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class HashJoin<TLeft, TRight, TKey, TOutput> : Join<TLeft, TRight, TOutput>
    where TLeft : notnull
    where TRight : notnull
    where TOutput : notnull
    where TKey : notnull
{
    private readonly Dictionary<TKey, Dictionary<TLeft, int>> _left = new();
    private readonly Dictionary<TKey, Dictionary<TRight, int>> _right = new();
    private readonly Func<TLeft,TKey> _leftKeySelector;
    private readonly Func<TRight,TKey> _rightKeySelector;

    public HashJoin(Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector)
    {
        _leftKeySelector = leftKeySelector;
        _rightKeySelector = rightKeySelector;
    }

    protected override void ProcessLeft(IOutputSet<TLeft> data, IOutputSet<TOutput> outputSet)
    {
        foreach (var itm in data.GetResults())
        {
            var joinKey = _leftKeySelector(itm.Key);
            ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_left, joinKey, out var exists);
            found ??= new Dictionary<TLeft, int>();

            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(found!, itm.Key, out _);
            delta += itm.Value;

            if (delta == 0)
                found.Remove(itm.Key);

            if (found.Count == 0)
                _left.Remove(joinKey);
        }
    }

    protected override void ProcessRight(IOutputSet<TRight> data, IOutputSet<TOutput> outputSet)
    {

        foreach (var itm in data.GetResults())
        {
            var joinKey = _rightKeySelector(itm.Key);
            ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_right, joinKey, out var exists);
            found ??= new Dictionary<TRight, int>();

            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(found!, itm.Key, out _);
            delta += itm.Value;

            if (delta == 0)
                found.Remove(itm.Key);

            if (found.Count == 0)
                _right.Remove(joinKey);
        }

    }

}
