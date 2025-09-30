using System;
using Maynard.Json;
using MongoDB.Bson;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.Models;

/// <summary>
/// An object containing information about keys/records scanned and whether or not the query was covered by indexes.
/// </summary>
public class MongoQueryStats : Model
{
    public bool IndexScan { get; private set; }
    public bool CollectionScan { get; private set; }
    public long DocumentsExamined { get; private set; }
    public long DocumentsReturned { get; private set; }
    public long ExecutionTimeMs { get; private set; }
    public long KeysExamined { get; private set; }

    public bool IsNotCovered => !IndexScan && CollectionScan;
    public bool IsPartiallyCovered => IndexScan && CollectionScan;
    public bool IsFullyCovered => IndexScan || !CollectionScan;

    // When Mongo is initializing, every PlatformDataModel is created with the smallest constructor available to it.
    // Consequently, null reference errors can cause exceptions from the normal constructor on startup.  This silences
    // logs when the service is starting up and hasn't made any queries yet.
    private MongoQueryStats() { }

    public MongoQueryStats(BsonDocument explainResult)
    {
        FlexJson result = explainResult;

        // Get stats for the queries
        try
        {
            FlexJson winningPlan = result
                .Require<FlexJson>("queryPlanner")
                .Require<FlexJson>("winningPlan");
            IndexScan = winningPlan.ContainsValueRecursive("IXSCAN");
            CollectionScan = winningPlan.ContainsValueRecursive("COLLSCAN");
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to parse Mongo explanation component: winning plan", exception: e);
        }
            
        // Parse the execution stats
        try
        {
            FlexJson stats = result.Require<FlexJson>("executionStats");
            DocumentsReturned = stats.Require<FlexJson>("nReturned").Require<long>("$numberInt");
            DocumentsExamined = stats.Require<FlexJson>("totalDocsExamined").Require<long>("$numberInt");
            ExecutionTimeMs = stats.Require<FlexJson>("executionTimeMillis").Require<long>("$numberInt");
            KeysExamined = stats.Require<FlexJson>("totalKeysExamined").Require<long>("$numberInt");
        }
        catch (Exception e)
        {
            Log.Error(Owner.Will, "Unable to parse Mongo explanation component: execution stats", exception: e);
        }
    }
}