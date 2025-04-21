using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Rules;

public class RuleDefinition
{
    public List<Clause> Clauses { get; } = new();

    public LVar[] ReturnLVars { get; set; } = [];


    public RuleDefinition With<T>(Flow<T> flow, LVar<T> lvar)
    {
        Clauses.Add(new FlowClause<T>(flow, lvar));
        return this;
    }

    public RuleDefinition With<T1, T2>(Flow<(T1, T2)> flow, LVar<T1> lvar1, LVar<T2> lvar2)
    {
        Clauses.Add(new FlowClause<T1, T2>(flow, lvar1, lvar2));
        return this;
    }

    public RuleDefinition With<T1, T2>(Flow<(T1, T2)> flow, out LVar<T1> lvar1, LVar<T2> lvar2, [CallerArgumentExpression(nameof(lvar1))] string? lvar1Name = null)
    {
        lvar1 = new LVar<T1>(lvar1Name);
        Clauses.Add(new FlowClause<T1, T2>(flow, lvar1, lvar2));
        return this;
    }

    public RuleDefinition With<T1, T2>(Flow<(T1, T2)> flow, out LVar<T1> lvar1, out LVar<T2> lvar2,
        [CallerArgumentExpression(nameof(lvar1))] string? lvar1Name = null,
        [CallerArgumentExpression(nameof(lvar2))] string? lvar2Name = null)
    {
        lvar1 = new LVar<T1>(lvar1Name);
        lvar2 = new LVar<T2>(lvar2Name);
        Clauses.Add(new FlowClause<T1, T2>(flow, lvar1, lvar2));
        return this;
    }

    public Flow<T1> Return<T1>(T1 v1)
    {
        throw new NotImplementedException();
    }

    public Flow<(T1, T2)> Return<T1, T2>(LVar<T1> v1, LVar<T2> v2)
    {
        ReturnLVars = [v1, v2];

        return (Flow<(T1, T2)>)Compiler.Compile(this);
    }


    public Flow<T2> Return<T1, T2>(T1 v1, T2 v2)
    {
        throw new NotImplementedException();
    }
}
