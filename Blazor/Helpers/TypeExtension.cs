using System;

namespace Maynard.Minq.Blazor.Helpers;

internal static class TypeExtension
{
    internal static bool IsComplex(this Type type)
    {
        while (true)
        {
            if (type == typeof(string)
                || type.IsPrimitive
                || type.IsEnum
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(decimal)
                || type == typeof(Guid)
                || type.IsArray
                || typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
            )
                return false;
            
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
                continue;
            }

            return type.IsClass || type.IsValueType;
        }
    }

    internal static string GetFriendlyName(this Type type)
    {
        if (type.IsByRef)
            type = type.GetElementType() ?? type;

        return type switch
        {
            _ when type.IsArray => $"{type.GetElementType()?.Name}[]",
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(string) => "string",
            _ when type == typeof(bool) => "bool",
            _ => type.Name
        };
    }
}