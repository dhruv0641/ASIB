namespace ASIB.Models.ViewModels;

public class ViewPostPageViewModel
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public ViewPostItem? Post { get; set; }
    public List<ViewPostCommentItem> Comments { get; set; } = new();
}

public class ViewPostItem
{
    public long PostId { get; set; }
    public long AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string AuthorProfilePhoto { get; set; } = "";
    public string CreatedAtText { get; set; } = "";
    public string ContentHtml { get; set; } = "";
    public string? PostType { get; set; }
    public string? PhotoUrl { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int SharesCount { get; set; }
    public bool IsFollowing { get; set; }
}

public class ViewPostCommentItem
{
    public string CommenterName { get; set; } = "";
    public string CommenterPhoto { get; set; } = "";
    public string Content { get; set; } = "";
}
