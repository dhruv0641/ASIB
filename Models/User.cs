using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class User
{
    public long UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public long? RoleId { get; set; }

    public int? VerificationStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public long ContactNumber { get; set; }

    public string Address { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public long? RoleRequested { get; set; }

    public int? BatchYear { get; set; }

    public string? EnrollmentNumber { get; set; }

    public DateTime? LastSeen { get; set; }

    public bool? IsOnline { get; set; }
}
