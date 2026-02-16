using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

public class GlobalSettingsPayload
{
    public int PageSize { get; set; }
    public int RefreshInterval { get; set; }
    public int TableFontSize { get; set; }
    internal TimestampFormatOption TimestampFormat { get; set; }
    public bool FlattenJsonProperties { get; set; }
    public bool HideDefaultValues { get; set; }
    public string ThemeName { get; set; }
}