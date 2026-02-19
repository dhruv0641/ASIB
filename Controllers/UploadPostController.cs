using ASIB.Core.Interfaces;
using ASIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Controllers;

public class UploadPostController : Controller
{
    private readonly AsibContext _context;
    private readonly IUploadStorageService _uploadStorage;

    public UploadPostController(AsibContext context, IUploadStorageService uploadStorage)
    {
        _context = context;
        _uploadStorage = uploadStorage;
    }

    [HttpPost("/UploadPost/UploadPost")]
    public async Task<IActionResult> UploadPost()
    {
        if (HttpContext.Session.GetString("user_id") == null)
            return Json(new { success = false, message = "User not logged in." });

        if (!long.TryParse(HttpContext.Session.GetString("user_id"), out var userId))
            return Json(new { success = false, message = "User not logged in." });

        var content = (Request.Form["content"].ToString() ?? "").Trim();
        var mediaFile = Request.Form.Files.FirstOrDefault(f => f.Name == "media");

        if (string.IsNullOrEmpty(content) && (mediaFile == null || mediaFile.Length == 0))
            return Json(new { success = false, message = "Post cannot be empty." });

        var postType = "general";
        if (mediaFile != null && mediaFile.Length > 0)
        {
            var ext = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif")
                postType = "image";
            else if (ext == ".mp4" || ext == ".mov" || ext == ".avi")
                postType = "video";
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        string? savedPath = null;
        try
        {
            var post = new Post
            {
                UserId = userId,
                Content = content,
                PhotoUrl = null,
                PostType = postType,
                CreatedAt = DateTime.Now,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            if (mediaFile != null && mediaFile.Length > 0)
            {
                var save = await _uploadStorage.SavePostImageAsync(userId, post.PostId, mediaFile, 1);
                if (!save.Success || string.IsNullOrEmpty(save.RelativePath))
                    throw new Exception(save.Error ?? "Failed to save uploaded file.");

                savedPath = save.RelativePath;
                post.PhotoUrl = savedPath;
                await _context.SaveChangesAsync();
            }

            var followerIds = await _context.Follows
                .Where(f => f.FollowingId == userId && f.FollowerId != null)
                .Select(f => f.FollowerId!.Value)
                .ToListAsync();

            foreach (var followerId in followerIds)
            {
                var notif = new Notification
                {
                    ReceiverId = followerId,
                    SenderId = userId,
                    Type = "new_post",
                    EntityId = post.PostId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var postData = await _context.Posts
                .Where(p => p.PostId == post.PostId && p.UserId != null)
                .Join(_context.Users, p => p.UserId!.Value, u => u.UserId, (p, u) => new { p, u })
                .GroupJoin(_context.Profiles, pu => pu.u.UserId, pr => pr.UserId, (pu, pr) => new { pu.p, pu.u, pr })
                .SelectMany(x => x.pr.DefaultIfEmpty(), (x, pr) => new
                {
                    post_id = x.p.PostId,
                    user_id = x.p.UserId,
                    content = x.p.Content,
                    photo_url = x.p.PhotoUrl,
                    post_type = x.p.PostType,
                    created_at = x.p.CreatedAt,
                    likes_count = x.p.LikesCount,
                    comments_count = x.p.CommentsCount,
                    shares_count = x.p.SharesCount,
                    first_name = x.u.FirstName,
                    last_name = x.u.LastName,
                    profile_photo_url = pr != null ? pr.ProfilePhotoUrl : null
                })
                .FirstOrDefaultAsync();

            return Json(new { success = true, post = postData });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Json(new { success = false, message = ex.Message });
        }
    }
}

