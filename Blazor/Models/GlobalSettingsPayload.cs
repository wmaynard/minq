using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

internal class GlobalSettingsPayload : FlexModel
{
    [FlexIgnore(ignore: Ignore.InBson)]
    public int PageSize { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public int RefreshInterval { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public int TableFontSize { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    internal TimestampFormatOption TimestampFormat { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public bool FlattenJsonProperties { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public bool HideDefaultValues { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public string ThemeName { get; set; }
}