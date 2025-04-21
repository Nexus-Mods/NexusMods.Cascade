using System;

namespace NexusMods.Cascade.Rules;

public abstract class Clause
{
    public Clause(params LVar[] lvars)
    {
        LVars = lvars;
    }

    /// <summary>
    /// The LVars that are used in this clause, in order of appearance in the flow
    /// </summary>
    public LVar[] LVars { get; }

    public abstract Flow GetFlow();

    public abstract Type ReturnType { get; }
}

public abstract class Clause<T1> : Clause
{
    public Clause(LVar<T1> lvar1) : base(lvar1)
    {
    }

    public abstract Flow<T1> GetFlowCore();

    public override Flow GetFlow()
    {
        return GetFlowCore();
    }
}

public abstract class Clause<T1, T2> : Clause
{

    public Clause(LVar<T1> lvar1, LVar<T2> lvar2) : base(lvar1, lvar2)
    {
    }


    public abstract Flow<(T1, T2)> GetFlowCore();

    public override Flow GetFlow()
    {
        return GetFlowCore();
    }
}
