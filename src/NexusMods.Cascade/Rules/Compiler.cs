using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Rules;

public static class Compiler
{
    public static Flow Compile(RuleDefinition definition)
    {
        // The the input
        var inputEnvironment = new List<LVar[]>();
        var joinVars = new List<LVar[]>();
        var outputEnvironment = new List<LVar[]>();

        // Empty input environment at the start
        inputEnvironment.Add([]);

        foreach (var clause in definition.Clauses)
        {
            var input = inputEnvironment[^1];
            joinVars.Add(clause.LVars.Intersect(input).ToArray());
            outputEnvironment.Add(input.Union(clause.LVars).ToArray());
            inputEnvironment.Add(outputEnvironment[^1]);
        }
        var accClause = definition.Clauses[0];

        var accFlow = accClause.GetFlow();
        var accType = accClause.ReturnType;

        for (int i = 1; i < definition.Clauses.Count; i++)
        {
            var clause = definition.Clauses[i];
            var input = inputEnvironment[i];
            var output = outputEnvironment[i];

            // Get the join variables for the current clause
            var joinVarsForClause = joinVars[i];

            var keyType = TupleHelpers.TupleTypeFor(joinVarsForClause.Select(v => v.Type).ToArray());
            var outputType = TupleHelpers.TupleTypeFor(output.Select(v => v.Type).ToArray());


            // Get the flow for the current clause
            var flow = clause.GetFlow();


            // Create a key function for the left side of the join
            var leftKeyFn = TupleHelpers.Selector(accClause.ReturnType, joinVarsForClause.Select(v => Array.IndexOf(accClause.LVars, v)).ToArray());
            var rightKeyFn = TupleHelpers.Selector(clause.ReturnType, joinVarsForClause.Select(v => Array.IndexOf(clause.LVars, v)).ToArray());

            List<(bool Left, int idx)> selectorMappings = [];

            foreach (var v in output)
            {
                if (input.Contains(v))
                {
                    selectorMappings.Add((true, Array.IndexOf(input, v)));
                }
                else if (clause.LVars.Contains(v))
                {
                    selectorMappings.Add((false, Array.IndexOf(clause.LVars, v)));
                }
            }

            var joinFn = TupleHelpers.ResultSelector(
                accClause.ReturnType,
                clause.ReturnType,
                selectorMappings.ToArray());

            // Create a new flow that combines the first flow and the current flow
            var method = typeof(FlowExtensions).GetMethod(nameof(FlowExtensions.Join))
               ?.MakeGenericMethod(accClause.ReturnType, clause.ReturnType, keyType, outputType);

            accFlow = (Flow)method!.Invoke(null, [accFlow!, flow, leftKeyFn, rightKeyFn, joinFn])!;
            accClause = clause;
            accType = outputType;
        }

        var finalType = TupleHelpers.TupleTypeFor(definition.ReturnLVars.Select(v => v.Type).ToArray());
        var finalIndexes = definition.ReturnLVars.Select(v => Array.IndexOf(outputEnvironment[^1], v)).ToArray();

        var finalFn = TupleHelpers.Selector(accType, finalIndexes);

        var method2 = typeof(FlowExtensions).GetMethod(nameof(FlowExtensions.Select))
            ?.MakeGenericMethod(accType, finalType);
        var finalFlow = (Flow)method2!.Invoke(null, [accFlow!, finalFn, "", "", 1])!;

        return finalFlow;

    }

}
