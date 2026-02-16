using System.Collections.Generic;
using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

public class LocalSettingsPayload
{
    public int PinnedColumnWidth { get; set; }
    public int MaxColumnWidth { get; set; }
    public RowClickBehaviorOption RowClickBehavior { get; set; }
    public List<string> HiddenColumns { get; set; }
}