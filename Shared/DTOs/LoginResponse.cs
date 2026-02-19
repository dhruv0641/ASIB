namespace ASIB.Shared.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string ErrorType { get; set; } = "error"; // "error", "pending", "blocked", "rejected"
    public string? RedirectUrl { get; set; }
}
