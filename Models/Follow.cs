using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Follow
{
    public long FollowId { get; set; }

    public long? FollowerId { get; set; }

    public long? FollowingId { get; set; }

    public DateTime FollowedAt { get; set; }
}
