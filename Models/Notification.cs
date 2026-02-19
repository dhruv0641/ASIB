using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Notification
{
    public long NotificationId { get; set; }

    public long ReceiverId { get; set; }

    public long SenderId { get; set; }

    public string Type { get; set; } = null!;

    /// <summary>
    /// ID of the related item (post_id, sender_id, etc.)
    /// </summary>
    public long EntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
