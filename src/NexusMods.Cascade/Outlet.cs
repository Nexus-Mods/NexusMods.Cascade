using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Outlet<T> : AStageDefinition, IOutletDefinition<T>, IQuery<T>
    where T : notnull
{

    public Outlet(IOutputDefinition<T> upstreamInput) : base([(typeof(T), "results")], [], [upstreamInput])
    {
    }

    public class Stage : AStageDefinition.Stage, IOutlet<T>
    {
        private readonly ObservableResultSet<T> _results = new();

        public Stage(IFlow flow, IStageDefinition definition) : base(flow, definition)
        {
        }

        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);

            _results.Update(((IOutputSet<T>)outputSet).GetResults());
        }

        public IReadOnlyCollection<T> GetResults()
        {
            return _results.GetResults();
        }

        public IObservableResultSet<T> ObserveResults()
        {
            return _results;
        }
    }

    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }

    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];
}
