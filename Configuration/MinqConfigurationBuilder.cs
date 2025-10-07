using System;
using System.Linq;
using System.Text.RegularExpressions;
using Maynard.Configuration;
using Maynard.Logging;
using MongoDB.Driver;

namespace Maynard.Minq.Configuration;

public partial class MinqConfigurationBuilder : Builder
{
    public MinqConfigurationBuilder Connect(string connectionString) => OnceOnly<MinqConfigurationBuilder>(() =>
    {
        Match match = ConnectionStringRegex().Match(connectionString);

        if (!match.Success)
        {
            Log.Alert("Unable to parse MongoDB connection string.  All database connections will fail.");
            return;
        }
        
        // string protocol = match.Groups[1].Value;
        // string username = match.Groups[2].Value;
        // string password = match.Groups[3].Value;
        // string[] hosts = match.Groups[4].Value.Split(',');
        string database = match.Groups[5].Value;
        // string queryParams = match.Groups[6].Value;

        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ConnectTimeout = TimeSpan.FromSeconds(10);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.SocketTimeout = TimeSpan.FromSeconds(30);
        settings.TranslationOptions = new()
        {
            EnableClientSideProjections = true
        };

        MinqConnection.Client = new(settings);
        MinqConnection.Database = MinqConnection.Client.GetDatabase(database);
        Log.Good("Connected to MongoDB.");
    });
    
    [GeneratedRegex(@"^mongodb(\+srv)?://(?:([^:]+):([^@]+)@)?([^/,]+(?:,[^/,]+)*)/([^?]+)(?:\?(.*))?$")]
    private static partial Regex ConnectionStringRegex();
}