using System;

namespace Maynard.Minq.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MinqViewAttribute(bool sticky = false, int order = int.MaxValue) : Attribute
{
    public bool Sticky { get; set; } = sticky;
    public int Order { get; set; } = order;
}