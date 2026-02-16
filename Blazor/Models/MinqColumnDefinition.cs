using System;
using System.Collections.Generic;
using System.Reflection;

namespace Maynard.Minq.Blazor.Models;

public class MinqColumnDefinition
{
    public string Name { get; set; }
    public string PropertyName { get; set; }
    public string BsonName { get; set; }
    public string JsonName { get; set; }
    public PropertyInfo[] PropertyPath { get; set; } = [];
    public Type PropertyType { get; set; }
    public bool IsIgnored { get; set; }
    public bool IsJsonIgnored { get; set; }
    public bool IsBsonIgnored { get; set; }
    public bool IsSticky { get; set; }
    public bool ReadOnly { get; set; }
    public List<int> OrderPath { get; set; } = [int.MaxValue];
    public bool IsTimestamp { get; set; }
    public bool IsBool { get; set; }
    public bool IsNested { get; set; }
    public bool IsComplex { get; set; }
}