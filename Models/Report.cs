using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Report
{
    public long ReportId { get; set; }

    public long PostId { get; set; }

    public long ReporterId { get; set; }

    public string? Reason { get; set; }

    public DateTime ReportedAt { get; set; }
}
