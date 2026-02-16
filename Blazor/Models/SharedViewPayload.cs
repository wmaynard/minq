using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;

namespace Maynard.Minq.Blazor.Models;

internal class SharedViewPayload : FlexModel
{
    [FlexIgnore(ignore: Ignore.InBson)]
    internal GlobalSettingsPayload Global { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    internal LocalSettingsPayload Local { get; set; }
}