using System;

namespace Maynard.Minq.Indexing.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = true)]
public class AdditionalIndexKey(string group, string key, int priority, bool ascending = true) : Attribute
{
    internal bool Ascending { get; set; } = ascending;
    internal string GroupName { get; set; } = group;
    internal string DatabaseKey { get; set; } = key;
    internal int Priority { get; set; } = priority;
}