namespace ASIB.Models.ViewModels;

public class MyNetworkSuggestionRow
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Headline { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Role { get; set; }
}
