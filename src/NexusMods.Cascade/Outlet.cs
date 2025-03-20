using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// The implementation of an outlet.
/// </summary>
public class Outlet<T>(UpstreamConnection upstreamInput)
    : AStageDefinition(Inputs, [], [upstreamInput]), IOutletDefinition<T>
    where T : notnull
{
    private static readonly IInputDefinition[] Inputs = [new InputDefinition<T>("results", 0)];

    /// <summary>
    /// The outlet stage implementation.
    /// </summary>
    public new class Stage(IFlowImpl flow, IStageDefinition definition)
        : AStageDefinition.Stage(flow, definition), IOutlet<T>
    {
        private readonly ResultSetFactory<T> _results = new();
        private readonly List<Change<T>> _recentChanges = new();

        public ResultSetFactory<T> ResultSetFactory => _results;

        /// <inheritdoc />
        public override void AcceptChanges<TIn>(ChangeSet<TIn> changeSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);

            _results.Update((ChangeSet<T>)(IChangeSet)changeSet);
            _recentChanges.AddRange((ChangeSet<T>)(IChangeSet)changeSet);
        }

        public void ResetCurrentChanges()
        {
            _recentChanges.Clear();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<T> Results => _results.GetResults();


        /// <summary>
        /// Get the current state as a set of changes.
        /// </summary>
        public IEnumerable<Change<T>> CurrentChanges => _results.GetResultsAsChanges();
    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    /// <inheritdoc />
    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];
}
