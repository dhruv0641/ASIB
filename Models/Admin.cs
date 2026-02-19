using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Admin
{
    public long AdminId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AdminAction> AdminActions { get; set; } = new List<AdminAction>();

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}
