using System.Text.RegularExpressions;
using Maynard.Configuration;
using Maynard.Logging;
using Maynard.Singletons;
using MongoDB.Driver;
using Rumble.Platform.Common.MinqOld;

namespace Maynard.Minq.Extensions;

public class MinqConfigurationBuilder : Builder
{
    public MinqConfigurationBuilder Connect(string connectionString) => OnceOnly<MinqConfigurationBuilder>(() =>
    {
        string pattern = @"^mongodb(\+srv)?://(?:([^:]+):([^@]+)@)?([^/,]+(?:,[^/,]+)*)/([^?]+)(?:\?(.*))?$";

        Match match = Regex.Match(connectionString, pattern);

        if (!match.Success)
        {
            Log.Alert("Unable to parse MongoDB connection string.  All database connections will fail.");
            return;
        }
        
        string protocol = match.Groups[1].Value;
        string username = match.Groups[2].Value;
        string password = match.Groups[3].Value;
        string[] hosts = match.Groups[4].Value.Split(',');
        string database = match.Groups[5].Value;
        string queryParams = match.Groups[6].Value;
        
        MinqConnection.Client = new MongoClient(connectionString);
        MinqConnection.Database = MinqConnection.Client.GetDatabase(database);
        Log.Good("Connected to MongoDB.");
        
        
        
    });
}