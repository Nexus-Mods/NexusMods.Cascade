

using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A topology is a collection of flows that are deduped and shared between each-other. The same flow added
/// to the same topology will return the same source.
/// </summary>
public interface ITopology
{

    NodeRef Intern(FlowDescription flow);

    Inlet<T> Intern<T>(InletDefinition<T> inletDefinition) where T : notnull;

    DiffInlet<T> Intern<T>(DiffInletDefinition<T> inletDefinition) where T : notnull;


    public static ITopology Create()
    {
        return new Topology();
    }

    /// <summary>
    /// Mostly an internal method. This starts a flow of data from the given node to any connected nodes.
    /// </summary>
    void FlowFrom(Node state, object value);


    Outlet<T> Outlet<T>(IFlow<T> flow);

    DiffOutlet<T> Outlet<T>(DiffFlow<T> flow) where T : notnull;
}

