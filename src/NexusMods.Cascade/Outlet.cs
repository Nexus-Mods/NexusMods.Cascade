using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// The implementation of an outlet.
/// </summary>
public abstract class Outlet<T>(UpstreamConnection upstreamInput)
    : AStageDefinition(Inputs, [], [upstreamInput])
    where T : notnull
{
    private static readonly IInputDefinition[] Inputs = [new InputDefinition<T>("results", 0)];

    /// <summary>
    /// The outlet stage implementation.
    /// </summary>
    public abstract class Stage(IFlowImpl flow, IStageDefinition definition)
        : AStageDefinition.Stage(flow, definition), IOutlet<T>
    {
        private readonly ChangeSet<T> _pendingChanges = [];

        /// <inheritdoc />
        public override void AcceptChanges<TIn>(ChangeSet<TIn> changeSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);

            // Merge in the changes
            _pendingChanges.Add((ChangeSet<T>)(IChangeSet)changeSet);
        }

        /// <inheritdoc />
        public void ReleasePendingSends()
        {
            if (_pendingChanges.Count == 0)
                return;

            ReleaseChanges(_pendingChanges);
            _pendingChanges.Reset();
        }

        /// <summary>
        /// Override this method to release the changes outside the flow
        /// </summary>
        protected abstract void ReleaseChanges(ChangeSet<T> changeSet);

    }

    /// <inheritdoc />
    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];
}
