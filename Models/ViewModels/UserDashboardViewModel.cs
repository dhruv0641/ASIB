using System;
using System.Collections.Generic;

namespace ASIB.Models.ViewModels;

public class UserDashboardViewModel
{
    public long UserId { get; set; }
    public string UserRole { get; set; } = "";
    public string UserName { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public string CurrentPage { get; set; } = "UserDashboard";
    public List<UserDashboardEventItem> UpcomingEvents { get; set; } = new();
    public List<UserDashboardAnnouncementItem> Announcements { get; set; } = new();
    public List<UserDashboardPostItem> FeedPosts { get; set; } = new();
}

public class UserDashboardEventItem
{
    public long EventId { get; set; }
    public string Title { get; set; } = "";
    public string StartTimeText { get; set; } = "";
}

public class UserDashboardAnnouncementItem
{
    public long AnnouncementId { get; set; }
    public string Title { get; set; } = "";
    public string ContentBase64 { get; set; } = "";
    public string CreatedAtText { get; set; } = "";
}

public class UserDashboardPostItem
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
    public List<UserDashboardCommentItem> Comments { get; set; } = new();
}

public class UserDashboardCommentItem
{
    public long PostId { get; set; }
    public string CommenterName { get; set; } = "";
    public string CommenterPhoto { get; set; } = "";
    public string Content { get; set; } = "";
}

public class EventPageViewModel
{
    public bool IsFaculty { get; set; }
    public bool IsScheduleView { get; set; }
    public string PageTitle { get; set; } = "";
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public string CurrentPage { get; set; } = "Event";
    public List<FacultyEventItem> FacultyEvents { get; set; } = new();
    public List<ScheduleEventItem> ScheduleEvents { get; set; } = new();
    public bool FoundEvents { get; set; }
}

public class FacultyEventItem
{
    public long EventId { get; set; }
    public string Title { get; set; } = "";
    public string StartDateText { get; set; } = "";
    public int ApprovedCount { get; set; }
    public List<FacultyPendingRequestItem> PendingRequests { get; set; } = new();
}

public class FacultyPendingRequestItem
{
    public int RequestId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class ScheduleEventItem
{
    public long EventId { get; set; }
    public string Title { get; set; } = "";
    public string DescriptionHtml { get; set; } = "";
    public string StartTimeText { get; set; } = "";
    public string OrganizerName { get; set; } = "";
    public string? RequestStatus { get; set; }
    public string RequestStatusLabel { get; set; } = "";
    public bool IsStartingSoon { get; set; }
    public int MinutesUntilStart { get; set; }
    public string? MeetingUrl { get; set; }
}

public class MessagePageViewModel
{
    public long LoggedInUserId { get; set; }
    public string UserName { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public string CurrentPage { get; set; } = "Message";
}

public class MyNetworkPageViewModel
{
    public long CurrentUserId { get; set; }
    public string UserName { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public int ConnectionsCount { get; set; }
    public int FollowingCount { get; set; }
    public int FollowersCount { get; set; }
    public int TotalFollows { get; set; }
    public int EventsCount { get; set; }
    public int InvitationCount { get; set; }
    public string CurrentPage { get; set; } = "MyNetwork";
    public List<MyNetworkSuggestionItem> Suggestions { get; set; } = new();
}

public class MyNetworkSuggestionItem
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Headline { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Role { get; set; }
    public string DisplayPhoto { get; set; } = "";
    public string DisplayHeadline { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class NotificationsPageViewModel
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string ProfileHeadline { get; set; } = "";
    public string CurrentPage { get; set; } = "Notifications";
    public List<NotificationListItem> Notifications { get; set; } = new();
}

public class NotificationListItem
{
    public string Link { get; set; } = "#";
    public string TextHtml { get; set; } = "";
    public string ProfilePhoto { get; set; } = "";
    public string TimeText { get; set; } = "";
}

