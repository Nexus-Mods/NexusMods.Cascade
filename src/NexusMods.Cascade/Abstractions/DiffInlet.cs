using System.Collections.Generic;
using Clarp;

namespace NexusMods.Cascade.Abstractions;

public class DiffInlet<T> where T : notnull
{
    private readonly NodeRef _ref;
    public DiffInlet(NodeRef inletRef)
    {
        _ref = inletRef;
    }

    /// <summary>
    /// Gets or sets the value of the inlet. Setting the value will cause the flow to be updated
    /// to reflect the new values. When the set returns, the flow will be updated.
    /// </summary>
    public IEnumerable<T> Values
    {
        get => ((ResultSet<T>)_ref.Value.UserState!).Values;
        set
        {
            Runtime.DoSync(static s =>
            {
                var (self, value) = s;
                var oldState = self._ref.Value;
                var newSet = new DiffSet<T>(value);
                newSet.MergeIn((ResultSet<T>)oldState.UserState!);
                self._ref.Value = oldState with { UserState = newSet.ToResultSet() };
                oldState.Topology.FlowFrom(oldState, newSet);
            }, (this, value));
        }
    }

}
