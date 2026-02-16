using System.Collections.Generic;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Minq.Blazor.Enums;

namespace Maynard.Minq.Blazor.Models;

internal class LocalSettingsPayload : FlexModel
{
    [FlexIgnore(ignore: Ignore.InBson)]
    internal int PinnedColumnWidth { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    internal int MaxColumnWidth { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    internal RowClickBehaviorOption RowClickBehavior { get; set; }
    [FlexIgnore(ignore: Ignore.InBson)]
    internal List<string> HiddenColumns { get; set; }
}