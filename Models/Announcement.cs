using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Announcement
{
    public long AnnouncementId { get; set; }

    public long AdminId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
