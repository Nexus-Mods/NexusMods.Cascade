﻿using System;
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

    private readonly Dictionary<IStageDefinition, IOutletDefinition> _implicitOutlets = [];

    /// <summary>
    /// Mapping of stage outputs to stages that require that output
    /// </summary>
    private readonly Dictionary<(IStage OutputStage, int OutputIndex), List<(IStage InputStage, int InputIndex)>> _connections = new();

    /// <summary>
    /// Backwards index of connections from an input to an output
    /// </summary>
    private readonly Dictionary<(IStage InputStage, int InputIndex), List<(IStage OutputStage, int OutputIndex)>> _backConnections = new();

    private IStage AddStage(IStageDefinition definition)
    {
        // Deduplicate the stage
        if (_stages.TryGetValue(definition, out var found))
            return found;

        // Create the stage if it doesn't exist
        var instance = definition.CreateInstance(this);
        _stages.Add(definition, instance);
        var idx = 0;
        foreach (var outlet in definition.UpstreamInputs)
        {
            var upstream = AddStage(outlet.Stage);
            Connect(upstream, outlet.Index, instance, idx);
            idx++;
        }
        return instance;
    }

    private IOutlet<T> GetOutlet<T>(IQuery<T> stageDefinition)
        where T : notnull
    {
        if (stageDefinition is IOutletDefinition<T> outlet)
            return (IOutlet<T>)AddStage(outlet);

        var newOutlet = new Outlet<T>(stageDefinition.Output);
        _implicitOutlets.Add(stageDefinition, newOutlet);
        return (IOutlet<T>)AddStage(newOutlet);
    }

    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<T> input, int delta = 1)
        where T : notnull
    {
        var stage = AddStage(definition);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.OutputSets[0].Reset();
        inlet.Add(input, delta);

        FlowDataFrom(stage);
    }

    internal void AddData<T>(IInletDefinition<T> definition, ReadOnlySpan<(T Item, int delta)> input)
        where T : notnull
    {
        var stage = AddStage(definition);

        var inlet = (Inlet<T>.Stage)stage;
        inlet.OutputSets[0].Reset();
        inlet.Add(input);

        FlowDataFrom(stage);
    }

    private void FlowDataFrom(IStage stage)
    {
        var idx = 0;
        foreach (var output in stage.OutputSets)
        {
            if (!_connections.TryGetValue((stage, idx), out var connections))
                continue;

            foreach (var (inputStage, inputIndex) in connections)
            {
                inputStage.ResetAllOutputs();
                inputStage.AddData(output, inputIndex);
                FlowDataFrom(inputStage);
            }
            idx++;
        }
    }

    internal IReadOnlyCollection<T> GetAllResults<T>(IQuery<T> stageDefinition) where T : notnull
    {

        var stage = GetOutlet(stageDefinition);

        if (stage is not Outlet<T>.Stage outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        BackPropagate(outlet);

        return outlet.Results;

    }

    /// <summary>
    /// Calculates the given stage by getting the results of the upstream stages and then running
    /// the logic for this stage
    /// </summary>
    private void BackPropagate(IStage stage)
    {
        foreach (var output in stage.OutputSets)
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
                var upstreamStage = AddStage(upstream.Stage);
                BackPropagate(upstreamStage);
                stage.AddData(upstreamStage.OutputSets[upstream.Index], idx);
                idx++;
            }
        }
    }

    public IObservableResultSet<T> ObserveAllResults<T>(IQuery<T> stage) where T : notnull
    {
        if (!_stages.TryGetValue(stage, out var found))
        {
            throw new ArgumentException("Stage not found", nameof(stage));
        }

        if (found is not Outlet<T>.Stage outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        return outlet.Observe();
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
