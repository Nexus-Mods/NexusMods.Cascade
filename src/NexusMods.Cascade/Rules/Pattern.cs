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

        return CompileAggregate(keyVars, aggregates, retVals);
    }

    private Flow CompileAggregate(LVar[] keyVars, IAggregate[] aggregates, IReturnValue[] outputOrder)
    {
        // Generate the key information.
        var keyIdxes = keyVars.Select(lvar => _mappings[lvar]).ToArray();
        var keyType = TupleHelpers.TupleTypeFor(keyVars.Select(lvar => lvar.Type).ToArray());
        var keyFn = TupleHelpers.Selector(_flow!.OutputType, keyIdxes);

        // Re-key the initial flow based on the key variables.
        var rekeyed = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Rekey))
            ?.MakeGenericMethod(_flow.OutputType, keyType)
            .Invoke(null, new object[] { _flow, keyFn, "<unknown>", "", 0 })!;

        // Process each aggregate separately.
        List<Flow> aggFlows = new List<Flow>();
        foreach (var agg in aggregates)
        {
            var mapping = _mappings[agg.Source];
            var getter = TupleHelpers.AggGetterFn(_flow.OutputType, mapping);
            Flow aggFlow;

            // Assume AggregateType is an enum indicating which aggregation to perform.
            if (agg.AggregateType == IAggregate.AggregateTypes.Count)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.Count))
                    ?.MakeGenericMethod(rekeyed.OutputType)
                    .Invoke(null, new object[] { rekeyed, getter })!;
            }
            else if (agg.AggregateType == IAggregate.AggregateTypes.Max)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.MaxBy))
                    ?.MakeGenericMethod(keyType, _flow.OutputType, agg.SourceType)
                    .Invoke(null, new object[] { rekeyed, getter })!;
            }
            else
            {
                throw new NotSupportedException($"Aggregate type '{agg.AggregateType}' is not supported.");
            }
            aggFlows.Add(aggFlow);
        }

        // Combine the individual aggregate flows into one.
        // Each aggregate flow is assumed to produce a tuple: (key, aggregateValue).
        // We join them together on the key so that the final aggregate flow becomes:
        // (key, agg1, agg2, ..., aggN)
        Flow combinedAggFlow = aggFlows[0];
        for (int i = 1; i < aggFlows.Count; i++)
        {
            var nextFlow = aggFlows[i];

            // Both flows contain the key as their first element.
            var joinKeySelectorLeft = TupleHelpers.Selector(combinedAggFlow.OutputType,
                                                            Enumerable.Range(0, keyIdxes.Length).ToArray());
            var joinKeySelectorRight = TupleHelpers.Selector(nextFlow.OutputType, new int[] { 0 });

            // Build a result selector that keeps all columns from the left and
            // appends the aggregate value (skipping the key) from the right.
            var resultSelector = TupleHelpers.ResultSelector(
                combinedAggFlow.OutputType,
                nextFlow.OutputType,
                new (bool Left, int idx)[]
                {
                    // Pass through the left tuple (which has key and prior aggregates)
                    (true, 0),
                    // Append the aggregate from the right (assumed to be at index 1)
                    (false, 1)
                });

            // Determine the result tuple type by combining the types from the current left and new right.
            // For simplicity, we assume that the left's OutputType has generic arguments starting
            // with the key types followed by its aggregate(s), and that nextFlow's OutputType is (key, newAgg).
            var leftTypes = combinedAggFlow.OutputType.GenericTypeArguments;
            var rightTypes = nextFlow.OutputType.GenericTypeArguments.Skip(1).ToArray();
            var joinedTypes = leftTypes.Concat(rightTypes).ToArray();
            var resultTupleType = TupleHelpers.TupleTypeFor(joinedTypes);

            combinedAggFlow = (Flow)typeof(FlowExtensions)
                .GetMethod(nameof(FlowExtensions.Join))
                ?.MakeGenericMethod(
                    combinedAggFlow.OutputType,
                    nextFlow.OutputType,
                    keyType,
                    resultTupleType)
                .Invoke(null, new object[]
                {
                    combinedAggFlow,
                    nextFlow,
                    joinKeySelectorLeft,
                    joinKeySelectorRight,
                    resultSelector
                })!;
        }

        // Now flatten the keyed results into a final tuple
        var finalResultTypes = outputOrder.Select(o => o.Type).ToArray();
        var finalResultType = TupleHelpers.TupleTypeFor(finalResultTypes);

        var indices = outputOrder.Select(ret =>
        {
            if (ret is LVar lvar)
            {
                // For key variables, find their position in the combined flow
                var keyIdx = Array.IndexOf(keyVars, lvar);
                return (Left: true, keyIdx);
            }
            else if (ret is IAggregate agg)
            {
                // For aggregates, offset by number of key columns
                var aggIdx = keyVars.Length + Array.IndexOf(aggregates, agg);
                return (Left: false, aggIdx);
            }
            throw new ArgumentException($"Unknown return type: {ret.GetType()}");
        }).ToArray();

        var finalSelector = TupleHelpers.ResultKeyedSelector(
            combinedAggFlow.OutputType,
            indices);

        var finalFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(
                combinedAggFlow.OutputType,
                finalResultType)
            .Invoke(null, new object[]
            {
                combinedAggFlow,
                finalSelector,
                "<unknown>",
                "",
                0
            })!;

        return finalFlow;
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
