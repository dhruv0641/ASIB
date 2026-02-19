using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Embedding
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; } = null!;

    public string Embedding1 { get; set; } = null!;
}
