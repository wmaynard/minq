using System;
using System.Collections.Generic;
using Maynard.Minq.Blazor.Models;

namespace Maynard.Minq.Blazor.Helpers;

internal sealed class ColumnComparer(Dictionary<string, MinqColumnDefinition> columnDefinitions) : IComparer<string>
{
    public int Compare(string a, string b)
    {
        MinqColumnDefinition definitionA = columnDefinitions.TryGetValue(a, out MinqColumnDefinition _a) ? _a : new();
        MinqColumnDefinition definitionB = columnDefinitions.TryGetValue(b, out MinqColumnDefinition _b) ? _b : new();

        if (definitionA.IsSticky != definitionB.IsSticky)
            return definitionA.IsSticky ? -1 : 1;

        int minLen = Math.Min(definitionA.OrderPath.Count, definitionB.OrderPath.Count);
        for (int i = 0; i < minLen; i++)
            if (definitionA.OrderPath[i] != definitionB.OrderPath[i])
                return definitionA.OrderPath[i].CompareTo(definitionB.OrderPath[i]);

        if (definitionA.OrderPath.Count != definitionB.OrderPath.Count)
            return definitionA.OrderPath.Count.CompareTo(definitionB.OrderPath.Count);

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
