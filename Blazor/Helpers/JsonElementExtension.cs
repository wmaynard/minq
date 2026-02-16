using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Maynard.Minq.Blazor.Models;

namespace Maynard.Minq.Blazor.Helpers;

internal static class JsonElementExtension
{
    internal static bool IsDefault(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            case JsonValueKind.String:
                return  element.GetString() switch
                {
                    null => true,
                    "" => true,
                    "00000000-0000-0000-0000-000000000000" => true,
                    "0001-01-01T00:00:00" => true,
                    "0001-01-01T00:00:00Z" => true,
                    _ => false
                };
            case JsonValueKind.Number:
                return element.TryGetDouble(out double num) && num == 0;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Array:
                return element.GetArrayLength() == 0;
            case JsonValueKind.Object:
                return !element.EnumerateObject().Any();
            case JsonValueKind.True:
            default:
                return false;
        }
    }
}