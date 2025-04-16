using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Abstractions;

public class DiffInletDefinition<T> : IDiffFlow<T> where T : notnull
{
    private readonly FlowDescription _description;

    public DiffInletDefinition([CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
    {
        _description = new FlowDescription
        {
            Name = "DiffInlet",
            InitFn = static () => ResultSet<T>.Empty,
            StateFn = static state => state.UserState!,
            UpstreamFlows = [],
            Reducers = [],
            DebugInfo = DebugInfo.Create($"Inlet: {typeof(T).Name}", path, line)
        };
    }
    public DiffInletDefinition(string name, [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
    {
        _description = new FlowDescription
        {
            Name = name,
            InitFn = static () => ResultSet<T>.Empty,
            StateFn = static (state) => state.UserState!,
            UpstreamFlows = [],
            Reducers = [],
            DebugInfo = DebugInfo.Create($"Inlet: {name} ({typeof(T).Name}", path, line)
        };
    }

    public FlowDescription AsFlow() => _description;
}
