using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class PasswordReset
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string OtpHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public DateTime CreatedAt { get; set; }
}
