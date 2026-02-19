using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Share
{
    public long ShareId { get; set; }

    public long PostId { get; set; }

    public long UserId { get; set; }

    public DateTime SharedAt { get; set; }
}
