using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Message
{
    public long MessageId { get; set; }

    public long? SenderId { get; set; }

    public long? ReceiverId { get; set; }

    public string Content { get; set; } = null!;

    public string? AttachmentPath { get; set; }

    public string? AttachmentType { get; set; }

    public DateTime SentAt { get; set; }

    public bool? Status { get; set; }

    public bool? Deleted { get; set; }
}
