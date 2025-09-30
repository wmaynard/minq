using Maynard.Json;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.Interfaces;

public interface IPlatformService
{
    public string Name { get; }
    public FlexJson HealthStatus { get; }
}