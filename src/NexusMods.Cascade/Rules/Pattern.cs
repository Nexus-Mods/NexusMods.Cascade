using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Rules;

public record Pattern
{
    /// <summary>
    /// Start the definition of a new pattern
    /// </summary>
    public static Pattern Create() => new();

    private ImmutableHashSet<LVar> _inScope = ImmutableHashSet<LVar>.Empty;

    private ImmutableDictionary<LVar, int> _mappings = ImmutableDictionary<LVar, int>.Empty;

    private Flow? _flow = null;

    public Pattern Define<T>(out LVar<T> variable, [CallerArgumentExpression(nameof(variable))] string? name = null)
    {
        variable = LVar<T>.Create(name ?? string.Empty);
        return this;
    }

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

    public Pattern With<T1, T2>(Flow<(T1, T2)> flow, LVar<T1> lvar1, LVar<T2> lvar2)
    {
        if (_flow is null)
            return new Pattern
            {
                _inScope = _inScope.Add(lvar1).Add(lvar2),
                _mappings = _mappings.Add(lvar1, 0).Add(lvar2, 1),
                _flow = flow
            };

        return Join(flow, lvar1, lvar2);
    }

    public Flow<(T1, T2)> Return<T1, T2>(IReturnValue<T1> lvar1, IReturnValue<T2> lvar2)
    {
        return (Flow<(T1, T2)>)CompileReturn(lvar1, lvar2);
    }

    private Flow CompileReturn(params IReturnValue[] retVals)
    {
        var lvars = retVals.OfType<LVar>().ToArray();
        if (lvars.Length == retVals.Length)
            return CompileDirectReturn(lvars);

        var keyVars = retVals.OfType<LVar>().ToArray();
        var aggregates = retVals.OfType<IAggregate>().ToArray();

        return CompileAggregate(keyVars, aggregates);
    }

    private Flow CompileAggregate(LVar[] keyVars, IAggregate[] aggregates)
    {
        var keyIdxes = keyVars.Select(lvar => _mappings[lvar]).ToArray();
        var keyType = TupleHelpers.TupleTypeFor(keyVars.Select(lvar => lvar.Type).ToArray());
        var keyFn = TupleHelpers.Selector(_flow!.OutputType, keyIdxes);

        var rekeyed = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Rekey))
            ?.MakeGenericMethod(_flow.OutputType, keyType)
            .Invoke(null, [_flow, keyFn, "<unknown>", "", 0])!;

        List<Flow> aggFlows = new List<Flow>();
        foreach (var agg in aggregates)
        {
            var aggIdx = Array.IndexOf(keyVars, agg.Source);
            var getter = TupleHelpers.AggGetterFn(rekeyed.OutputType, _mappings[agg.Source]);
            var aggFlow = agg.Constructor
                .MakeGenericMethod(rekeyed.OutputType,
        }

        var aggIdxes = aggregates.Select(agg => _mappings[agg.Source]).ToArray();
        var aggStateTypes = aggregates.Select(agg => agg.StateType).ToArray();
        var aggStateTuple = TupleHelpers.TupleTypeFor(aggStateTypes.ToArray());



        throw new NotImplementedException();
    }

    private Flow CompileDirectReturn(LVar[] lvars)
    {
        var retIdxes = lvars.Select(lvar => _mappings[lvar]).ToArray();
        var retFn = TupleHelpers.Selector(_flow!.OutputType, retIdxes);
        var resultType = TupleHelpers.TupleTypeFor(lvars.Select(lvar => lvar.Type).ToArray());

        var resultFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(_flow.OutputType, resultType)
            .Invoke(null, [_flow, retFn, "<unknown>", "", 0])!;

        return resultFlow;
    }

    public Pattern Join(Flow rightFlow, params ReadOnlySpan<LVar> lvars)
    {
        var lvarsArray = lvars.ToArray();
        var newMappings = _mappings;
        foreach (var lvar in lvars)
        {
            if (!newMappings.ContainsKey(lvar))
            {
                newMappings = _mappings.Add(lvar, newMappings.Count);
            }
        }

        var joinKeys = _mappings.Keys.Intersect(lvarsArray).ToArray();
        var joinType = TupleHelpers.TupleTypeFor(joinKeys.Select(l => l.Type).ToArray());

        var leftIdxes = joinKeys.Select(k => _mappings[k]).ToArray();
        var leftFn = TupleHelpers.Selector(_flow!.OutputType, leftIdxes);

        var rightIdxes = joinKeys.Select(k => Array.IndexOf(lvarsArray, k)).ToArray();
        var rightFn = TupleHelpers.Selector(rightFlow.OutputType, rightIdxes);

        var selectMappings = new List<(bool Left, int idx)>();

        var resultTypes = newMappings.OrderBy(static kv => kv.Value).ToArray();
        var resultType = TupleHelpers.TupleTypeFor(resultTypes.Select(r => r.Key.Type).ToArray());

        foreach (var (lvar, idx) in resultTypes)
        {
            if (_mappings.TryGetValue(lvar, out var mapping))
            {
                selectMappings.Add((true, mapping));
            }
            else
            {
                selectMappings.Add((false, Array.IndexOf(lvarsArray, lvar)));
            }
        }

        var resultSelector = TupleHelpers.ResultSelector(
            _flow.OutputType, rightFlow.OutputType, selectMappings.ToArray());

        var joinFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Join))
            ?.MakeGenericMethod(new[]
            {
                _flow.OutputType,
                rightFlow.OutputType,
                joinType,
                resultType
            })
            .Invoke(null, [_flow, rightFlow, leftFn, rightFn, resultSelector])!;

        return new Pattern
        {
            _inScope = _inScope.Union(lvarsArray),
            _mappings = newMappings,
            _flow = joinFlow
        };
    }
}
