using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Rules;

public record Pattern
{
    /// <summary>
    /// Start the definition of a new pattern
    /// </summary>
    public static Pattern Create() => new();

    internal ImmutableHashSet<LVar> _inScope = ImmutableHashSet<LVar>.Empty;

    internal ImmutableDictionary<LVar, int> _mappings = ImmutableDictionary<LVar, int>.Empty;

    internal Flow? _flow = null;

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


        return Join(flow, true, lvar);
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

        return Join(flow, true, lvar1, lvar2);
    }


    public Pattern Project<TIn, TOut>(LVar<TIn> lvar, Func<TIn, TOut> projector, out LVar<TOut> output,
        [CallerArgumentExpression(nameof(output))] string? name = null,
        [CallerArgumentExpression(nameof(output))] string? projecterExpr = null)
    {
        output = LVar<TOut>.Create(name ?? string.Empty);

        var newInScope = _inScope.Add(output);
        var newMappings = _mappings.Add(output, _mappings.Count);

        var inputType = _flow!.OutputType;
        var outputType = TupleHelpers.TupleTypeFor(newMappings.OrderBy(kv => kv.Value).Select(kv => kv.Key.Type).ToArray());
        var srcIdx = _mappings[lvar];
        var xform = TupleHelpers.TupleAppendFn(inputType, inputType.GetGenericArguments(), outputType, projector, srcIdx);

        var flow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))!
            .MakeGenericMethod(inputType, outputType)
            .Invoke(null, [_flow, xform, projecterExpr ?? "<unknown>", "", 0])!;

        return new Pattern()
        {
            _inScope = newInScope,
            _mappings = newMappings,
            _flow = flow
        };
    }

    public Pattern With<T1, T2>(Flow<KeyedValue<T1, T2>> flow, out LVar<T1> lvar1, out LVar<T2> lvar2,
        [CallerArgumentExpression(nameof(lvar1))] string? name1 = null,
        [CallerArgumentExpression(nameof(lvar2))] string? name2 = null)
        where T1 : notnull
        where T2 : notnull
    {
        lvar1 = LVar<T1>.Create(name1 ?? string.Empty);
        lvar2 = LVar<T2>.Create(name2 ?? string.Empty);
        if (_flow is null)
            return new Pattern
            {
                _inScope = _inScope.Add(lvar1).Add(lvar2),
                _mappings = _mappings.Add(lvar1, 0).Add(lvar2, 1),
                _flow = flow
            };

        return Join(flow, true, lvar1, lvar2);
    }

    public Flow<(T1, T2)> Return<T1, T2>(IReturnValue<T1> lvar1, IReturnValue<T2> lvar2)
    {
        return (Flow<(T1, T2)>)CompileReturn(lvar1, lvar2);
    }

    public Flow<(T1, T2, T3)> Return<T1, T2, T3>(IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3)
    {
        return (Flow<(T1, T2, T3)>)CompileReturn(lvar1, lvar2, lvar3);
    }

    public Flow<(T1, T2, T3, T4)> Return<T1, T2, T3, T4>(IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4)
    {
        return (Flow<(T1, T2, T3, T4)>)CompileReturn(lvar1, lvar2, lvar3, lvar4);
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

        var keyExpr = string.Join(", ", keyVars.Select(lvar => lvar.ToString()));
        // Re-key the initial flow based on the key variables.
        var rekeyed = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Rekey))
            ?.MakeGenericMethod(_flow.OutputType, keyType)
            .Invoke(null, new object[] { _flow, keyFn, $"({keyExpr})", "", 0 })!;

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
                    ?.MakeGenericMethod(keyType, _flow.OutputType)
                    .Invoke(null, new object[] { rekeyed, "", 0 })!;
            }
            else if (agg.AggregateType == IAggregate.AggregateTypes.Max)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.MaxOf))
                    ?.MakeGenericMethod(keyType, _flow.OutputType, agg.SourceType)
                    .Invoke(null, new object[] { rekeyed, getter, agg.Source.ToString() })!;
            }
            else if (agg.AggregateType == IAggregate.AggregateTypes.Sum)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.SumOf))
                    ?.MakeGenericMethod(keyType, _flow.OutputType, agg.SourceType)
                    .Invoke(null, new object[] { rekeyed, getter, agg.Source.ToString() })!;
            }
            else
            {
                throw new NotSupportedException($"Aggregate type '{agg.AggregateType}' is not supported.");
            }
            aggFlows.Add(aggFlow);
        }


        Flow finalAggFlow;

        if (aggFlows.Count > 1)
        {
            var method = typeof(FlowExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(f => f.Name == nameof(FlowExtensions.LeftInnerJoinFlatten) && f.IsGenericMethodDefinition &&
                            f.GetParameters().Length == aggFlows.Count);

            var genericArgs =
                new[] { keyType }.Concat(
                        aggFlows.Select(f => f.OutputType.GetGenericArguments()[1]))
                    .ToArray();

            var finalMethod = method.MakeGenericMethod(genericArgs);

            finalAggFlow = (Flow)finalMethod.Invoke(null, aggFlows.ToArray())!;
        }
        else
        {
            finalAggFlow = aggFlows[0];
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
                var aggIdx = Array.IndexOf(aggregates, agg);
                return (Left: false, aggIdx);
            }
            throw new ArgumentException($"Unknown return type: {ret.GetType()}");
        }).ToArray();

        var finalSelector = TupleHelpers.ResultKeyedSelector(
            finalAggFlow.OutputType,
            indices);

        var finalFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(
                finalAggFlow.OutputType,
                finalResultType)
            .Invoke(null, new object[]
            {
                finalAggFlow,
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

    public Pattern Join(Flow rightFlow, bool innerJoin, params ReadOnlySpan<LVar> lvars)
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

        var methodName = innerJoin ? nameof(FlowExtensions.Join) : nameof(FlowExtensions.OuterJoin);

        var joinFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(methodName)
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
