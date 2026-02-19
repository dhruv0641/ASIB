using ASIB.Core.Interfaces;
using ASIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Controllers;

public class NotificationsController : Controller
{
    private readonly INotificationsService _notificationsService;
    private readonly AsibContext _context;

    public NotificationsController(INotificationsService notificationsService, AsibContext context)
    {
        _notificationsService = notificationsService;
        _context = context;
    }

    [HttpGet("/Dashboard/Notifications")]
    [HttpGet("notifications")]
    
    public async Task<IActionResult> Index()
    {
        if (!IsUserLoggedIn())
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return Redirect("/Auth/Login");

        var model = await _notificationsService.BuildNotificationsPageModelAsync(userId);
        if (model == null)
            return Redirect("/Auth/Logout");

        await _notificationsService.MarkAllAsReadAsync(userId);

        return View("Index", model);
    }

    private bool IsUserLoggedIn()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        return isLoggedIn && role == "user";
    }

    private bool IsAdminLoggedIn()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        return isLoggedIn && role == "admin";
    }

    [HttpGet("/Notifications/Count")]
    public async Task<IActionResult> Count()
    {
        if (!TryGetUserId(out var userId))
            return Json(new { unread_count = 0 });

        var unreadCount = await _context.Notifications
            .Where(n => n.ReceiverId == userId && n.IsRead == false)
            .CountAsync();

        return Json(new { unread_count = unreadCount });
    }

    [HttpGet("/Notifications/List")]
    public async Task<IActionResult> List()
    {
        if (!TryGetUserId(out var userId))
            return Json(new { notifications = Array.Empty<object>() });

        var notifications = await (
            from n in _context.Notifications
            join u in _context.Users on n.SenderId equals u.UserId
            join p in _context.Profiles on n.SenderId equals p.UserId into up
            from p in up.DefaultIfEmpty()
            where n.ReceiverId == userId
            orderby n.CreatedAt descending
            select new
            {
                notification_id = n.NotificationId,
                receiver_id = n.ReceiverId,
                sender_id = n.SenderId,
                type = n.Type,
                entity_id = n.EntityId,
                is_read = n.IsRead ? 1 : 0,
                created_at = n.CreatedAt,
                first_name = u.FirstName,
                last_name = u.LastName,
                profile_photo_url = p != null ? p.ProfilePhotoUrl : null
            }
        ).Take(10).ToListAsync();

        var mapped = notifications.Select(n => new
        {
            n.notification_id,
            n.receiver_id,
            n.sender_id,
            n.type,
            n.entity_id,
            n.is_read,
            n.created_at,
            n.first_name,
            n.last_name,
            profile_photo_url = string.IsNullOrWhiteSpace(n.profile_photo_url)
                ? $"https://placehold.co/48x48/6366f1/white?text={GetInitial(n.first_name)}"
                : n.profile_photo_url
        });

        return Json(new { notifications = mapped });
    }

    [HttpPost("/Notifications/MarkRead")]
    [HttpPost("/MarkNotificationsRead")]
    public async Task<IActionResult> MarkRead()
    {
        if (!TryGetUserId(out var userId))
            return Json(new { success = false, message = "Not logged in." });

        var unread = await _context.Notifications
            .Where(n => n.ReceiverId == userId && n.IsRead == false)
            .ToListAsync();

        foreach (var item in unread)
            item.IsRead = true;

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    private bool TryGetUserId(out long userId)
    {
        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out userId) || userId <= 0)
            return false;
        return true;
    }

    private static string GetInitial(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return "U";
        return firstName.Trim().Substring(0, 1).ToUpperInvariant();
    }
}

