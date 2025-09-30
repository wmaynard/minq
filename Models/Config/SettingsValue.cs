using System.Text.Json.Serialization;
using Maynard.Json;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.Common.Models.Config;

[BsonIgnoreExtraElements]
public class SettingsValue : Model
{
    [BsonElement("value")]
    [JsonInclude, JsonPropertyName("value")]
    public string Value { get; init; }
    
    [BsonElement("comment")]
    [JsonInclude, JsonPropertyName("comment")]
    public string Comment { get; set; }

    [BsonConstructor, JsonConstructor]
    public SettingsValue(string value = null, string comment = null)
    {
        Value = value;
        Comment = comment;
    }
}