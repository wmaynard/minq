using System;
using System.Text.Json.Serialization;
using Maynard.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Maynard.Minq.Models;

public class MongoIndexModel : FlexModel
{
    [BsonElement("v")]
    [JsonPropertyName("v")]
    public int Version { get; set; }
    
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [BsonElement("ns")]
    [JsonPropertyName("ns")]
    public string Namespace { get; set; }

    [BsonElement("key")]
    [JsonPropertyName("key")]
    public FlexJson KeyInformation { get; set; }
    
    [BsonElement("unique")]
    [JsonPropertyName("unique")]
    public bool Unique { get; set; }

    public bool IsText => KeyInformation?.Optional<string>("_fts") == "text";
    public bool IsSimple => KeyInformation?.Optional<string>("_fts") != "text" && KeyInformation?.Keys.Count == 1;
    public bool IsCompound => !IsText && (KeyInformation?.Keys.Count ?? 0) > 1;

    internal static MongoIndexModel[] FromCollection<T>(IMongoCollection<T> collection)
    {
        try
        {
            return ((FlexJson)$"{{\"data\":{collection.Indexes.List().ToList().ToJson()}}}").Require<MongoIndexModel[]>("data");
        }
        catch
        {
            return [];
        }
    }
}