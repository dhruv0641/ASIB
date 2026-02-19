using ASIB.Core.Interfaces;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Encodings.Web;

namespace ASIB.Core.Services;

public class ViewPostService : IViewPostService
{
    private readonly ASIB.Models.AsibContext _context;

    public ViewPostService(ASIB.Models.AsibContext context)
    {
        _context = context;
    }

    public async Task<ViewPostPageViewModel?> BuildViewPostPageModelAsync(long userId, int postId)
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

        var postSql = @"
            SELECT p.post_id AS PostId, p.user_id AS UserId, p.content AS Content, p.photo_url AS PhotoUrl, 
                   p.post_type AS PostType, DATE_FORMAT(p.created_at, '%Y-%m-%d %H:%i:%s') AS CreatedAtText,
                   p.likes_count AS LikesCount, p.comments_count AS CommentsCount, p.shares_count AS SharesCount,
                   u.first_name AS FirstName, u.last_name AS LastName, pr.profile_photo_url AS ProfilePhotoUrl,
                   (f.follow_id IS NOT NULL) AS IsFollowing
            FROM posts p
            JOIN users u ON p.user_id = u.user_id
            LEFT JOIN profiles pr ON u.user_id = pr.user_id
            LEFT JOIN follows f ON f.follower_id = {0} AND f.following_id = p.user_id
            WHERE p.post_id = {1}
            LIMIT 1";

        var postRow = await _context.ViewPostRows
            .FromSqlRaw(postSql, userId, postId)
            .FirstOrDefaultAsync();

        if (postRow == null)
            return new ViewPostPageViewModel
            {
                UserId = userId,
                UserName = $"{userData.FirstName} {userData.LastName}",
                ProfilePhoto = profilePhoto,
                ProfileHeadline = profileHeadline
            };

        var authorName = $"{postRow.FirstName} {postRow.LastName}";
        var authorProfilePhoto = !string.IsNullOrWhiteSpace(postRow.ProfilePhotoUrl)
            ? postRow.ProfilePhotoUrl!
            : "https://placehold.co/50x50";

        var contentHtml = HtmlEncoder.Default.Encode(postRow.Content).Replace("\n", "<br>");

        var comments = await (
            from e in _context.Engagements
            join u in _context.Users on e.UserId equals u.UserId
            join pr in _context.Profiles on u.UserId equals pr.UserId into upr
            from pr in upr.DefaultIfEmpty()
            where e.PostId == postRow.PostId && e.EngagementType == "comment"
            orderby e.CreatedAt
            select new
            {
                u.FirstName,
                u.LastName,
                ProfilePhotoUrl = pr != null ? pr.ProfilePhotoUrl : null,
                e.Content
            }
        ).ToListAsync();

        var commentItems = new List<ViewPostCommentItem>();
        foreach (var c in comments)
        {
            var commenterName = $"{c.FirstName} {c.LastName}";
            var commenterImg = !string.IsNullOrWhiteSpace(c.ProfilePhotoUrl)
                ? c.ProfilePhotoUrl!
                : "https://placehold.co/30x30";

            commentItems.Add(new ViewPostCommentItem
            {
                CommenterName = commenterName,
                CommenterPhoto = commenterImg,
                Content = WebUtility.HtmlEncode(c.Content ?? "")
            });
        }

        return new ViewPostPageViewModel
        {
            UserId = userId,
            UserName = $"{userData.FirstName} {userData.LastName}",
            ProfilePhoto = profilePhoto,
            ProfileHeadline = profileHeadline,
            Post = new ViewPostItem
            {
                PostId = postRow.PostId,
                AuthorId = postRow.UserId,
                AuthorName = authorName,
                AuthorProfilePhoto = authorProfilePhoto,
                CreatedAtText = postRow.CreatedAtText,
                ContentHtml = contentHtml,
                PostType = postRow.PostType,
                PhotoUrl = postRow.PhotoUrl,
                LikesCount = postRow.LikesCount,
                CommentsCount = postRow.CommentsCount,
                SharesCount = postRow.SharesCount,
                IsFollowing = postRow.IsFollowing == 1
            },
            Comments = commentItems
        };
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
