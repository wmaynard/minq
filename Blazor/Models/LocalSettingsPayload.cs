using System.Collections.Generic;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

internal class LocalSettingsPayload : FlexModel
{
    [FlexIgnore(ignore: Ignore.InBson)]
    public int PinnedColumnWidth { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public int MaxColumnWidth { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public RowClickBehaviorOption RowClickBehavior { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    public List<string> HiddenColumns { get; set; }
}