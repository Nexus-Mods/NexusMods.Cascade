using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace NexusMods.Cascade;

public sealed class Topology
{
    private readonly HashSet<Node> _inlets = [];

    /// <summary>
    /// The nodes, all sorted by the processing order. Whenever the flow is run, the
    /// nodes are processed in this order.
    /// </summary>
    private readonly List<Node> _processOrder = [];

    /// <summary>
    /// A temporary queue used to process the order of nodes in the topology.
    /// </summary>
    private readonly Queue<Node> _tempSortOrder = new();

    /// <summary>
    /// While processing the flow some nodes will have data in their outputs, we track those nodes here,
    /// and reset the outputs after the flow is done.
    /// </summary>
    private readonly List<Node> _dirtyNodes = [];

    private readonly Dictionary<int, Node> _nodes = new();
    private readonly Dictionary<int, Node> _outletNodes = new();


    /// <summary>
    ///     The global lock for the topology, only one thread can be in the topology at a time.
    /// </summary>
    internal readonly Lock Lock = new();

    private readonly Queue<Node> _queue = new();


    public InletNode<T> Intern<T>(Inlet<T> inlet) where T : notnull
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(inlet.Id, out var node)) return (InletNode<T>)node;

            var inletNode = new InletNode<T>(this, inlet);
            _nodes[inlet.Id] = inletNode;
            _inlets.Add(inletNode);

            SortNodes();

            return inletNode;
        }
    }

    /// <summary>
    ///     Flows data from any inlets through the topology to all graph nodes.
    /// </summary>
    private void FlowData()
    {
        var sw = Stopwatch.StartNew();
        foreach (var node in _processOrder)
        {
            node.EndEpoch();

            if (!node.HasOutputData())
                continue;

            _dirtyNodes.Add(node);

            foreach (var (subscriber, tag) in node.Subscribers)
            {
                node.FlowOut(subscriber, tag);
            }
        }

        foreach (var node in _dirtyNodes)
        {
            node.ResetOutput();
        }
        _dirtyNodes.Clear();
        Debug.WriteLine($"Flow took {sw.ElapsedMilliseconds}ms");
    }

    private Node Intern(Flow flow)
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(flow.Id, out var node)) return node;

            node = flow.CreateNode(this);
            for (var idx = 0; idx < flow.Upstream.Length; idx++)
            {
                var upstream = Intern(flow.Upstream[idx]);
                upstream.Subscribers.Add((node, idx));
                node.Upstream[idx] = upstream;
                node.LastSeenIds[idx] = upstream.RevsionId;
                node.ResetOutput();
            }
            node.Created();

            _nodes[flow.Id] = node;
            return node;
        }
    }

    public OutletNode<T> Outlet<T>(Flow<T> flow) where T : notnull
    {
        lock (Lock)
        {
            if (_outletNodes.TryGetValue(flow.Id, out var node)) return (OutletNode<T>)node;

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
            outletNode.Accept(0, upstream.Output);
            upstream.ResetOutput();

            _outletNodes[flow.Id] = outletNode;

            SortNodes();

            return outletNode;
        }
    }

    public void FlowFrom<T>(InletNode<T> inletNode) where T : notnull
    {
        FlowData();
    }

    private void SortNodes()
    {
        // Initialize each node's InDegree based on its upstream dependencies.
        foreach (var node in _nodes.Values)
        {
            node.InDegree = node.Upstream.Length;
            if (node.InDegree == 0)
                _queue.Enqueue(node);
        }

        // Start with nodes that have no upstream dependencies.

        _processOrder.Clear();

        while (_queue.Any())
        {
            var current = _queue.Dequeue();
            _processOrder.Add(current);

            // For each downstream subscriber, reduce its InDegree.
            // Subscribers is a list of (Node, int) where Node is the downstream node.
            foreach (var (subscriber, _) in current.Subscribers)
            {
                subscriber.InDegree--;
                if (subscriber.InDegree == 0)
                {
                    _queue.Enqueue(subscriber);
                }
            }
        }

        _processOrder.AddRange(_outletNodes.Values);

        // If we haven't processed every node, a cycle exists.
        if (_processOrder.Count != _nodes.Count + _outletNodes.Count)
        {
            throw new InvalidOperationException("Cycle detected in the dependency graph.");
        }
    }
}
