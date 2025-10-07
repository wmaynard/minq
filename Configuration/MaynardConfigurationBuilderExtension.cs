using System;
using System.Linq;
using System.Reflection;
using Maynard.Configuration;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Serializers;
using Maynard.Logging;
using MongoDB.Bson.Serialization;

namespace Maynard.Minq.Configuration;

public static class MaynardConfigurationBuilderExtension
{
    public static MaynardConfigurationBuilder ConfigureMinq(this MaynardConfigurationBuilder builder, Action<MinqConfigurationBuilder> mongo)
    {
        mongo.Invoke(new());
        
        Log.Verbose("Applying BSON converters...");
        BsonSerializer.RegisterSerializer(new BsonGenericConverter());

        FlexModel.RegisterModelsWithMongo();

        Log.Good("Mongo configured successfully!");
        return builder;
    }
}