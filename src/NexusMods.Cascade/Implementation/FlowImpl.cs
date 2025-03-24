using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Outlets;
using ObservableCollections;

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
    private readonly Dictionary<(IStage, Type), IOutlet> _outlets = [];

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

    private IOutlet GetOutlet<TValue, TOutletType>(IQuery<TValue> stageDefinition, out bool wasCreated)
        where TOutletType : IOutletDefinition<TValue>
        where TValue : notnull
    {
        // Resolve the upstream stage
        var stage = AddStage(stageDefinition, out wasCreated);

        // Look for an existing outlet
        var key = (stage, typeof(TOutletType));
        if (_outlets.TryGetValue(key, out var existingOutlet))
        {
            wasCreated = false;
            return existingOutlet;
        }

        // Otherwise create a new outlet
        wasCreated = true;
        var newOutletDefinition = TOutletType.Create(stageDefinition.ToUpstreamConnection());
        var outlet = (IOutlet)AddStage(newOutletDefinition, out _);
        _outlets.Add(key, outlet);
        BackPropagate(outlet);
        outlet.ReleasePendingSends();
        _dirtyOutlets.Clear();
        return outlet;
    }


    public ObservableDictionary<T,int> Observe<T>(IQuery<T> queryDefinition) where T : notnull
    {
        var outlet = (ObservableCollectionResult<T>.Stage)GetOutlet<T, ObservableCollectionResult<T>>(queryDefinition, out var wasCreated);
        return outlet.Results;
    }


    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<T> input, int delta = 1)
        where T : notnull
    {
        var stage = AddStage(definition, out _);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.ChangeSets[0].Reset();
        inlet.Add(input, delta);

        FlowDataFrom(stage);
    }

    private void ReleasePendingSends()
    {
        foreach (var outlet in _dirtyOutlets)
        {
            outlet.ReleasePendingSends();
        }
        _dirtyOutlets.Clear();
    }

    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<Change<T>> input)
        where T : notnull
    {
        var stage = AddStage(definition, out _);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.ChangeSets[0].Reset(); inlet.Add(input);

        FlowDataFrom(stage);
    }

    public void RunFlows()
    {
        ReleasePendingSends();
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

        var stage = (CollectionResult<T>.Stage)GetOutlet<T, CollectionResult<T>>(stageDefinition, out var wasCreated);
        return stage.Results;

    }

    public ObservableDictionary<TKey,TActive> ObserveActive<TKey, TActive, TBase>(IQuery<TBase> queryDefinition)
        where TKey : notnull
        where TActive : IActiveRow<TBase, TKey>
        where TBase : IRowDefinition<TKey>
    {
        var stage = (ActiveRecordObservableCollectionResult<TKey, TActive, TBase>.Stage)GetOutlet<TBase, ActiveRecordObservableCollectionResult<TKey, TActive, TBase>>(queryDefinition, out _);
        return stage.Results;
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

/*
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
    */

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
