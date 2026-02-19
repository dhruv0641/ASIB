namespace ASIB.Models.ViewModels;

public class ViewPostRow
{
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public string? PostType { get; set; }
    public string CreatedAtText { get; set; } = "";
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int SharesCount { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? ProfilePhotoUrl { get; set; }
    public int IsFollowing { get; set; }
}
