using ASIB.Core.Interfaces;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace ASIB.Core.Services;

public class NotificationsService : INotificationsService
{
    private readonly ASIB.Models.AsibContext _context;

    public NotificationsService(ASIB.Models.AsibContext context)
    {
        _context = context;
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE notifications SET is_read = 1 WHERE receiver_id = {0} AND is_read = 0",
            userId
        );
    }

    public async Task<NotificationsPageViewModel?> BuildNotificationsPageModelAsync(long userId)
    {
        var userData = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where u.UserId == userId
            select new
            {
                u.FirstName,
                u.LastName,
                Role = r != null ? r.Role1 : ""
            }
        ).FirstOrDefaultAsync();

        if (userData == null)
            return null;

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var firstLetter = !string.IsNullOrWhiteSpace(userData.FirstName)
            ? userData.FirstName.Trim().Substring(0, 1).ToUpperInvariant()
            : "U";
        var profilePhoto = !string.IsNullOrWhiteSpace(profile?.ProfilePhotoUrl)
            ? profile!.ProfilePhotoUrl!
            : $"https://placehold.co/100x100/6366f1/white?text={firstLetter}";
        var profileHeadline = !string.IsNullOrWhiteSpace(profile?.Headline)
            ? profile!.Headline!
            : CapitalizeFirstLetter(userData.Role);

        var notifications = await (
            from n in _context.Notifications
            join u in _context.Users on n.SenderId equals u.UserId
            join p in _context.Profiles on n.SenderId equals p.UserId into up
            from p in up.DefaultIfEmpty()
            where n.ReceiverId == userId
            orderby n.CreatedAt descending
            select new
            {
                n.SenderId,
                n.EntityId,
                n.Type,
                n.CreatedAt,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePhotoUrl = p != null ? p.ProfilePhotoUrl : null
            }
        ).ToListAsync();

        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, ist);

        var list = new List<NotificationListItem>();
        foreach (var n in notifications)
        {
            var display = GetNotificationDisplay(n.FirstName, n.LastName, n.SenderId, n.EntityId, n.Type);
            var profilePic = !string.IsNullOrWhiteSpace(n.ProfilePhotoUrl)
                ? n.ProfilePhotoUrl!
                : $"https://placehold.co/56x56/6366f1/white?text={GetInitial(n.FirstName)}";

            var timeText = GetTimeAgoText(n.CreatedAt, now);

            list.Add(new NotificationListItem
            {
                Link = display.link,
                TextHtml = display.text,
                ProfilePhoto = profilePic,
                TimeText = timeText
            });
        }

        return new NotificationsPageViewModel
        {
            UserId = userId,
            UserName = $"{userData.FirstName} {userData.LastName}",
            ProfilePhoto = profilePhoto,
            ProfileHeadline = profileHeadline,
            CurrentPage = "Notifications",
            Notifications = list
        };
    }

    private static (string text, string link) GetNotificationDisplay(string? firstName, string? lastName, long senderId, long entityId, string? type)
    {
        var senderName = WebUtility.HtmlEncode($"{firstName} {lastName}");
        var text = "";
        var link = "#";

        switch (type)
        {
            case "like":
                text = $"<strong>{senderName}</strong> liked your post.";
                link = $"/Dashboard/ViewPost?id={entityId}";
                break;
            case "comment":
                text = $"<strong>{senderName}</strong> commented on your post.";
                link = $"/Dashboard/ViewPost?id={entityId}";
                break;
            case "follow":
                text = $"<strong>{senderName}</strong> started following you.";
                link = $"/Dashboard/UserProfile?id={senderId}";
                break;
            case "connect_request":
                text = $"<strong>{senderName}</strong> sent you a connection request.";
                link = "/Dashboard/MyNetwork";
                break;
            case "connect_accept":
                text = $"<strong>{senderName}</strong> accepted your connection request.";
                link = $"/Dashboard/UserProfile?id={senderId}";
                break;
            case "new_post":
                text = $"<strong>{senderName}</strong> published a new post.";
                link = $"/Dashboard/ViewPost?id={entityId}";
                break;
            case "message":
                text = $"<strong>{senderName}</strong> sent you a new message.";
                link = $"/Messages/Conversation/{senderId}";
                break;
        }

        return (text, link);
    }

    private static string GetTimeAgoText(DateTime createdAt, DateTime now)
    {
        var diff = now - createdAt;

        if (diff.TotalDays >= 365)
            return $"{(int)(diff.TotalDays / 365)}y ago";
        if (diff.TotalDays >= 30)
            return $"{(int)(diff.TotalDays / 30)}mo ago";
        if (diff.TotalDays >= 1)
            return $"{(int)diff.TotalDays}d ago";
        if (diff.TotalHours >= 1)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalMinutes >= 1)
            return $"{(int)diff.TotalMinutes}m ago";
        return "Just now";
    }

    private static string GetInitial(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "U";
        return name.Trim().Substring(0, 1).ToUpperInvariant();
    }

    private static string CapitalizeFirstLetter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        if (trimmed.Length == 1)
            return trimmed.ToUpperInvariant();

        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }
}

