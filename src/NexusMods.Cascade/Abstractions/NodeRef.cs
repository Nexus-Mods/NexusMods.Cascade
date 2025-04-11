using Clarp.Concurrency;

namespace NexusMods.Cascade.Abstractions;

public class NodeRef : Ref<Node>
{
    public NodeRef(Node node) : base(node)
    {
    }

    /// <summary>
    /// Add the given sink to the list of subscribers for this node, with the given tag.
    /// </summary>
    public void Connect(NodeRef sink, int tag)
    {
        Value = Value with { Subscribers = Value.Subscribers.Add((sink, tag)) };
    }
}
