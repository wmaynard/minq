using System.Linq;
using Maynard.Json;
using Maynard.Json.Attributes;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.Services;

/// <summary>
/// Useful when a service needs to store configuration values specific to itself between runs.
/// This service uses its own MongoDB collection to store RumbleJson values.
/// </summary>
public sealed class ConfigSingleton : MongoSingleton<ConfigSingleton.ServiceConfig>
{
    private ServiceConfig _config;
    private ServiceConfig Config => _config ?? Refresh();
    public static ConfigSingleton Instance { get; private set; }

    public T Value<T>(string key) => Config.Data.Optional<T>(key);
    private object Value(string key) => Config.Data.Optional(key);

    public T Require<T>(string key) => Config.Data.Require<T>(key);
    public T Optional<T>(string key) => Config.Data.Optional<T>(key);
    public void Set(string key, object data) => Update(key, data);

    public void Update(string key, object data)
    {
        bool changed = data != null && !data.Equals(Value(key));
        Config.Data[key] = data;
        if (changed)
            Update(Config); // TODO: UpdateAsync fire and forget
    }
    public ServiceConfig Refresh() => _config = 
        Find(config => true).FirstOrDefault() 
        ?? Create(new ServiceConfig());

    public ConfigSingleton() : base("config")
    {
        Refresh();
        Instance = this;
    } 

    public class ServiceConfig : MinqDocument
    {
        [FlexKeys(json: "data", bson: "data")]
        public FlexJson Data { get; set; }
        internal ServiceConfig() => Data = new FlexJson();
    }
}