using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Template;

public class Flow : IFlow
{
    private SemaphoreSlim _lock = new(1, 1);
    private HashSet<IStage> _stages = [];

    /// <summary>
    /// Mapping of stage outputs to stages that require that output
    /// </summary>
    private readonly Dictionary<(IStage OutputStage, int OutputIndex), List<(IStage InputStage, int InputIndex)>> _connections = new();

    /// <summary>
    /// Backwards index of connections from an input to an output
    /// </summary>
    private readonly Dictionary<(IStage InputStage, int InputIndex), List<(IStage OutputStage, int OutputIndex)>> _backConnections = new();

    public async ValueTask<FlowLock> LockAsync()
    {
        await _lock.WaitAsync();
        return new FlowLock(this);
    }

    public FlowLock Lock()
    {
        _lock.Wait();
        return new FlowLock(this);
    }

    public IStage AddStage<T>(IStage stage) where T : notnull
    {
        throw new NotImplementedException();
    }

    public void AddInputData<T>(IInlet<T> stageId, ReadOnlySpan<T> input) where T : notnull
    {
        throw new NotImplementedException();
    }

    public TStage AddStage<TStage>(TStage stage)
    where TStage : IStage
    {
        // Deduplicate the stage
        if (_stages.TryGetValue(stage, out var found))
        {
            return (TStage)found;
        }
        else
        {
            _stages.Add(stage);
            return stage;
        }
    }

    public void AddInputData<TStage, TType>(TStage stage, ReadOnlySpan<TType> input)
        where TStage : IInlet<TType>
        where TType : notnull
    {
        stage = AddStage(stage);

        ((Inlet<TType>)(IStage)stage).AddInputData(input);

        FlowDataFrom(stage);
    }

    public void RemoveInputData<T>(IInlet<T> stageId, ReadOnlySpan<T> input)
        where T : notnull
    {
        if (!_stages.TryGetValue(stageId, out var stage))
        {
            throw new ArgumentException("Stage not found", nameof(stageId));
        }

        ((Inlet<T>)stage).RemoveInputData(input);

        FlowDataFrom(stage);
    }

    private void FlowDataFrom(IStage stage)
    {
        foreach (var output in stage.Outputs)
        {
            if (!_connections.TryGetValue((stage, output.Index), out var connections))
                continue;

            foreach (var (inputStage, inputIndex) in connections)
            {
                var castedInputStage = (AStage)inputStage;
                castedInputStage.AddData(output.OutputSet, inputIndex);
                FlowDataFrom(inputStage);
            }
        }

    }

    public IReadOnlyCollection<T> GetAllResults<T>(IOutlet<T> stage) where T : notnull
    {
        if (!_stages.TryGetValue(stage, out var found))
        {
            throw new ArgumentException("Stage not found", nameof(stage));
        }

        if (found is not Outlet<T> outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        return outlet.GetResults();
    }

    public IObservableResultSet<T> ObserveAllResults<T>(IOutlet<T> stage) where T : notnull
    {
        if (!_stages.TryGetValue(stage, out var found))
        {
            throw new ArgumentException("Stage not found", nameof(stage));
        }

        if (found is not Outlet<T> outlet)
        {
            throw new ArgumentException("Stage is not an Outlet", nameof(stage));
        }

        return outlet.ObserveResults();
    }


    public void Unlock()
    {
        _lock.Release();
    }


    public void Connect(IStage inlet, int outputId, IStage filter, int inputId)
    {
        ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_connections, (inlet, outputId), out _);
        found ??= new List<(IStage, int)>();
        found.Add((filter, inputId));

        ref var backFound = ref CollectionsMarshal.GetValueRefOrAddDefault(_backConnections, (filter, inputId), out _);
        backFound ??= new List<(IStage, int)>();
        backFound.Add((inlet, outputId));
    }
}
