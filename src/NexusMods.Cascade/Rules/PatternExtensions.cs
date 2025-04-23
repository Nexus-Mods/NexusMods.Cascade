using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Rules;

public static partial class PatternExtensions
{
    [GenerateLVarOverrides]
    public static Pattern With<T1, T2>(this Pattern pattern, Flow<KeyedValue<T1, T2>> flow, LVar<T1> lvar1, LVar<T2> lvar2)
        where T1 : notnull
        where T2 : notnull
    {
        if (pattern._flow is null)
            return new Pattern
            {
                _inScope = pattern._inScope.Add(lvar1).Add(lvar2),
                _mappings = pattern._mappings.Add(lvar1, 0).Add(lvar2, 1),
                _flow = flow
            };

        return pattern.Join(flow, true, lvar1, lvar2);
    }

}
