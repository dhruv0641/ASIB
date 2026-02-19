using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class EventRequest
{
    public int RequestId { get; set; }

    public long EventId { get; set; }

    public long UserId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }
}
