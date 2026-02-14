using System;

namespace Maynard.Minq.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MinqViewAttribute(bool sticky = false) : Attribute
{
    public bool Sticky { get; set; } = sticky;
}