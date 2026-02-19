using ASIB.Core.Interfaces;
using ASIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Controllers;

[Route("Messages")]
public class MessagesController : Controller
{
    private const long MaxAttachmentSizeBytes = 25L * 1024L * 1024L;

    private readonly IMessageService _messageService;
    private readonly AsibContext _context;
    private readonly IWebHostEnvironment _environment;

    public MessagesController(IMessageService messageService, AsibContext context, IWebHostEnvironment environment)
    {
        _messageService = messageService;
        _context = context;
        _environment = environment;
    }

    [HttpGet("")]
    [HttpGet("Conversation/{userId:long}")]
    public async Task<IActionResult> Index(long? userId = null)
    {
        if (!TryGetUserId(out var currentUserId))
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var model = await _messageService.BuildMessagePageModelAsync(currentUserId);
        if (model == null)
            return Redirect("/Auth/Logout");

        var isVerified = await _messageService.IsUserVerifiedAsync(currentUserId);
        if (!isVerified)
        {
            const string script = "<script>alert('Your account is not yet verified/accepted.'); window.location.href='/Auth/Logout';</script>";
            return Content(script, "text/html");
        }

        ViewBag.SelectedConversationUserId = userId;
        return View("~/Views/Message/Index.cshtml", model);
    }

    [HttpGet("/Dashboard/Message")]
    [HttpGet("/message")]
    public IActionResult RedirectLegacyMessage()
    {
        return Redirect("/Messages");
    }

    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers([FromQuery(Name = "select_user_id")] long? selectUserId)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { status = "error", message = "User not logged in." });

        var users = await _context.Users
            .Where(u => u.UserId != currentUserId && u.VerificationStatus == 1)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.IsOnline,
                Role = _context.Roles.Where(r => r.RoleId == u.RoleId).Select(r => r.Role1).FirstOrDefault(),
                ProfilePhotoUrl = _context.Profiles.Where(p => p.UserId == u.UserId).Select(p => p.ProfilePhotoUrl).FirstOrDefault()
            })
            .ToListAsync();

        var filtered = new List<object>();
        foreach (var user in users)
        {
            var hasThread = await _context.Messages.AnyAsync(m =>
                (m.SenderId == user.UserId && m.ReceiverId == currentUserId) ||
                (m.SenderId == currentUserId && m.ReceiverId == user.UserId));

            if (!hasThread && (!selectUserId.HasValue || selectUserId.Value != user.UserId))
                continue;

            var unreadCount = await _context.Messages.CountAsync(m =>
                m.SenderId == user.UserId &&
                m.ReceiverId == currentUserId &&
                m.Status == false);

            var lastMessage = await _context.Messages
                .Where(m =>
                    (m.SenderId == user.UserId && m.ReceiverId == currentUserId) ||
                    (m.SenderId == currentUserId && m.ReceiverId == user.UserId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new
                {
                    m.Content,
                    m.SentAt,
                    m.AttachmentPath
                })
                .FirstOrDefaultAsync();

            var profilePhoto = string.IsNullOrWhiteSpace(user.ProfilePhotoUrl)
                ? $"https://placehold.co/50x50/6366f1/white?text={GetInitial(user.FirstName)}"
                : user.ProfilePhotoUrl;

            filtered.Add(new
            {
                user_id = user.UserId,
                first_name = user.FirstName,
                last_name = user.LastName,
                is_online = user.IsOnline ?? false,
                role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role,
                profile_photo_url = profilePhoto,
                unread_count = unreadCount,
                last_message = lastMessage?.Content,
                last_message_time = lastMessage?.SentAt,
                last_message_attachment = lastMessage?.AttachmentPath
            });
        }

        var ordered = filtered
            .OrderBy(x =>
            {
                var userId = (long)x.GetType().GetProperty("user_id")!.GetValue(x)!;
                return selectUserId.HasValue && userId == selectUserId.Value ? 0 : 1;
            })
            .ThenByDescending(x =>
            {
                var v = x.GetType().GetProperty("last_message_time")!.GetValue(x);
                return v as DateTime? ?? DateTime.MinValue;
            })
            .ToList();

        return Json(ordered);
    }

    [HttpPost("SearchUsers")]
    public async Task<IActionResult> SearchMessageUsers([FromForm(Name = "search_term")] string? searchTerm)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { status = "error", message = "User not logged in." });

        var term = (searchTerm ?? string.Empty).Trim().ToLowerInvariant();
        var likeEmpty = string.IsNullOrEmpty(term);

        var candidates = await _context.Users
            .Where(u => u.UserId != currentUserId && u.VerificationStatus == 1)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.IsOnline,
                Role = _context.Roles.Where(r => r.RoleId == u.RoleId).Select(r => r.Role1).FirstOrDefault(),
                ProfilePhotoUrl = _context.Profiles.Where(p => p.UserId == u.UserId).Select(p => p.ProfilePhotoUrl).FirstOrDefault()
            })
            .ToListAsync();

        var users = new List<object>();
        foreach (var user in candidates)
        {
            var fullName = $"{user.FirstName} {user.LastName}".ToLowerInvariant();
            if (!likeEmpty && !fullName.Contains(term) && !(user.FirstName ?? string.Empty).ToLowerInvariant().Contains(term) && !(user.LastName ?? string.Empty).ToLowerInvariant().Contains(term))
                continue;

            var unreadCount = await _context.Messages.CountAsync(m =>
                m.SenderId == user.UserId &&
                m.ReceiverId == currentUserId &&
                m.Status == false);

            var lastMessage = await _context.Messages
                .Where(m =>
                    (m.SenderId == user.UserId && m.ReceiverId == currentUserId) ||
                    (m.SenderId == currentUserId && m.ReceiverId == user.UserId))
                .OrderByDescending(m => m.SentAt)
                .Select(m => new { m.Content, m.SentAt })
                .FirstOrDefaultAsync();

            users.Add(new
            {
                user_id = user.UserId,
                first_name = user.FirstName,
                last_name = user.LastName,
                is_online = user.IsOnline ?? false,
                role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role,
                profile_photo_url = string.IsNullOrWhiteSpace(user.ProfilePhotoUrl)
                    ? $"https://placehold.co/50x50/6366f1/white?text={GetInitial(user.FirstName)}"
                    : user.ProfilePhotoUrl,
                unread_count = unreadCount,
                last_message = lastMessage?.Content,
                last_message_time = lastMessage?.SentAt
            });
        }

        return Json(users.OrderByDescending(x =>
        {
            var v = x.GetType().GetProperty("last_message_time")!.GetValue(x);
            return v as DateTime? ?? DateTime.MinValue;
        }));
    }

    [HttpPost("Conversation/{userId:long}")]
    public async Task<IActionResult> GetConversation(long userId)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { status = "error", message = "User not logged in." });

        if (userId <= 0)
            return Json(new { status = "error", message = "Invalid receiver ID." });

        var incomingUnread = await _context.Messages
            .Where(m => m.SenderId == userId && m.ReceiverId == currentUserId && m.Status == false)
            .ToListAsync();

        foreach (var message in incomingUnread)
            message.Status = true;

        if (incomingUnread.Count > 0)
            await _context.SaveChangesAsync();

        var messages = await _context.Messages
            .Where(m =>
                (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                (m.SenderId == userId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                message_id = m.MessageId,
                sender_id = m.SenderId,
                content = m.Content,
                sent_at = m.SentAt,
                status = m.Status,
                attachment_path = m.AttachmentPath,
                attachment_type = m.AttachmentType
            })
            .ToListAsync();

        return Json(messages);
    }

    [HttpPost("Send")]
    public async Task<IActionResult> SendMessage([FromForm(Name = "receiver_id")] long receiverId, [FromForm(Name = "message")] string? messageText, IFormFile? attachment)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { status = "error", message = "User not logged in." });

        var content = (messageText ?? string.Empty).Trim();
        if (receiverId <= 0)
            return Json(new { status = "error", message = "Invalid receiver ID." });

        if (string.IsNullOrEmpty(content) && (attachment == null || attachment.Length == 0))
            return Json(new { status = "error", message = "Cannot send an empty message." });

        string? attachmentPath = null;
        string? attachmentType = null;

        if (attachment != null && attachment.Length > 0)
        {
            if (attachment.Length > MaxAttachmentSizeBytes)
                return Json(new { status = "error", message = "File is too large (Max 25MB)." });

            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "attachments");
            Directory.CreateDirectory(uploadDirectory);

            var originalName = Path.GetFileNameWithoutExtension(attachment.FileName);
            var cleanName = string.Concat(originalName.Where(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-'));
            if (string.IsNullOrWhiteSpace(cleanName))
                cleanName = "file";

            var extension = Path.GetExtension(attachment.FileName);
            var uniqueName = $"file_{Guid.NewGuid():N}_{cleanName}{extension}";
            var fullPath = Path.Combine(uploadDirectory, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await attachment.CopyToAsync(stream);

            attachmentPath = $"/uploads/attachments/{uniqueName}";
            attachmentType = attachment.ContentType;
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Messages.Add(new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content,
                AttachmentPath = attachmentPath,
                AttachmentType = attachmentType,
                Status = false,
                SentAt = DateTime.Now
            });

            _context.Notifications.Add(new Notification
            {
                ReceiverId = receiverId,
                SenderId = currentUserId,
                Type = "message",
                EntityId = currentUserId,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Json(new { status = "success", message = "Message sent." });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Json(new { status = "error", message = ex.Message });
        }
    }

    [HttpGet("ShareRecipients")]
    public async Task<IActionResult> GetShareRecipients()
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { success = false, message = "Not logged in." });

        var senderIds = await _context.Messages
            .Where(m => m.ReceiverId == currentUserId && m.SenderId != null)
            .Select(m => m.SenderId!.Value)
            .Distinct()
            .ToListAsync();

        var receiverIds = await _context.Messages
            .Where(m => m.SenderId == currentUserId && m.ReceiverId != null)
            .Select(m => m.ReceiverId!.Value)
            .Distinct()
            .ToListAsync();

        var ids = senderIds
            .Concat(receiverIds)
            .Where(id => id != currentUserId)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return Json(new { success = true, users = Array.Empty<object>() });

        var users = await (
            from u in _context.Users
            join p in _context.Profiles on u.UserId equals p.UserId into up
            from p in up.DefaultIfEmpty()
            where ids.Contains(u.UserId)
            orderby u.FirstName, u.LastName
            select new
            {
                user_id = u.UserId,
                first_name = u.FirstName,
                last_name = u.LastName,
                profile_photo_url = p != null ? p.ProfilePhotoUrl : null,
                headline = p != null ? p.Headline : null
            }
        ).ToListAsync();

        var mapped = users.Select(u => new
        {
            u.user_id,
            u.first_name,
            u.last_name,
            profile_photo_url = string.IsNullOrWhiteSpace(u.profile_photo_url)
                ? $"https://placehold.co/50x50/6366f1/white?text={GetInitial(u.first_name)}"
                : u.profile_photo_url,
            headline = string.IsNullOrWhiteSpace(u.headline) ? "User" : u.headline
        });

        return Json(new { success = true, users = mapped });
    }

    private bool TryGetUserId(out long userId)
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        var role = (HttpContext.Session.GetString("role") ?? string.Empty).Trim().ToLowerInvariant();
        var userIdString = HttpContext.Session.GetString("user_id");

        if (!isLoggedIn || role != "user" || !long.TryParse(userIdString, out userId) || userId <= 0)
        {
            userId = 0;
            return false;
        }

        return true;
    }

    private bool IsAdminLoggedIn()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        return isLoggedIn && role == "admin";
    }

    private static string GetInitial(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return "U";
        return firstName.Trim().Substring(0, 1).ToUpperInvariant();
    }
}
