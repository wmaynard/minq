using System;

namespace Maynard.Minq.Indexing.Attributes;

[AttributeUsage(validOn: AttributeTargets.Property)]
public sealed class TextIndex : PlatformMongoIndex
{
    public string[] DatabaseKeys { get; set; }
    public TextIndex() { }
}