namespace NexusMods.Cascade.Pattern;

public static class ReturnPatternExtensions
{

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2)> Return<T1, T2>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2)
    {
        return (Flow<(T1, T2)>)pattern.CompileReturn(lvar1, lvar2);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3)> Return<T1, T2, T3>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3)
    {
        return (Flow<(T1, T2, T3)>)pattern.CompileReturn(lvar1, lvar2, lvar3);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3, T4)> Return<T1, T2, T3, T4>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4)
    {
        return (Flow<(T1, T2, T3, T4)>)pattern.CompileReturn(lvar1, lvar2, lvar3, lvar4);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3, T4, T5)> Return<T1, T2, T3, T4, T5>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4, IReturnValue<T5> lvar5)
    {
        return (Flow<(T1, T2, T3, T4, T5)>)pattern.CompileReturn(lvar1, lvar2, lvar3, lvar4, lvar5);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3, T4, T5, T6)> Return<T1, T2, T3, T4, T5, T6>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4, IReturnValue<T5> lvar5, IReturnValue<T6> lvar6)
    {
        return (Flow<(T1, T2, T3, T4, T5, T6)>)pattern.CompileReturn(lvar1, lvar2, lvar3, lvar4, lvar5, lvar6);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3, T4, T5, T6, T7)> Return<T1, T2, T3, T4, T5, T6, T7>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4, IReturnValue<T5> lvar5, IReturnValue<T6> lvar6, IReturnValue<T7> lvar7)
    {
        return (Flow<(T1, T2, T3, T4, T5, T6, T7)>)pattern.CompileReturn(lvar1, lvar2, lvar3, lvar4, lvar5, lvar6, lvar7);
    }

    /// <summary>
    /// Finishes the pattern definition and creates a flow from the proceeding pattern. If any of the lvars are transformed
    /// into aggregates (via calling an aggregate operator on the lvar) then the flow will be an aggregation flow, where
    /// the non-aggregate lvars are the keys and the aggregate lvars are the values.
    /// </summary>
    public static Flow<(T1, T2, T3, T4, T5, T6, T7, T8)> Return<T1, T2, T3, T4, T5, T6, T7, T8>(this Pattern pattern, IReturnValue<T1> lvar1, IReturnValue<T2> lvar2, IReturnValue<T3> lvar3, IReturnValue<T4> lvar4, IReturnValue<T5> lvar5, IReturnValue<T6> lvar6, IReturnValue<T7> lvar7, IReturnValue<T8> lvar8)
    {
        return (Flow<(T1, T2, T3, T4, T5, T6, T7, T8)>)pattern.CompileReturn(lvar1, lvar2, lvar3, lvar4, lvar5, lvar6, lvar7, lvar8);
    }
}
