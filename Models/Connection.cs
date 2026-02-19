using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Connection
{
    public long ConnectionId { get; set; }

    public long RequesterId { get; set; }

    public long AddresseeId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
