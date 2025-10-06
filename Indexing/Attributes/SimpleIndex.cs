using System;

namespace Maynard.Minq.Indexing.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public sealed class SimpleIndex(bool unique = false, bool ascending = true) : PlatformMongoIndex
{
    public bool Unique { get; init; } = unique;
    public bool Ascending { get; init; } = ascending;

    public override string ToString() => Name;
}