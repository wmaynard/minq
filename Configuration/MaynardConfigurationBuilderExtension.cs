using System;
using System.Linq;
using System.Reflection;
using Maynard.Configuration;
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
        
        Type[] types = Assembly
           .GetEntryAssembly()
           ?.GetExportedTypes() 
           .Concat(Assembly.GetExecutingAssembly().GetExportedTypes())
           .Where(type => type.IsClass && !type.IsAbstract)
           .Where(type => !type.ContainsGenericParameters) // Filter out open generic types
           .Where(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Any(p => p.GetCustomAttribute<FlexKeys>() != null)
           )
           .ToArray()
           ?? [];

        foreach (Type type in types)
        {
            try
            {
                Type serializerType = typeof(FlexKeysBsonSerializer<>).MakeGenericType(type);
                IBsonSerializer serializer = (IBsonSerializer)Activator.CreateInstance(serializerType);
                BsonSerializer.RegisterSerializer(type, serializer);
            }
            catch (Exception e)
            {
                Log.Error("Unable to add BSON serializer", data: new
                {
                    Type = type.FullName
                }, exception: e);
            }
        }

        Log.Good("Mongo configured successfully!");
        return builder;
    }
}