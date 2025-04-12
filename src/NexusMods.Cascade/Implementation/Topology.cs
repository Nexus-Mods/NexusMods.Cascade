using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Clarp;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation;

internal class Topology : ITopology
{
    private readonly TxDictionary<object, NodeRef> _nodes = new();

    /// <summary>
    /// Mapping of flows to sources would be a IFlow -> ISource mapping, but some things like
    /// indexed flows are not strictly flows or sources, but we still store them here.
    /// </summary>
    private readonly TxDictionary<object, object> _flows = new();
    private readonly TxDictionary<(FlowDescription Flow, Type Type), object> _outlets = new();
    private readonly Agent<object> _effectQueue = new();

    private void FlowData(Ref<Node> nodeRef, object data, int tag)
    {
        var queue = new Queue<(Ref<Node> Node, object Data, int Tag)>();
        queue.Enqueue((nodeRef, data, tag));
        RunFlowQueue(queue);
    }

    private static void RunFlowQueue(Queue<(Ref<Node> Node, object Data, int Tag)> queue)
    {
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            var node = item.Node.Value;

            var (newNode, output) = node.Flow.Reducers[item.Tag](node, item.Tag, item.Data);

            if (!ReferenceEquals(newNode, node))
                item.Node.Value = newNode;

            if (output is null)
                continue;

            foreach (var (subscriberRef, subTag) in node.Subscribers)
                queue.Enqueue((subscriberRef, output, subTag));
        }
    }

    public NodeRef Intern(FlowDescription flow)
    {
        return Runtime.DoSync(static s =>
        {
            var (self, flow) = s;
            if (self._nodes.TryGetValue(flow, out var nodeRef))
                return nodeRef;
            var constructed = self.Construct(flow);
            self._nodes[flow] = constructed;

            // We need to backflow into the node if it has a state function
            if (constructed.Value.Flow.StateFn is not null && constructed.Value.Upstream.Length > 0)
            {
                var newState = self.BackflowInto(constructed);
                constructed.Value = newState;
            }

            return constructed;
        }, (this, flow));
    }

    public Inlet<T> Intern<T>(InletDefinition<T> inletDefinition) where T : notnull
    {
        var interned = Intern(inletDefinition.Description);
        return new Inlet<T>(interned);
    }

    public DiffInlet<T> Intern<T>(DiffInletDefinition<T> inletDefinition) where T : notnull
    {
        var interned = Intern(inletDefinition.Description);
        return new DiffInlet<T>(interned);
    }

    public void FlowFrom(Node state, object value)
    {
        var queue = new Queue<(Ref<Node> Node, object Data, int Tag)>();
        foreach (var sub in state.Subscribers)
        {
            var (subscriber, tag) = sub;
            var node = subscriber.Value;
            if (node.State == State.Stopped)
                continue;
            queue.Enqueue((subscriber, value, tag));
        }
        RunFlowQueue(queue);
    }

    public Outlet<T> Outlet<T>(Flow<T> flow)
    {
        return Runtime.DoSync(static s =>
        {
            var (self, flow) = s;
            var key = (flow.Description, typeof(Outlet<T>));
            if (self._outlets.TryGetValue(key, out var outlet))
                return (Outlet<T>)outlet;

            var outletDefinition = Abstractions.Outlet<T>.MakeFlow(flow.Description);

            var outletRef = self.Intern(outletDefinition);
            var newState = self.BackflowInto(outletRef);
            outletRef.Value = newState;
            self._outlets[key] = outletRef;
            return new Outlet<T>(outletRef);
        }, (this, flow));
    }

    public DiffOutlet<T> Outlet<T>(DiffFlow<T> flow) where T : notnull
    {
        return Runtime.DoSync(static s =>
        {
            var (self, flow) = s;
            var key = (flow.Description, typeof(DiffOutlet<T>));
            if (self._outlets.TryGetValue(key, out var outlet))
                return (DiffOutlet<T>)outlet;

            var outletDefinition = Abstractions.DiffOutlet<T>.MakeFlow(flow.Description);

            var outletRef = self.Intern(outletDefinition);
            var newState = self.BackflowInto(outletRef);
            outletRef.Value = newState;
            self._outlets[key] = outletRef;
            return new DiffOutlet<T>(outletRef);
        }, (this, flow));
    }

    private Node BackflowInto(NodeRef outletRef)
    {
        // Create some structures to old the nodes we need to scan
        var nodesToScan = new Queue<NodeRef>();

        // This will be all the nodes we have seen
        var visited = new HashSet<NodeRef>();

        // This will be all the nodes that have a replayable state function
        var baseNodes = new List<NodeRef>();

        nodesToScan.Enqueue(outletRef);

        while (nodesToScan.Count > 0)
        {
            var thisNode = nodesToScan.Dequeue();

            if (!visited.Add(thisNode))
                continue;

            // If we have a stateFn, then we are a base node
            if (thisNode.Value.Flow.StateFn != null && thisNode != outletRef)
                baseNodes.Add(thisNode);

            foreach (var upstream in thisNode.Value.Upstream)
            {
                if (visited.Contains(upstream))
                    continue;
                nodesToScan.Enqueue(upstream);
            }
        }

        // Now start at the base nodes, and flow down through the graph. But we don't want to muck
        // with the internal state of the nodes, so we'll reset the state

        // First we make a new pending flow queue
        var pendingFlowQueue = new Queue<(NodeRef Node, object Data, int Tag)>();

        // And now start by feeding in the states of all the base nodes
        foreach (var baseNodeRef in baseNodes)
        {
            var baseNode = baseNodeRef.Value;
            var state = baseNode.Flow.StateFn!(baseNode);

            // Enqueue the state for each of the subscribers
            foreach (var (node, tag) in baseNode.Subscribers)
                pendingFlowQueue.Enqueue((node, state, tag));
        }

        // Now we can run the flow queue, but this time we don't propagate the state to any nodes not
        // in our visited list, and we reset each node to the initial state

        // We'll reset the node's state, but we want to track the state of the nodes while we propagate
        var localState = new Dictionary<NodeRef, Node>();
        while (pendingFlowQueue.Count > 0)
        {
            var (nodeRef, data, tag) = pendingFlowQueue.Dequeue();

            // If we've never seen this node before, we need to reset its state
            if (!localState.TryGetValue(nodeRef, out var node))
            {
                node = nodeRef.Value;
                if (node.Flow.InitFn != null)
                    node = node with { UserState = node.Flow.InitFn() };
                else if (node.UserState != null)
                    node = node with { UserState = null };
            }

            var (newNode, output) = node.Flow.Reducers[tag](node, tag, data);

            localState[nodeRef] = newNode;

            if (output == null)
                continue;

            // Now we propagate to the subscribers, but only if they are in our visited list
            foreach (var (subscriberRef, subTag) in node.Subscribers)
            {
                if (!visited.Contains(subscriberRef))
                    continue;

                pendingFlowQueue.Enqueue((subscriberRef, output, subTag));
            }
        }

        // Now return the local state for the current outlet
        if (!localState.TryGetValue(outletRef, out var nodeState))
            return outletRef.Value;
        return localState[outletRef];
    }

    private NodeRef Construct(FlowDescription flow)
    {
        var upstream = GC.AllocateUninitializedArray<NodeRef>(flow.UpstreamFlows.Length);
        for (var i = 0; i < flow.UpstreamFlows.Length; i++)
        {
            upstream[i] = Intern(flow.UpstreamFlows[i]);
        }

        object? userState = null;
        if (flow.InitFn is not null)
            userState = ((Func<object>)flow.InitFn)();

        var nodeRef = new NodeRef(new Node
        {
            Flow = flow,
            Topology = this,
            State = State.Running,
            Subscribers = ImmutableArray<(NodeRef, int)>.Empty,
            UserState = userState,
            Upstream = upstream,
        });

        for (var idx = 0; idx < flow.UpstreamFlows.Length; idx++)
        {
            upstream[idx].Connect(nodeRef, idx);
        }

        return nodeRef;
    }
}
