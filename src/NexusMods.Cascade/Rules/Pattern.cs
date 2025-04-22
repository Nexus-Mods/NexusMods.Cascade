using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.Cascade.Rules;

public record Pattern
{
    /// <summary>
    /// Start the definition of a new pattern
    /// </summary>
    public static Pattern Define() => new();

    private ImmutableHashSet<LVar> _inScope = ImmutableHashSet<LVar>.Empty;

    private ImmutableDictionary<LVar, int> _mappings = ImmutableDictionary<LVar, int>.Empty;

    private Flow? _flow = null;

    public Pattern With<T>(Flow<T> flow, LVar<T> lvar)
    {
        if (_flow is null)
            return new Pattern()
            {
                _inScope = _inScope.Add(lvar),
                _mappings = _mappings.Add(lvar, 0),
                _flow = flow
            };


        return Join(flow, lvar);
    }

    public Pattern Join(Flow flow, params ReadOnlySpan<LVar> lvars)
    {
        var newMappings = _mappings;
        foreach (var lvar in lvars)
        {
            if (

        }

    }
}
