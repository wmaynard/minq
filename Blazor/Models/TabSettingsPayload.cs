using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;

namespace Maynard.Minq.Blazor.Models;

internal class TabSettingsPayload : FlexModel
{
    [FlexIgnore(Ignore.InBson)]
    public string ThemeName { get; set; }
}