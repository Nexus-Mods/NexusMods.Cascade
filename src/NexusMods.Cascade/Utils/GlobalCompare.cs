using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace NexusMods.Cascade.Abstractions;

public sealed class GlobalCompare
{
    private static ImmutableDictionary<Type, IComparer> _comparers = ImmutableDictionary<Type, IComparer>.Empty;


    public static int Compare<T>(T a, T b)
    {
        if (a is IComparable<T> comparableA)
        {
            return comparableA.CompareTo(b);
        }

        return FallbackCompare(a!, b!);
    }

    private static int FallbackCompare<T>(T a, T b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        var hashA = a?.GetHashCode() ?? 0;
        var hashB = b?.GetHashCode() ?? 0;
        var cmp = hashA.CompareTo(hashB);
        if (cmp != 0)
            return cmp;

        return SlowCompare(a, b);
    }


    private static int SlowCompare<T>(T a, T b)
    {
        if (!_comparers.TryGetValue(a!.GetType(), out var comparer))
            comparer = MakeComparer<T>();
        return comparer.Compare(a, b);
    }

    private static IComparer MakeComparer<T>()
    {
        var getters = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        var paramA = Expression.Parameter(typeof(T), "a");
        var paramB = Expression.Parameter(typeof(T), "b");
        // For each property we're going to call GlobalCompare.Compare on the property values
        // and return any non-zero value, otherwise go to the next property.
        var method = typeof(GlobalCompare).GetMethod(nameof(Compare))!;

        var finalReturn = Expression.Label(typeof(int));

        var block = new List<Expression>();
        var innerParams = new List<ParameterExpression>();
        foreach (var property in getters)
        {
            var getA = Expression.Property(paramA, property);
            var getB = Expression.Property(paramB, property);
            var methodSpecialized = method.MakeGenericMethod(property.PropertyType);
            var call = Expression.Call(methodSpecialized, getA, getB);
            var localName = Expression.Parameter(typeof(int), "cmp_"+property.Name);
            innerParams.Add(localName);
            var assign = Expression.Assign(localName, call);
            var ifTrueReturn = Expression.IfThen(Expression.NotEqual(assign, Expression.Constant(0)), Expression.Return(finalReturn, localName));

            block.Add(assign);
            block.Add(ifTrueReturn);
        }

        block.Add(Expression.Label(finalReturn, Expression.Constant(0)));

        var lambda = Expression.Lambda<Comparison<T>>(Expression.Block(innerParams, block), paramA, paramB);
        var func = lambda.Compile();
        var comparer = Comparer<T>.Create(func);

        do {
            var old = _comparers;
            var newComparers = old.Add(typeof(T), comparer!);
            if (Interlocked.CompareExchange(ref _comparers, newComparers, old) == old)
                break;
        } while (true);

        return comparer;
    }
}

public sealed class GlobalComparer<T> : IComparer<T>
where T : notnull
{
    public static readonly GlobalComparer<T> Instance = new();

    public int Compare(T? x, T? y) => GlobalCompare.Compare(x!, y!);
}
