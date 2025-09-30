using Rumble.Platform.Common.Minq;

namespace Rumble.Platform.Common.Interfaces;

public interface IPlatformMongoService : IPlatformService, IGdprHandler
{
    public bool IsHealthy { get; }
    public bool IsConnected { get; }
    public void InitializeCollection();
    public void CreateIndexes();
}

