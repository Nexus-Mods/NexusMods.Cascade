using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Rules;

public static class TupleHelpers
{

    public static Delegate Selector(Type input, int[] indexes)
    {
        var outputTypes = new List<Type>();

        for (var i = 0; i < indexes.Length; i++)
        {
            outputTypes.Add(IndexType(input, indexes[i]));
        }

        var outputType = TupleTypeFor(outputTypes.ToArray());

        var argExprs = new List<Expression>();

        var inputParam = Expression.Parameter(input, "input");
        for (var i = 0; i < indexes.Length; i++)
        {
            var expr = Getter(input, inputParam, indexes[i]);
            argExprs.Add(expr);
        }
        var tuple = Expression.New(outputType.GetConstructors().First(), argExprs);

        var lambda = Expression.Lambda(tuple, inputParam);
        return lambda.Compile();
    }

    public static Delegate ResultSelector(Type input1, Type input2, (bool Left, int idx)[] selectors)
    {
        var outputTypes = new List<Type>();

        for (var i = 0; i < selectors.Length; i++)
        {
            var inputType = selectors[i].Left ? input1 : input2;
            outputTypes.Add(IndexType(inputType, selectors[i].idx));
        }

        var outputType = TupleTypeFor(outputTypes.ToArray());

        var argExprs = new List<Expression>();
        var input1Param = Expression.Parameter(input1, "input1");
        var input2Param = Expression.Parameter(input2, "input2");

        for (var i = 0; i < selectors.Length; i++)
        {
            var expr = Getter(selectors[i].Left ? input1 : input2, selectors[i].Left ? input1Param : input2Param, selectors[i].idx);
            argExprs.Add(expr);
        }

        var tuple = Expression.New(outputType.GetConstructors().First(), argExprs);
        var lambda = Expression.Lambda(tuple, input1Param, input2Param);
        return lambda.Compile();
    }

    private static Type IndexType(Type baseType, int idx)
    {
        if (baseType.IsAssignableTo(typeof(ITuple)))
        {
            var genericArgs = baseType.GenericTypeArguments;
            if (idx < 0 || idx >= genericArgs.Length)
                throw new ArgumentOutOfRangeException(nameof(idx), $"Index {idx} is out of range for type {baseType.Name}.");
            return genericArgs[idx];
        }
        throw new NotSupportedException($"Type {baseType.Name} is not supported.");
    }

    private static Expression Getter(Type baseType, Expression src, int index)
    {
        if (baseType.IsAssignableTo(typeof(ITuple)))
            return Expression.PropertyOrField(src, "Item" + (index + 1));
        throw new NotSupportedException($"Type {baseType.Name} is not supported.");
    }

    public static Type TupleTypeFor(Type[] types)
    {
        return types.Length switch
        {
            0 => throw new ArgumentException("Tuple must have at least one element.", nameof(types)),
            1 => typeof(ValueTuple<>).MakeGenericType(types[0]),
            2 => typeof(ValueTuple<,>).MakeGenericType(types[0], types[1]),
            3 => typeof(ValueTuple<,,>).MakeGenericType(types[0], types[1], types[2]),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(types[0], types[1], types[2], types[3]),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], types[4]),
            _ => throw new NotImplementedException()
        };
    }
}
