﻿using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A filter stage definition, this applies a predicate to each value filtering out those that do not match.
/// </summary>
public class Filter<T> : AUnaryStageDefinition<T, T>
    where T : notnull
{

    /// <summary>
    /// Primary constructor.
    /// </summary>
    public Filter(Func<T, bool> func, IOutputDefinition upstreamInput) : base(upstreamInput) {
        _func = func;
    }

    private readonly Func<T, bool> _func;

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private new class Stage(IFlowImpl flow, Filter<T> definition) : AUnaryStageDefinition<T, T>.Stage(flow, definition) {
        protected override void Process(IOutputSet<T> input, IOutputSet<T> output)
        {
            foreach (var kvp in input.GetResults())
            {
                if (definition._func(kvp.Key))
                {
                    output.Add(kvp);
                }
            }
        }
    }
}
