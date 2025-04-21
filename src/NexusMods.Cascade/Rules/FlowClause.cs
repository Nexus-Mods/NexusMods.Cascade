using System;

namespace NexusMods.Cascade.Rules;

public class FlowClause<T> : Clause<T>
{
    private readonly Flow<T> _flow;

    public FlowClause(Flow<T> flow, LVar<T> lvar1) : base(lvar1)
    {
        _flow = flow;
    }

    public override Flow<T> GetFlowCore()
    {
        return _flow;
    }

    public override Type ReturnType => typeof(T);
}


public class FlowClause<T1, T2> : Clause<T1, T2>
{
    private readonly Flow<(T1, T2)> _flow;

    public override Type ReturnType => typeof((T1, T2));

    public FlowClause(Flow<(T1, T2)> flow, LVar<T1> lvar1, LVar<T2> lvar2) : base(lvar1, lvar2)
    {
        _flow = flow;
    }

    public override Flow<(T1, T2)> GetFlowCore()
    {
        return _flow;
    }

}
