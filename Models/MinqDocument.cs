using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Minq.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Maynard.Minq.Models;

public abstract class MinqDocument : FlexModel
{
    public const string DB_KEY_CREATED_ON = "created";
    public const string FRIENDLY_KEY_CREATED_ON = "createdOn";
    
    [MinqView(Sticky = true, Order = int.MinValue, ReadOnly = true)]
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    [FlexIgnore(Ignore.Never)]
    public string Id { get; internal set; }
    
    [MinqView(Order = int.MaxValue, ReadOnly = true)]
    [FlexKeys(json: "createdOn", bson: "created")]
    public long CreatedOn { get; set; }
    
    [MinqView(Order = int.MaxValue, ReadOnly = true)]
    [FlexKeys(json: "updatedOn", bson: "updated")]
    public long UpdatedOn { get; set; }

    public void ChangeId() => Id = ObjectId.GenerateNewId().ToString();
    
    [FlexKeys(json: "cachedUntil", Ignore = Ignore.InBson | Ignore.WhenJsonNullOrDefault)]
    public long CachedUntil { get; set; }
}