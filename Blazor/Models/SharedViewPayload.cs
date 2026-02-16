using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;

namespace Maynard.Minq.Blazor.Models;

public class SharedViewPayload : FlexModel
{
    [FlexIgnore(ignore: Ignore.InBson)]
    public GlobalSettingsPayload Global { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public LocalSettingsPayload Local { get; set; }
}