using System.Text.Json.Serialization;
using Maynard.Json;
using Maynard.Json.Attributes;
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
    [FlexIgnore(Ignore.Never)]
    public string Id { get; protected set; }
    
    [FlexKeys(json: "createdOn", bson: "created")]
    public long CreatedOn { get; set; }

    public void ChangeId() => Id = ObjectId.GenerateNewId().ToString();
    
    [FlexKeys(json: "cachedUntil", Ignore = Ignore.InBson | Ignore.WhenJsonNullOrDefault)]
    public long CachedUntil { get; set; }
}