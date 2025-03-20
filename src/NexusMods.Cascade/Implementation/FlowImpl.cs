using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

internal class FlowImpl : IFlowImpl
{
    /// <summary>
    /// A mapping of stage definitions to their instances
    /// </summary>
    private readonly Dictionary<IStageDefinition, IStage> _stages = [];

    /// <summary>
    /// Implicitly created o
    /// </summary>
    private readonly Dictionary<IStageDefinition, IOutlet> _implicitOutlets = [];

    private readonly Dictionary<IOutlet, Dictionary<Type, IQueryObserver>> _observers = [];

    private readonly HashSet<IOutlet> _dirtyOutlets = [];

    /// <summary>
    /// Stages that are yet to be flowed from, and won't until the end of the update call
    /// </summary>
    private readonly HashSet<IStage> _dirtyStages = [];

    /// <summary>
    /// Mapping of stage outputs to stages that require that output
    /// </summary>
    private readonly Dictionary<(IStage OutputStage, int OutputIndex), List<(IStage InputStage, int InputIndex)>> _connections = new();

    /// <summary>
    /// Backwards index of connections from an input to an output
    /// </summary>
    private readonly Dictionary<(IStage InputStage, int InputIndex), List<(IStage OutputStage, int OutputIndex)>> _backConnections = new();

    private IStage AddStage(IStageDefinition definition, out bool wasCreated)
    {
        // Deduplicate the stage
        if (_stages.TryGetValue(definition, out var found))
        {
            wasCreated = false;
            return found;
        }

        wasCreated = true;

        // Create the stage if it doesn't exist
        var instance = definition.CreateInstance(this);
        _stages.Add(definition, instance);
        var idx = 0;
        foreach (var upstreamConnection in definition.UpstreamInputs)
        {
            var upstream = AddStage(upstreamConnection.StageDefinition, out _);
            Connect(upstream, upstreamConnection.OutputDefinition.Index, instance, idx);
            idx++;
        }
        return instance;
    }

    private IOutlet<T> GetOutlet<T>(IQuery<T> stageDefinition, out bool wasCreated)
        where T : notnull
    {
        if (stageDefinition is IOutletDefinition<T> castedOutletDefinition)
        {
            return (IOutlet<T>)AddStage(castedOutletDefinition, out wasCreated);
        }

        if (_implicitOutlets.TryGetValue(stageDefinition, out var found))
        {
            wasCreated = false;
            return (IOutlet<T>)found;
        }

        wasCreated = true;
        var newOutletDefinition = new Outlet<T>(stageDefinition.ToUpstreamConnection());
        var outlet = (IOutlet)AddStage(newOutletDefinition, out _);
        _implicitOutlets.Add(stageDefinition, outlet);
        return (IOutlet<T>)outlet;
    }

    private void StartFlow()
    {
        _dirtyOutlets.Clear();
    }

    private void EndFlow()
    {
        foreach (var dirty in _dirtyOutlets)
        {
            if (!_observers.TryGetValue(dirty, out var observersForOutlet))
                continue;
            foreach (var observer in observersForOutlet.Values)
                observer.Update(dirty);

            dirty.ResetCurrentChanges();
        }

        _dirtyOutlets.Clear();

    }

    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<T> input, int delta = 1)
        where T : notnull
    {
        StartFlow();
        var stage = AddStage(definition, out _);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.ChangeSets[0].Reset();
        inlet.Add(input, delta);

        FlowDataFrom(stage);
        EndFlow();
    }

    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<Change<T>> input)
        where T : notnull
    {
        StartFlow();

        var stage = AddStage(definition, out _);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.ChangeSets[0].Reset();
        inlet.Add(input);

        FlowDataFrom(stage);
        EndFlow();
    }

    public void RunFlows()
    {
        if (_dirtyStages.Count == 0)
            return;

        foreach (var stage in _dirtyStages)
            FlowDataFrom(stage);
        _dirtyStages.Clear();
    }

    private void FlowDataFrom(IStage stage)
    {
        var idx = 0;
        if (stage is IOutlet outlet)
            _dirtyOutlets.Add(outlet);

        foreach (var output in stage.ChangeSets)
        {
            if (!_connections.TryGetValue((stage, idx), out var connections))
                continue;

            foreach (var (inputStage, inputIndex) in connections)
            {
                inputStage.ResetAllOutputs();
                inputStage.Definition.Inputs[inputIndex].AcceptChanges(inputStage, output);
                FlowDataFrom(inputStage);
            }
            idx++;
        }
    }

    internal IReadOnlyCollection<T> GetAllResults<T>(IQuery<T> stageDefinition) where T : notnull
    {

        var stage = GetOutlet(stageDefinition, out var wasCreated);

        if (stage is not Outlet<T>.Stage outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        if (wasCreated)
            BackPropagate(outlet);

        return outlet.Results;

    }

    /// <summary>
    /// Calculates the given stage by getting the results of the upstream stages and then running
    /// the logic for this stage
    /// </summary>
    private void BackPropagate(IStage stage)
    {
        foreach (var output in stage.ChangeSets)
            output.Reset();

        var idx = 0;
        if (stage is IHasSnapshot hasSnapshot)
        {
            hasSnapshot.OutputSnapshot();
        }
        else
        {
            foreach (var upstream in stage.Definition.UpstreamInputs)
            {
                var upstreamStage = AddStage(upstream.StageDefinition, out _);
                BackPropagate(upstreamStage);
                var newChangeset = upstreamStage.ChangeSets[upstream.OutputDefinition.Index];
                stage.Definition.Inputs[idx].AcceptChanges(stage, newChangeset);
                idx++;
            }
        }
    }

    public TObserver Observe<T, TObserver>(IQuery<T> stageDefinition) where T : notnull
        where TObserver : IQueryObserver<T>
    {
        StartFlow();
        var stage = GetOutlet(stageDefinition, out var wasCreated);

        if (stage is not Outlet<T>.Stage outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        if (wasCreated)
            BackPropagate(outlet);

        if (!_observers.TryGetValue(outlet, out var observersForOutlet))
        {
            observersForOutlet = new Dictionary<Type, IQueryObserver>();
            _observers.Add(outlet, observersForOutlet);
        }

        if (!observersForOutlet.TryGetValue(typeof(TObserver), out var observer))
        {
            observer = TObserver.Create(outlet, outlet.CurrentChanges);
            observersForOutlet.Add(typeof(T), observer);

            var observerAsT = (TObserver)observer;
            observerAsT.Update(outlet.ResultSetFactory.GetResultsAsChanges());
        }

        if (wasCreated)
            outlet.ResetCurrentChanges();

        EndFlow();
        return (TObserver)observer;
    }

    private void Connect(IStage inlet, int outputId, IStage filter, int inputId)
    {
        ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_connections, (inlet, outputId), out _);
        found ??= new List<(IStage, int)>();
        found.Add((filter, inputId));

        ref var backFound = ref CollectionsMarshal.GetValueRefOrAddDefault(_backConnections, (filter, inputId), out _);
        backFound ??= new List<(IStage, int)>();
        backFound.Add((inlet, outputId));
    }
}
