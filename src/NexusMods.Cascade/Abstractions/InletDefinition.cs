using System;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Abstractions;

public class InletDefinition<T> : IFlow<T> where T : notnull
{
    private readonly FlowDescription _description;

    public InletDefinition([CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
    {
        _description = new FlowDescription
        {
            InitFn = static () => default(T)!,
            StateFn = static (state) => state.UserState!,
            UpstreamFlows = [],
            Reducers = [],
            DebugInfo = DebugInfo.Create($"Inlet: {typeof(T).Name}", path, line)
        };
    }
    public InletDefinition(string name, [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
    {
        _description = new FlowDescription
        {
            InitFn = static () => default(T)!,
            StateFn = static (state) => state.UserState!,
            UpstreamFlows = [],
            Reducers = [],
            DebugInfo = DebugInfo.Create($"Inlet: {name} ({typeof(T).Name}", path, line)
        };
    }

    public FlowDescription Description => _description;
}

