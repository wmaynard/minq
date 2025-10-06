using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Maynard.Json;
using Maynard.Logging;
using Maynard.Minq.Indexing.Attributes;
using Maynard.Minq.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Maynard.Minq.Extensions;

public static class MongoCollectionExtension
{
    public static PlatformMongoIndex[] GetIndexes<T>(this IMongoCollection<T> collection) where T : MinqDocument
    {
        Type type = collection.GetType();
        List<PropertyInfo> candidates = GetIndexCandidates(type).ToList();
        
        candidates.AddRange(type
            .GetGenericArguments()
            .Union(type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(model => model.IsAssignableTo(typeof(FlexModel)))
            .SelectMany(GetIndexCandidates)
        );
        
        // For certain cases, like the QueueService, we also have to check the declaring type to see if there are any private classes that have index attributes.
        candidates.AddRange(type
            .GetGenericArguments()
            .Where(t => t.DeclaringType != null)
            .SelectMany(t => t.DeclaringType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(model => model.IsAssignableTo(typeof(FlexModel)))
            .SelectMany(GetIndexCandidates)
        );

        return FromCandidates(candidates);
    }
    
    private static PlatformMongoIndex[] FromCandidates(IEnumerable<PropertyInfo> candidates)
    {
        List<PlatformMongoIndex> output = new();
        List<PlatformMongoIndex> indexes = new();
        
        foreach (PropertyInfo property in candidates)
            indexes.AddRange(ExtractIndexes(property));
        
        // We can't add all the indexes on their own.  Limitations:
        //     A collection can only support one text index, so we have to combine them.
        //     Our compound indexes have not yet been combined into a comprehensive definition.
        TextIndex[] texts = indexes.OfType<TextIndex>().ToArray();
        CompoundIndex[] compounds = indexes.OfType<CompoundIndex>().ToArray();
        SimpleIndex[] simples = indexes.OfType<SimpleIndex>().ToArray();
        
        output.AddRange(simples);

        // Combine text indexes into one definition.
        if (texts.Any())
            output.Add(new TextIndex
            {
                Name = "text",
                DatabaseKeys = texts
                    .Select(text => text.DatabaseKey)
                    .ToArray()
            });
        // Combine compound indexes into one definition - grouped by their name.
        if (compounds.Any()) 
            output.AddRange(compounds
                .Select(compound => compound.GroupName)
                .Distinct()
                .Select(group => CompoundIndex.Combine(compounds
                    .Where(compound => compound.GroupName == group)
                    .ToArray())
                )
            );
        
        return output.ToArray();
    }
    
    /// <summary>
    /// Recursively pulls indexes from PlatformCollectionDocuments and PlatformDataModels.
    /// </summary>
    /// <param name="property">The property to draw indexes from.  May not necessarily have indexes.</param>
    /// <param name="parentName">The parent's friendly key, for logging purposes.</param>
    /// <param name="parentDbKey">The parent's database key, required to create indexes.</param>
    /// <param name="depth">The maximum depth to create keys for.</param>
    /// <returns>An array of indexes to create.</returns>
    private static PlatformMongoIndex[] ExtractIndexes(PropertyInfo property, string parentName = null, string parentDbKey = null, int depth = 5)
    {
        if (depth <= 0)
        {
            Log.Error("Maximum depth exceeded for Mongo indexes.");
            return Array.Empty<PlatformMongoIndex>();
        }

        BsonElementAttribute bson = property.GetCustomAttribute<BsonElementAttribute>();
        string dbName = bson?.ElementName;
        string friendlyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;

        parentDbKey = string.IsNullOrWhiteSpace(parentDbKey)
            ? dbName
            : $"{parentDbKey}.{dbName}";
        parentName = string.IsNullOrWhiteSpace(parentName)
            ? friendlyName
            : $"{parentName}.{friendlyName}";
        
        List<PlatformMongoIndex> output = property
            .GetCustomAttributes()
            .Where(attribute => attribute.GetType().IsAssignableTo(typeof(PlatformMongoIndex)))
            .Select(attribute => ((PlatformMongoIndex)attribute)
                .SetPropertyName(parentName)
                .SetDatabaseKey(parentDbKey)
            )
            .ToList();
        
        AdditionalIndexKey[] additionalKeys = property
            .GetCustomAttributes()
            .Where(attribute => attribute.GetType().IsAssignableTo(typeof(AdditionalIndexKey)))
            .Select(attribute => (AdditionalIndexKey)attribute)
            .ToArray();

        foreach (CompoundIndex compound in output.OfType<CompoundIndex>())
            compound.AddKeys(additionalKeys.Where(adds => adds.GroupName == compound.GroupName));
        
        if (property.PropertyType.IsAssignableTo(typeof(FlexModel)))
            foreach (PropertyInfo nested in GetIndexCandidates(property.PropertyType))
                output.AddRange(ExtractIndexes(nested, property.Name, parentDbKey, depth - 1));

        if (bson != null)
            return output.ToArray();
        if (output.Any())
            Log.Warn("Unable to create indexes without a BsonElement attribute also present on a property.", data: new
            {
                Name = property.Name
            });
        return Array.Empty<PlatformMongoIndex>();
    }
    
    private static PropertyInfo[] GetIndexCandidates(Type type) => type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(prop => !prop.GetCustomAttributes().Any(att => att.GetType().IsAssignableTo(typeof(BsonIgnoreAttribute))))
        .ToArray();
}