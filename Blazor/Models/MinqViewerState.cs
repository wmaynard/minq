using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

public class MinqViewerState
{
    public int PageSize { get; set; } = 25;
    public int RefreshInterval { get; set; } = 30;
    public int TableFontSize { get; set; } = 14;
    public int PinnedColumnWidth { get; set; } = 200;
    public int MaxColumnWidth { get; set; } = 400;
    public MinqViewerTimestampFormatOption MinqViewerTimestampFormat { get; set; } = MinqViewerTimestampFormatOption.Local;
    public bool FlattenJsonProperties { get; set; }
    public bool HideDefaultValues { get; set; }
    internal RowClickBehaviorOption RowClickBehavior { get; set; } = RowClickBehaviorOption.SelectText;
    public string SelectedThemeName { get; set; } = "Dark Mode";
}