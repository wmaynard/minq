using System;
using Maynard.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Maynard.Minq.Exceptions;

public class WriteConflictException : Exception
{
    private const string MESSAGE = "Write conflict encountered.  Check that you aren't updating the same field multiple times in one query.";
    
    public string Help { get; init; }
    public FlexJson MongoException { get; init; } 
    
    public WriteConflictException(Exception e) : base(MESSAGE)
    {
        Help = "This probably was a result of updating the same field more than once in the same query.  This is not allowed in Mongo and the driver does not handle it well.  If you're still unclear on what to do, reach out to #platform.";
        if (e is MongoException mongo)
            MongoException = mongo.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });
    }
}