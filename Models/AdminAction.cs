using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class AdminAction
{
    public long AdminActionId { get; set; }

    public long AdminId { get; set; }

    public string ActionType { get; set; } = null!;

    public long? TargetUserId { get; set; }

    public long? TargetPostId { get; set; }

    public long? TargetEventId { get; set; }

    public DateTime ActionTime { get; set; }

    public string? Reason { get; set; }

    public string? Description { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
