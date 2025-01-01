using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Filter<T> : AUnaryStageDefinition<T, T>
    where T : notnull
{
    public Filter(Func<T, bool> func, IOutputDefinition upstreamInput) : base(upstreamInput) {
        _func = func;
    }

    private readonly Func<T, bool> _func;

    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }

    public class Stage(IFlow flow, Filter<T> definition) : AUnaryStageDefinition<T, T>.Stage(flow, definition) {
        /// <inheritdoc />
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
