using System.Collections.Generic;

namespace NexusMods.Cascade.Rules;

public record Environment
{
    public Dictionary<LVar, int> LVars { get; } = new();
}
