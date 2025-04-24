using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Abstractions;

public static class PrettyTypePrinter
{
    private static Dictionary<Type, string> shorthandMap = new Dictionary<Type, string>
    {
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(long), "long" },
        { typeof(sbyte), "sbyte" },
        { typeof(short), "short" },
        { typeof(string), "string" },
        { typeof(uint), "uint" },
        { typeof(ulong), "ulong" },
        { typeof(ushort), "ushort" },
    };

    public static string CSharpTypeName(Type type, bool isOut = false)
    {
        if (type.IsByRef)
        {
            return $"{(isOut ? "out" : "ref")} {CSharpTypeName(type.GetElementType()!)}";
        }
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return $"{CSharpTypeName(Nullable.GetUnderlyingType(type)!)}?";
            }
            else if (type.IsAssignableTo(typeof(ITuple)))
            {
                return $"({string.Join(", ", type.GenericTypeArguments.Select(a => CSharpTypeName(a)).ToArray())})";
            }
            else
            {
                return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GenericTypeArguments.Select(a => CSharpTypeName(a)).ToArray())}>";
            }
        }
        if (type.IsArray)
        {
            return $"{CSharpTypeName(type.GetElementType()!)}[]";
        }

        return shorthandMap.TryGetValue(type, out var value) ? value : type.Name;
    }
}
