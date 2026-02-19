using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Post
{
    public long PostId { get; set; }

    public long? UserId { get; set; }

    public string Content { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string? PostType { get; set; }

    public DateTime CreatedAt { get; set; }

    public int LikesCount { get; set; }

    public int CommentsCount { get; set; }

    public int SharesCount { get; set; }
}
