using Rumble.Platform.Common.Models;

namespace Rumble.Platform.Common.Minq;

public interface IGdprHandler
{
    public long ProcessGdprRequest(string accountId, string dummyText);
}