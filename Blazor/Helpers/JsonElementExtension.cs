using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Maynard.Json;

namespace Maynard.Minq.Blazor.Helpers;

internal static class ObjectExtension
{
    internal static bool IsDefault(this object element)
    {
        if (element == null) 
            return true;

        return element switch
        {
            string s => s switch
            {
                "" => true,
                "00000000-0000-0000-0000-000000000000" => true,
                "0001-01-01T00:00:00" => true,
                "0001-01-01T00:00:00Z" => true,
                _ => false
            },
            double d => d == 0,
            float f => f == 0,
            int i => i == 0,
            long l => l == 0,
            decimal dec => dec == 0,
            bool => false, // Matching original JsonElement logic where false is not "default"
            FlexJson fj => !fj.Keys.Any(),
            IDictionary dict => dict.Count == 0,
            IEnumerable e => !e.Cast<object>().Any(),
            _ => false
        };
    }
}