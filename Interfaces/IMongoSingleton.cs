using Maynard.Singletons;
using Rumble.Platform.Common.MinqOld;

namespace Rumble.Platform.Common.Interfaces;

public interface IMongoSingleton : ISingleton, IGdprHandler
{
    public bool IsHealthy { get; }
    public bool IsConnected { get; }
    public void InitializeCollection();
    public void CreateIndexes();
}

