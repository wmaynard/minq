using Maynard.Singletons;

namespace Maynard.Minq.Interfaces;

public interface IMongoSingleton : ISingleton, IGdprHandler
{
    public bool IsHealthy { get; }
    public bool IsConnected { get; }
    public void InitializeCollection();
    public void CreateIndexes();
}

