using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Event
{
    public long EventId { get; set; }

    public long? UserId { get; set; }

    public long? CreatedBy { get; set; }

    public long? RoleId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? MeetingUrl { get; set; }
}
