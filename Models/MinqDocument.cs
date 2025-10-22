using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Maynard.Minq.Models;

public abstract class MinqDocument : FlexModel
{
    public const string DB_KEY_CREATED_ON = "created";
    public const string FRIENDLY_KEY_CREATED_ON = "createdOn";
    
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    [FlexIgnore(Ignore.Never)]
    public string Id { get; internal set; }
    
    [FlexKeys(json: "createdOn", bson: "created")]
    public long CreatedOn { get; set; }

    public void ChangeId() => Id = ObjectId.GenerateNewId().ToString();
    
    [FlexKeys(json: "cachedUntil", Ignore = Ignore.InBson | Ignore.WhenJsonNullOrDefault)]
    public long CachedUntil { get; set; }
}