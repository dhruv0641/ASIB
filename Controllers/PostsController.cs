using System.Text.Json;
using ASIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Controllers;

[Route("Posts")]
public class PostsController : Controller
{
    private readonly AsibContext _context;

    public PostsController(AsibContext context)
    {
        _context = context;
    }

    [HttpPost("Share/{postId:long}")]
    public async Task<IActionResult> Share(long postId, [FromForm(Name = "receiver_ids")] string? receiverIdsJson, [FromForm(Name = "message")] string? customMessage)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { success = false, message = "User not logged in." });

        if (postId <= 0)
            return Json(new { success = false, message = "Missing required data." });

        List<long>? receiverIds;
        try
        {
            receiverIds = JsonSerializer.Deserialize<List<long>>(receiverIdsJson ?? string.Empty);
        }
        catch
        {
            receiverIds = null;
        }

        if (receiverIds == null || receiverIds.Count == 0)
            return Json(new { success = false, message = "Invalid receivers." });

        var post = await (
            from p in _context.Posts
            join u in _context.Users on p.UserId equals u.UserId
            where p.PostId == postId
            select new
            {
                p.PostId,
                p.Content,
                p.PhotoUrl,
                p.PostType,
                Author = (u.FirstName ?? "") + " " + (u.LastName ?? "")
            }
        ).FirstOrDefaultAsync();

        if (post == null)
            return Json(new { success = false, message = "Post not found." });

        var snippet = (post.Content ?? string.Empty).Trim();
        if (snippet.Length > 100)
            snippet = snippet[..100] + "...";

        var payload = new
        {
            id = post.PostId,
            author = post.Author.Trim(),
            content_snippet = snippet,
            image_url = (post.PostType == "image" && !string.IsNullOrWhiteSpace(post.PhotoUrl)) ? post.PhotoUrl : null,
            post_url = $"{Request.Scheme}://{Request.Host}/Dashboard/ViewPost?id={post.PostId}"
        };

        var messageBody = string.IsNullOrWhiteSpace(customMessage)
            ? $"SHARED_POST::{JsonSerializer.Serialize(payload)}"
            : $"{customMessage.Trim()}\n\nSHARED_POST::{JsonSerializer.Serialize(payload)}";

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var receiverId in receiverIds.Where(id => id > 0 && id != currentUserId).Distinct())
            {
                var isValidReceiver = await _context.Users.AnyAsync(u => u.UserId == receiverId && u.VerificationStatus == 1);
                if (!isValidReceiver)
                    continue;

                _context.Messages.Add(new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = messageBody,
                    Status = false,
                    SentAt = DateTime.Now
                });

                var hasDuplicateUnread = await _context.Notifications.AnyAsync(n =>
                    n.ReceiverId == receiverId &&
                    n.SenderId == currentUserId &&
                    n.Type == "message" &&
                    n.EntityId == currentUserId &&
                    n.IsRead == false);

                if (!hasDuplicateUnread)
                {
                    _context.Notifications.Add(new Notification
                    {
                        ReceiverId = receiverId,
                        SenderId = currentUserId,
                        Type = "message",
                        EntityId = currentUserId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return Json(new { success = true, message = "Post sent successfully!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Action")]
    [HttpPost("/PostAction")]
    public async Task<IActionResult> Action([FromForm(Name = "post_id")] long postId, [FromForm] string action, [FromForm] string? content)
    {
        if (!TryGetUserId(out var currentUserId))
            return Json(new { success = false, message = "User not logged in." });

        if (postId <= 0 || string.IsNullOrWhiteSpace(action))
            return Json(new { success = false, message = "Invalid request." });

        var normalizedAction = action.Trim().ToLowerInvariant();
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
        if (post == null)
            return Json(new { success = false, message = "Post not found." });

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var liked = false;

            if (normalizedAction == "like")
            {
                var existingLike = await _context.Engagements.FirstOrDefaultAsync(e =>
                    e.PostId == postId &&
                    e.UserId == currentUserId &&
                    e.EngagementType == "like");

                if (existingLike != null)
                {
                    _context.Engagements.Remove(existingLike);
                    post.LikesCount = Math.Max(0, post.LikesCount - 1);
                }
                else
                {
                    _context.Engagements.Add(new Engagement
                    {
                        PostId = postId,
                        UserId = currentUserId,
                        EngagementType = "like",
                        CreatedAt = DateTime.Now
                    });
                    post.LikesCount += 1;
                    liked = true;
                    await CreateNotificationAsync(post.UserId, currentUserId, "like", postId);
                }
            }
            else if (normalizedAction == "comment")
            {
                var commentText = (content ?? string.Empty).Trim();
                if (commentText.Length == 0)
                    return Json(new { success = false, message = "Comment cannot be empty." });

                _context.Engagements.Add(new Engagement
                {
                    PostId = postId,
                    UserId = currentUserId,
                    EngagementType = "comment",
                    Content = commentText,
                    CreatedAt = DateTime.Now
                });
                post.CommentsCount += 1;
                await CreateNotificationAsync(post.UserId, currentUserId, "comment", postId);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return Redirect("/Dashboard/UserDashboard");
            }
            else if (normalizedAction == "share")
            {
                var existingShare = await _context.Shares.FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == currentUserId);
                if (existingShare == null)
                {
                    _context.Shares.Add(new Share
                    {
                        PostId = postId,
                        UserId = currentUserId,
                        SharedAt = DateTime.Now
                    });
                    post.SharesCount += 1;
                }
            }
            else
            {
                return Json(new { success = false, message = "Unsupported action." });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Json(new
            {
                success = true,
                liked,
                counts = new
                {
                    likes_count = post.LikesCount,
                    comments_count = post.CommentsCount,
                    shares_count = post.SharesCount
                }
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task CreateNotificationAsync(long? receiverId, long senderId, string type, long entityId)
    {
        if (!receiverId.HasValue || receiverId.Value == senderId)
            return;

        var hasDuplicateUnread = await _context.Notifications.AnyAsync(n =>
            n.ReceiverId == receiverId.Value &&
            n.SenderId == senderId &&
            n.Type == type &&
            n.EntityId == entityId &&
            n.IsRead == false);

        if (!hasDuplicateUnread)
        {
            _context.Notifications.Add(new Notification
            {
                ReceiverId = receiverId.Value,
                SenderId = senderId,
                Type = type,
                EntityId = entityId,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }
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
}
