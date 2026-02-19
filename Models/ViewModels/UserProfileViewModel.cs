namespace ASIB.Models.ViewModels;

public class UserProfileViewModel
{
    public long UserId { get; set; }
    public string FlashMessage { get; set; } = "";
    public string FlashMessagePersonal { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string Role { get; set; } = "";

    public string ProfilePhoto { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Skills { get; set; } = "";
    public string Experience { get; set; } = "";
}
