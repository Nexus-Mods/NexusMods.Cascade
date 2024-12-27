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
    private Dictionary<StageId, IStage> _stages = new();

    /// <summary>
    /// Mapping of stage outputs to stages that require that output
    /// </summary>
    private readonly Dictionary<(StageId OutputStage, int OutputIndex), List<(StageId InputStage, int InputIndex)>> _connections = new();

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

    public StageId AddStage<T>(IStage stage) where T : notnull
    {
        var newId = StageId.From(Guid.NewGuid());
        _stages.Add(newId, stage);
        return newId;
    }

    public void AddInputData<T>(StageId stageId, ReadOnlySpan<T> input) where T : notnull
    {
        if (!_stages.TryGetValue(stageId, out var stage))
        {
            throw new ArgumentException("Stage not found", nameof(stageId));
        }

        ((Inlet<T>)stage).AddInputData(input);

        FlowDataFrom(stage, stageId);
    }

    private void FlowDataFrom(IStage value, StageId id)
    {
        foreach (var output in value.Outputs)
        {
            if (!_connections.TryGetValue((id, 0), out var connections))
                continue;

            foreach (var (inputStageId, inputIndex) in connections)
            {
                var inputStage = (AStage)_stages[inputStageId];
                inputStage.AddData(output.OutputSet, inputIndex);
                FlowDataFrom(inputStage, inputStageId);
            }
        }

    }

    public StageId AddStage(IStage stage)
    {
        var newId = StageId.From(Guid.NewGuid());
        _stages.Add(newId, stage);
        return newId;
    }

    public IReadOnlyCollection<T> GetAllResults<T>(StageId stageId) where T : notnull
    {
        if (!_stages.TryGetValue(stageId, out var stage))
        {
            throw new ArgumentException("Stage not found", nameof(stage));
        }

        return ((Outlet<T>)stage).GetResults();
    }

    public void Unlock()
    {
        _lock.Release();
    }

    public void AddData<T>(StageId inletId, int index, IOutputSet inputData)
        where T : notnull
    {
        if (!_stages.TryGetValue(inletId, out var stage))
        {
            throw new ArgumentException("Stage not found", nameof(inletId));
        }

        ((AStage)stage).AddData(inputData, index);

    }

    private void FlowData<T>(StageId inlet, DeduppingOutputSet<T> outputColl) where T : notnull
    {

        throw new NotImplementedException();

    }

    public void Connect(StageId inlet, int outputId, StageId filter, int inputId)
    {
        ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_connections, (inlet, outputId), out _);
        found ??= new List<(StageId, int)>();
        found.Add((filter, inputId));
    }
}
