using System;

namespace Maynard.Minq.Blazor.Enums;

[Flags]
public enum MinqViewerDeletionMode
{
    None                    = 0b0000_0000,
    SingleRecordAdminOnly   = 0b0000_0001,
    CollectionAdminOnly     = 0b0000_0010,
    SingleRecord            = 0b0000_0101,
    Collection              = 0b0000_0110
}