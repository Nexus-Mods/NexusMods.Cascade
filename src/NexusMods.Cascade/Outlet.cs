using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// The implementation of an outlet.
/// </summary>
public class Outlet<T>(IOutputDefinition<T> upstreamInput)
    : AStageDefinition([(typeof(T), "results")], [], [upstreamInput]), IOutletDefinition<T>, IQuery<T>
    where T : notnull
{
    /// <summary>
    /// The outlet stage implementation.
    /// </summary>
    public new class Stage(IFlowImpl flow, IStageDefinition definition)
        : AStageDefinition.Stage(flow, definition), IOutlet<T>
    {
        private readonly ObservableResultSet<T> _results = new();

        /// <inheritdoc />
        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);

            _results.Update(((IOutputSet<T>)outputSet).GetResults());
        }

        /// <inheritdoc />
        public IReadOnlyCollection<T> Results => _results.GetResults();

        /// <inheritdoc />
        public IObservableResultSet<T> Observe() => _results;
    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    /// <inheritdoc />
    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];
}
