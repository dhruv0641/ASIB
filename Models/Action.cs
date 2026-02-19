using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Action
{
    public long ActionId { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string? Description { get; set; }
}
