using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public static class StageExtensions
{

    public static Filter<T> Filter<T>(this ISingleOutputStage<T> upstream, Func<T, bool> predicate)
        where T : notnull
    {
        return new Filter<T>(predicate, upstream.Output);
    }

    public static Outlet<T> Outlet<T>(this ISingleOutputStage<T> upstream)
        where T : notnull
    {
        return new Outlet<T>(upstream.Output);
    }

}
