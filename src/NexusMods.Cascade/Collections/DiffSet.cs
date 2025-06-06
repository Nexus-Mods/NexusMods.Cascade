﻿using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusMods.Cascade.Collections;

public class DiffSet<T> : Dictionary<T, int> where T : notnull
{
    public void MergeIn<TEnum>(TEnum items) where TEnum : IEnumerable<KeyValuePair<T, int>>
    {
        foreach (var (value, delta) in items)
        {
            ref var currentDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out var exists);
            currentDelta += delta;
            if (currentDelta == 0) Remove(value);
        }
    }

    public void MergeIn(IToDiffSpan<T> items)
    {
        foreach (var (value, delta) in items.ToDiffSpan())
        {
            ref var currentDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out var exists);
            currentDelta += delta;
            if (currentDelta == 0) Remove(value);
        }
    }

    public void MergeInInverted<TEnum>(TEnum items) where TEnum : IEnumerable<KeyValuePair<T, int>>
    {
        foreach (var (value, delta) in items)
        {
            ref var currentDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out var exists);
            currentDelta -= delta;
            if (currentDelta == 0) Remove(value);
        }
    }


    public void MergeIn(T[] value, int i)
    {
        foreach (var item in value)
            Update(item, i);
    }

    public void SetTo(DiffSet<T> state)
    {
        Clear();
        foreach (var (value, delta) in state) Add(value, delta);
    }

    public void Update(T item, int delta)
    {
        ref var currentDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, item, out var exists);
        currentDelta += delta;
        if (currentDelta == 0) Remove(item);
    }

    public void Reset(T[] value)
    {
        Clear();
        foreach (var item in value)
        {
            ref var currentDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, item, out var exists);
            currentDelta += 1; // assume delta is 1
        }
    }
}
