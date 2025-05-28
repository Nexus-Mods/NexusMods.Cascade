using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clarp.Concurrency;
using JetBrains.Annotations;

namespace NexusMods.Cascade;

public sealed class Topology : IDisposable
{
    [MustDisposeResource]
    public static Topology Create()
    {
        return new Topology();
    }

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
    /// Runner queue, used to run the topology in a single thread.
    /// </summary>
    private readonly Agent<int> _primaryRunner = new();

    /// <summary>
    /// A queue used to execute effects (change notifications)
    /// </summary>
    private readonly Agent<int> _effectQueue = new();

    private readonly Queue<Node> _queue = new();

    public InletNode<T> Intern<T>(Inlet<T> inlet) where T : notnull
    {
        return InternAsync(inlet).Result;
    }

    public Task<InletNode<T>> InternAsync<T>(Inlet<T> inlet) where T : notnull
    {
        return RunInMainThread(() =>
        {
            if (_nodes.TryGetValue(inlet.Id, out var node))
                return (InletNode<T>)node;

            var inletNode = new InletNode<T>(this, inlet);
            _nodes[inlet.Id] = inletNode;
            _inlets.Add(inletNode);

            SortNodes();
            return inletNode;
        });
    }

    internal Task<T> RunInMainThread<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _primaryRunner.Send(_ =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
                return 0;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return 0;
            }
        });

        return tcs.Task;
    }

    internal void RunInMainThreadNoWait(Action func)
    {
        var tcs = new TaskCompletionSource();
        _primaryRunner.Send(_ =>
        {
            try
            {
                func();
                tcs.SetResult();
                return 0;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return 0;
            }
        });
    }

    internal Task RunInMainThread(Action func)
    {
        var tcs = new TaskCompletionSource();
        _primaryRunner.Send(_ =>
        {
            try
            {
                func();
                tcs.SetResult();
                return 0;
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                return 0;
            }
        });

        return tcs.Task;
    }

    public void EnqueueEffect(Action a)
    {
        _effectQueue.Send(_ =>
        {
            a();
            return 0;
        });
    }

    /// <summary>
    /// Enqueues an empty effect to the effect queue, once this task completes, all effects
    /// up to this point will have been executed.
    /// </summary>
    public Task FlushEffectsAsync()
    {
        var tcs = new TaskCompletionSource();
        _effectQueue.Send(_ =>
        {
            tcs.SetResult();
            return 0;
        });
        return tcs.Task;
    }


    /// <summary>
    ///     Flows data from any inlets through the topology to all graph nodes.
    /// </summary>
    public Task FlowDataAsync()
    {
        return RunInMainThread(FlowData);
    }

    internal void FlowData()
    {
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
    }

    private Node Intern(Flow flow)
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

    [MustDisposeResource]
    public async Task<IQueryResult<T>> QueryAsync<T>(Flow<T> flow) where T : notnull
    {
        var view = new OutletNodeView<T>(this, flow);
        RunInMainThreadNoWait(() => QueryCore(flow, view));
        await view.Initialized;
        return view;
    }

    [MustDisposeResource]
    public IQueryResult<T> Query<T>(Flow<T> flow) where T : notnull
    {
        return QueryAsync(flow).Result;
    }

    [MustDisposeResource]
    public IQueryResult<T> QueryNoWait<T>(Flow<T> flow) where T : notnull
    {
        var view = new OutletNodeView<T>(this, flow);
        RunInMainThreadNoWait(() => QueryCore(flow, view));
        return view;
    }

    internal void QueryCore<T>(Flow<T> flow, OutletNodeView<T> view) where T : notnull
    {
        if (_outletNodes.TryGetValue(flow.Id, out var node))
        {
            var casted = (OutletNode<T>)node;
            casted.AddView(view);
            return;
        }

        var upstream = (Node<T>)Intern(flow);

        var outletFlow = new OutletFlow<T>(flow);

        var outletNode = new OutletNode<T>(this, outletFlow)
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
        outletNode.AddView(view);
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

    /// <summary>
    /// Return the current topology as a mermaid diagram.
    /// </summary>
    /// <returns></returns>
    public string Diagram()
    {
        return RunInMainThread(() =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");


            foreach (var node in _nodes.Concat(_outletNodes))
            {
                var debugInfo = node.Value.Flow.DebugInfo;
                string nodeName;
                if (debugInfo == null)
                    nodeName = "Unknown";
                else
                {
                    nodeName = debugInfo.Name;
                    nodeName += $" {debugInfo.Expression}";
                }

                var shape = (node.Value.Flow.DebugInfo?.FlowShape ?? DebugInfo.Shape.Rect).ToString().ToLowerInvariant().Replace("_", "-");
                sb.AppendLine($"  id{node.Value.Flow.Id}@{{ shape: {shape}, label: \"{nodeName}\" }}");

                foreach (var subscriber in node.Value.Subscribers)
                {
                    sb.AppendLine($"  id{node.Value.Flow.Id} --> |{subscriber.Tag}|id{subscriber.Node.Flow.Id}");
                }
            }

            return sb.ToString();
        }).Result;
    }



    internal void UnsubAndCleanup(Node node, (Node downstream, int tag) subscriber)
    {
        node.Subscribers.Remove(subscriber);

        // Don't dispose inlets, they are always alive.
        if (node is IInletNode)
        {
            return;
        }

        if (node.Subscribers.Count == 0)
        {
            _nodes.Remove(node.Flow.Id);
            foreach (var upstream in node.Upstream)
            {
                UnsubAndCleanup(upstream, (node, 0));
            }
        }
    }

    public void Dispose()
    {
        RunInMainThread(() =>
        {
            foreach (var (_, node) in _outletNodes)
            {
                var casted = (IQueryResult)node;
                // Set the references to 1, so that the Dispose method fully disposes the node.
                casted.Dispose();
            }

            foreach (var inlet in _inlets)
            {
                if (inlet is IInletNode inletNode)
                    inletNode.Dispose();
            }
        });
    }
}
