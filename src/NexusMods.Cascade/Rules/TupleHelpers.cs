using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Structures;

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

        if (baseType.IsGenericType)
        {
            var genericType = baseType.GetGenericTypeDefinition();
            if (genericType == typeof(KeyedValue<,>))
            {
                if (idx < 0 || idx >= 2)
                    throw new ArgumentOutOfRangeException(nameof(idx), $"Index {idx} is out of range for type {baseType.Name}.");
                return baseType.GenericTypeArguments[idx];
            }
        }

        if (idx == 0)
            return baseType;

        throw new NotSupportedException($"Type {baseType.Name} is not supported.");
    }

    public static Expression Getter(Type baseType, Expression src, int index)
    {
        if (baseType.IsAssignableTo(typeof(ITuple)))
            return Expression.PropertyOrField(src, "Item" + (index + 1));
        if (baseType.IsGenericType)
        {
            var types = baseType.GetGenericArguments();
            if (baseType.GetGenericTypeDefinition() == typeof(KeyedValue<,>))
            {
                if (index == 0)
                    return Expression.PropertyOrField(src, "Key");
                if (index == 1)
                    return Expression.PropertyOrField(src, "Value");
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for type {baseType.Name}.");
                }
            }
        }
        if (index == 0)
            return src;
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

    /// <summary>
    /// Makes a delegate that takes a tuple of the given type, and appends the value created by the xform to the end of the tuple.
    /// Creating a new tuple of the output type
    /// </summary>
    public static Delegate TupleAppendFn(Type input, Type[] inputTypes, Type output, Delegate xform, int srcIdx)
    {
        var inputParam = Expression.Parameter(input, "input");

        var getterExprs = new List<Expression>();

        for (var i = 0; i < inputTypes.Length; i++)
        {
            var expr = Getter(input, inputParam, i);
            getterExprs.Add(expr);
        }

        getterExprs.Add(Expression.Call(Expression.Constant(xform), "Invoke", [], Getter(input, inputParam, srcIdx)));

        var tuple = Expression.New(output.GetConstructors().First(), getterExprs);

        var lambda = Expression.Lambda(tuple, inputParam);
        return lambda.Compile();
    }

    public static Delegate AggGetterFn(Type rekeyedOutputType, int idx)
    {
        var param = Expression.Parameter(rekeyedOutputType, "input");
        var retVal = Getter(rekeyedOutputType, param, idx);
        var lambda = Expression.Lambda(retVal, param);
        return lambda.Compile();
    }

    public static object ResultKeyedSelector(Type keyedType, (bool Left, int idx)[] selectors)
    {
        var outputTypes = new List<Type>();

        var input1 = keyedType.GetGenericArguments()[0];
        var input2 = keyedType.GetGenericArguments()[1];

        var keyedInput = Expression.Parameter(typeof(KeyedValue<,>).MakeGenericType(input1, input2), "keyedInput");

        var input1Param = Expression.PropertyOrField(keyedInput, "Key");
        var input2Param = Expression.PropertyOrField(keyedInput, "Value");

        for (var i = 0; i < selectors.Length; i++)
        {
            var inputType = selectors[i].Left ? input1 : input2;
            outputTypes.Add(IndexType(inputType, selectors[i].idx));
        }

        var outputType = TupleTypeFor(outputTypes.ToArray());

        var argExprs = new List<Expression>();

        for (var i = 0; i < selectors.Length; i++)
        {
            var expr = Getter(selectors[i].Left ? input1 : input2, selectors[i].Left ? input1Param : input2Param, selectors[i].idx);
            argExprs.Add(expr);
        }

        var tuple = Expression.New(outputType.GetConstructors().First(), argExprs);
        var lambda = Expression.Lambda(tuple, keyedInput);
        return lambda.Compile();
    }
}
