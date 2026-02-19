namespace ASIB.Shared.DTOs;

public class RegisterRequest
{
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string ContactNumber { get; set; } = null!;
    public long RoleRequested { get; set; }
    public int? BatchYear { get; set; }
    public string? EnrollmentNumber { get; set; }
    public string Password { get; set; } = null!;
    public string PasswordConfirm { get; set; } = null!;
}
