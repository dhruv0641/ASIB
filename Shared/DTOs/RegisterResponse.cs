namespace ASIB.Shared.DTOs;

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Dictionary<string, string> Errors { get; set; } = new();
}
