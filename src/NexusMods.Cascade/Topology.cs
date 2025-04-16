using System.Collections.Generic;
using System.Threading;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public sealed class Topology
{
    /// <summary>
    /// The global lock for the topology, only one thread can be in the topology at a time.
    /// </summary>
    internal readonly Lock Lock = new();

    private readonly Dictionary<int, Node> _nodes = new();
    private readonly Dictionary<int, Node> _outletNodes = new();
    private Queue<Node> _queue = new();


    public InletNode<T> Intern<T>(Inlet<T> inlet) where T : notnull
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(inlet.Id, out var node))
            {
                return (InletNode<T>)node;
            }

            var inletNode = new InletNode<T>(this, inlet);
            _nodes[inlet.Id] = inletNode;
            return inletNode;
        }
    }

    private Node Intern(Flow flow)
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(flow.Id, out var node))
            {
                return node;
            }

            node = flow.CreateNode(this);
            for (var idx = 0; idx < flow.Upstream.Length; idx++)
            {
                var upstream = Intern(flow.Upstream[idx]);
                upstream.Subscribers.Add((node, idx));
                node.Upstream[idx] = upstream;
            }
            _nodes[flow.Id] = node;
            return node;
        }
    }

    public OutletNode<T> Outlet<T>(Flow<T> flow) where T : notnull
    {
        lock (Lock)
        {
            if (_outletNodes.TryGetValue(flow.Id, out var node))
            {
                return (OutletNode<T>)node;
            }

            var upstream = (Node<T>)Intern(flow);

            var outletNode = new OutletNode<T>(this, flow)
            {
                Upstream =
                {
                    [0] = upstream
                }
            };
            upstream.Subscribers.Add((outletNode, 0));

            upstream.Prime();
            outletNode.Accept(0, upstream.OutputSet);
            upstream.ResetOutput();

            _outletNodes[flow.Id] = outletNode;
            return outletNode;
        }
    }

    public void FlowFrom<T>(InletNode<T> inletNode) where T : notnull
    {
        _queue.Clear();
        _queue.Enqueue(inletNode);
        FlowFromCore();
    }

    private void FlowFromCore()
    {
        while (_queue.Count > 0)
        {
            var currentNode = _queue.Dequeue();
            foreach (var (subscriber, idx) in currentNode.Subscribers)
            {
                currentNode.FlowOut(_queue, subscriber, idx);
            }
        }
    }
}
