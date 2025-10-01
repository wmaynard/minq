using System.Text.Json.Serialization;
using Maynard.Json;
using Maynard.Json.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Rumble.Platform.Common.Utilities.JsonTools;

public abstract class MinqDocument : Model
{
    public const string DB_KEY_CREATED_ON = "created";
    public const string FRIENDLY_KEY_CREATED_ON = "createdOn";
    
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    [JsonInclude]
    public string Id { get; protected set; }
    
    [BsonElement(DB_KEY_CREATED_ON)]
    [JsonInclude, JsonPropertyName(FRIENDLY_KEY_CREATED_ON)]
    public long CreatedOn { get; set; }

    public void ChangeId() => Id = ObjectId.GenerateNewId().ToString();
    
    [BsonIgnore]
    [JsonInclude, JsonPropertyName("cachedUntil"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long CachedUntil { get; set; }
}