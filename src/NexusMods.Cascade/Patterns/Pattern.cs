using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Patterns;

/// <summary>
/// A pattern is a DSL for generating logic queries for Cascade. It works something like a statically typed datalog
/// where LVars are created to represent unknown values in the pattern, and these are use to create constraints between
/// the various clauses. This pattern is then converted to a Flow whenever Return is called.
/// </summary>
public record Pattern
{
    /// <summary>
    /// Start the definition of a new pattern
    /// </summary>
    public static Pattern Create() => new();

    internal ImmutableDictionary<LVar, int> _mappings = ImmutableDictionary<LVar, int>.Empty;

    internal Flow? _flow = null;

    /// <summary>
    /// Define a LVar for use later on in the pattern
    /// </summary>
    public Pattern Define<T>(out LVar<T> variable, [CallerArgumentExpression(nameof(variable))] string? name = null)
    {
        variable = LVar<T>.Create(name ?? string.Empty);
        return this;
    }

    /// <summary>
    /// Adds a given join to the pattern
    /// </summary>
    internal Pattern With<T>(Flow<T> flow, params ReadOnlySpan<LVar> lvars)
    {
        if (_flow is null)
        {
            var newmappings = _mappings;
            foreach (var lvar in lvars)
            {
                if (!newmappings.ContainsKey(lvar))
                {
                    newmappings = newmappings.Add(lvar, newmappings.Count);
                }
            }
            return new Pattern()
            {
                _mappings = newmappings,
                _flow = flow
            };
        }


        return Join(flow, true, lvars);
    }

    /// <summary>
    /// A logical "select" or "map", the given lvar is pulled from the environment and handed to the projector for transforming
    /// the transformed value is bound to the output lvar
    /// </summary>
    public Pattern Project<TIn, TOut>(LVar<TIn> lvar, Func<TIn, TOut> projector, out LVar<TOut> output,
        [CallerArgumentExpression(nameof(output))] string? name = null,
        [CallerArgumentExpression(nameof(output))] string? projecterExpr = null)
    {
        output = LVar<TOut>.Create(name ?? string.Empty);

        var newMappings = _mappings.Add(output, _mappings.Count);

        var inputType = _flow!.OutputType;
        var outputType = TupleHelpers.TupleTypeFor(newMappings.OrderBy(kv => kv.Value).Select(kv => kv.Key.Type).ToArray());
        var srcIdx = _mappings[lvar];
        var xform = TupleHelpers.TupleAppendFn(inputType, inputType.GetGenericArguments(), outputType, projector, srcIdx);

        var flow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))!
            .MakeGenericMethod(inputType, outputType)
            .Invoke(null, [_flow, xform, projecterExpr ?? "<unknown>", "", 0])!;

        return new Pattern
        {
            _mappings = newMappings,
            _flow = flow
        };
    }


    public Flow CompileReturn(Type? returnType, params IReturnValue[] retVals)
    {
        var lvars = retVals.OfType<LVar>().ToArray();
        if (lvars.Length == retVals.Length)
            return CompileDirectReturn(returnType, lvars);

        var keyVars = retVals.OfType<LVar>().ToArray();
        var aggregates = retVals.OfType<IAggregate>().ToArray();

        return CompileAggregate(keyVars, aggregates, retVals, returnType);
    }

    /// <summary>
    /// Compiles an aggregate
    /// </summary>
    private Flow CompileAggregate(LVar[] keyVars, IAggregate[] aggregates, IReturnValue[] outputOrder, Type? finalResultType)
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
            .Invoke(null, [_flow, keyFn, $"({keyExpr})", "", 0])!;

        // Process each aggregate separately.
        List<Flow> aggFlows = [];
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
                    .Invoke(null, [rekeyed, "", 0])!;
            }
            else if (agg.AggregateType == IAggregate.AggregateTypes.Max)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.MaxOf))
                    ?.MakeGenericMethod(keyType, _flow.OutputType, agg.SourceType)
                    .Invoke(null, [rekeyed, getter, agg.Source.ToString()])!;
            }
            else if (agg.AggregateType == IAggregate.AggregateTypes.Sum)
            {
                aggFlow = (Flow)typeof(FlowExtensions)
                    .GetMethod(nameof(FlowExtensions.SumOf))
                    ?.MakeGenericMethod(keyType, _flow.OutputType, agg.SourceType)
                    .Invoke(null, [rekeyed, getter, agg.Source.ToString()])!;
            }
            else
            {
                throw new NotSupportedException($"Aggregate type '{agg.AggregateType}' is not supported.");
            }
            aggFlows.Add(aggFlow);
        }


        // Combine all the flows into a single join
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
        finalResultType ??= TupleHelpers.TupleTypeFor(finalResultTypes);

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
            indices,
            finalResultType);

        var finalFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(
                finalAggFlow.OutputType,
                finalResultType)
            .Invoke(null, [
                finalAggFlow,
                finalSelector,
                "<unknown>",
                "",
                0
            ])!;

        return finalFlow;

    }

    /// <summary>
    /// Compiles a return with no aggregates
    /// </summary>
    private Flow CompileDirectReturn(Type? returnType, LVar[] lvars)
    {
        var retIdxes = lvars.Select(lvar => _mappings[lvar]).ToArray();
        returnType ??= TupleHelpers.TupleTypeFor(lvars.Select(lvar => lvar.Type).ToArray());
        var retFn = TupleHelpers.Selector(_flow!.OutputType, retIdxes, returnType);

        var resultFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(_flow.OutputType, returnType)
            .Invoke(null, [_flow, retFn, "<unknown>", "", 0])!;

        return resultFlow;
    }

    internal Pattern Where(Func<Expression, Expression, Expression> comparison, LVar left, LVar right)
    {
        var predicate = TupleHelpers.MakeItemCompareFn(
            _flow!.OutputType,
            _mappings[left],
            _mappings[right],
            comparison);

        var whereFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Where))!
            .MakeGenericMethod(_flow.OutputType)
            .Invoke(null, [_flow, predicate, "Where", "", 0])!;

        return new Pattern
        {
            _mappings = _mappings,
            _flow = whereFlow
        };
    }

    internal Pattern Where(Func<Expression, Expression> comparison, LVar testLVar)
    {
        var predicate = TupleHelpers.MakeFilter(
            _flow!.OutputType,
            _mappings[testLVar],
            comparison);

        var whereFlow = (Flow)typeof(FlowExtensions)
            .GetMethod(nameof(FlowExtensions.Where))!
            .MakeGenericMethod(_flow.OutputType)
            .Invoke(null, [_flow, predicate, "Where", "", 0])!;

        return new Pattern
        {
            _mappings = _mappings,
            _flow = whereFlow
        };
    }

    /// <summary>
    /// Performs a join between two flows
    /// </summary>
    public Pattern Join(Flow rightFlow, bool innerJoin, params ReadOnlySpan<LVar> lvars)
    {
        var lvarsArray = lvars.ToArray();
        var newMappings = _mappings;
        foreach (var lvar in lvars)
        {
            if (!newMappings.ContainsKey(lvar))
            {
                newMappings = newMappings.Add(lvar, newMappings.Count);
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
            _mappings = newMappings,
            _flow = joinFlow
        };
    }
}
