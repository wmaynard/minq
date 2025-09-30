using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Json;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.Exceptions;

public class MinqException : Exception // TODO: Should probably be an abstract class
{
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Endpoint { get; private set; }
  
    public MinqException() : this("No message provided."){}
#pragma warning disable CS0618
    public MinqException(string message, Exception inner = null) : base(message, inner)
    {
    }
#pragma warning restore CS0618

    public string Detail
    {
        get
        {
            if (InnerException == null)
                return null;
            string output = "";
            string separator = " | ";
      
            Exception inner = InnerException;
            do
            {
                output += $"({inner.GetType().Name}) {inner.Message}{separator}";
            } while ((inner = inner.InnerException) != null);
      
            output = output[..^separator.Length];
            return output;
        }
    }

    internal new FlexJson Data
    {
        get
        {
            FlexJson output = new();
            foreach (PropertyInfo info in GetType().GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                output[JsonNamingPolicy.CamelCase.ConvertName(info.Name)] = info.GetValue(this);
            return output;
        }
    }
}