using Maynard.Json;

namespace Rumble.Platform.Common.Interfaces;

public interface IPlatformService
{
    public string Name { get; }
    public FlexJson HealthStatus { get; }
}