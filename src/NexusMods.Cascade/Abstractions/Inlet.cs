using System.Diagnostics;
using Clarp;

namespace NexusMods.Cascade.Abstractions;

public class Inlet<T>
{
    private readonly NodeRef _ref;
    public Inlet(NodeRef inletRef)
    {
        _ref = inletRef;
    }

    /// <summary>
    /// Gets or sets the value of the inlet. Setting the value will cause the flow to be updated
    /// to reflect the new values. When the set returns, the flow will be updated.
    /// </summary>
    public T Value
    {
        get => (T)_ref.Value.UserState!;
        set
        {
            Runtime.DoSync(static s =>
            {
                var (self, value) = s;
                var oldState = self._ref.Value;
                self._ref.Value = oldState with { UserState = value };
                oldState.Topology.FlowFrom(oldState, value!);
            }, (this, value));
        }
    }

}
