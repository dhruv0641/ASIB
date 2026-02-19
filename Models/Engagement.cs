using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Engagement
{
    public long EngagementId { get; set; }

    public long? PostId { get; set; }

    public long? UserId { get; set; }

    /// <summary>
    /// e.g., &apos;like&apos;, &apos;comment&apos;
    /// </summary>
    public string EngagementType { get; set; } = null!;

    /// <summary>
    /// Used for comments
    /// </summary>
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }
}
