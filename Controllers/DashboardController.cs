using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Encodings.Web;

namespace ASIB.Controllers;

public class DashboardController : Controller
{
    private readonly IAuthService _authService;
    private readonly AsibContext _context;
    private readonly IAdminDashboardService _adminDashboardService;
    private readonly IBulkInsertService _bulkInsertService;

    public DashboardController(IAuthService authService, AsibContext context, IAdminDashboardService adminDashboardService, IBulkInsertService bulkInsertService)
    {
        _authService = authService;
        _context = context;
        _adminDashboardService = adminDashboardService;
        _bulkInsertService = bulkInsertService;
    }

    public async Task<IActionResult> AdminDashboard()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase)
            || HttpContext.Session.GetString("admin_id") != null
            || HttpContext.Session.GetString("user_id") != null;
        if (!isLoggedIn || HttpContext.Session.GetString("admin_id") == null)
            return RedirectToAction("Login", "Auth");

        if (HttpContext.Session.GetString("user_id") != null)
            return RedirectToAction("UserDashboard");

        var page = (Request.Query["page"].ToString() ?? "").Trim().ToLowerInvariant();
        AdminDashboardViewModel model;
        if (string.IsNullOrEmpty(page) || page == "dashboard")
        {
            model = await _adminDashboardService.BuildDashboardAsync();
        }
        else if (page == "verification")
        {
            model = await _adminDashboardService.BuildVerificationAsync();
        }
        else if (page == "suspend")
        {
            model = await _adminDashboardService.BuildSuspendAsync();
        }
        else if (page == "all_users")
        {
            var view = (Request.Query["view"].ToString() ?? "").Trim().ToLowerInvariant();
            var idStr = (Request.Query["id"].ToString() ?? "").Trim();
            if (view == "detail" && long.TryParse(idStr, out var detailId) && detailId > 0)
            {
                var detailModel = await _adminDashboardService.BuildAllUsersDetailAsync(detailId);
                if (detailModel == null)
                    return RedirectToAction("AdminDashboard", new { page = "all_users" });

                model = detailModel;
            }
            else
            {
                var filter = (Request.Query["filter"].ToString() ?? "").Trim().ToLowerInvariant();
                model = await _adminDashboardService.BuildAllUsersListAsync(filter);
            }
        }
        else if (page == "events")
        {
            var view = (Request.Query["view"].ToString() ?? "").Trim().ToLowerInvariant();
            var idStr = (Request.Query["id"].ToString() ?? "").Trim();
            if (view == "detail" && long.TryParse(idStr, out var eventId) && eventId > 0)
            {
                var detailModel = await _adminDashboardService.BuildEventDetailsAsync(eventId);
                if (detailModel == null)
                    return RedirectToAction("AdminDashboard", new { page = "events" });

                model = detailModel;
            }
            else
            {
                model = await _adminDashboardService.BuildEventsListAsync();
            }
        }
        else if (page == "announcements")
        {
            var view = (Request.Query["view"].ToString() ?? "").Trim().ToLowerInvariant();
            var idStr = (Request.Query["id"].ToString() ?? "").Trim();
            if (view == "add")
            {
                model = (await _adminDashboardService.BuildAnnouncementEditAsync(0, true))!;
            }
            else if (view == "edit" && long.TryParse(idStr, out var announcementId) && announcementId > 0)
            {
                var editModel = await _adminDashboardService.BuildAnnouncementEditAsync(announcementId, false);
                if (editModel == null)
                    return RedirectToAction("AdminDashboard", new { page = "announcements" });

                model = editModel;
            }
            else
            {
                model = await _adminDashboardService.BuildAnnouncementsListAsync();
            }
        }
        else if (page == "admin_log")
        {
            var startDate = Request.Query["start_date"].ToString();
            var endDate = Request.Query["end_date"].ToString();
            model = await _adminDashboardService.BuildAdminLogAsync(startDate, endDate);
        }
        else if (page == "promotion")
        {
            model = await _adminDashboardService.BuildPromotionAsync();
        }
        else if (page == "add_user")
        {
            model = await _adminDashboardService.BuildAddUserAsync();
        }
        else if (page == "bulk_insert")
        {
            model = new AdminDashboardViewModel
            {
                Page = "bulk_insert",
                PageTitle = "Bulk User Insert"
            };
        }
        else
        {
            model = await _adminDashboardService.BuildDashboardAsync();
            model.Page = page;
            model.PageTitle = "Admin Panel";
        }
        model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
        return View("AdminDashboard", model);
    }

    [HttpPost]
    [ActionName("AdminDashboard")]
    public async Task<IActionResult> AdminDashboardPost()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase)
            || HttpContext.Session.GetString("admin_id") != null
            || HttpContext.Session.GetString("user_id") != null;
        if (!isLoggedIn || HttpContext.Session.GetString("admin_id") == null)
            return RedirectToAction("Login", "Auth");

        if (HttpContext.Session.GetString("user_id") != null)
            return RedirectToAction("UserDashboard");

        var page = (Request.Query["page"].ToString() ?? "").Trim().ToLowerInvariant();
        if (page != "suspend" && page != "events" && page != "announcements" && page != "promotion" && page != "add_user" && page != "bulk_insert")
            return RedirectToAction("AdminDashboard", new { page });

        var adminIdStr = HttpContext.Session.GetString("admin_id");
        var adminId = 0L;
        long.TryParse(adminIdStr, out adminId);

        var userIdStr = Request.Form["user_id"].ToString();
        var action = Request.Form["action"].ToString();
        var reason = Request.Form["reason"].ToString();

        if (page == "suspend")
        {
            string flash;
            string flashType;

            if (!long.TryParse(userIdStr, out var userId) || userId <= 0)
            {
                flash = "Invalid User ID provided.";
                flashType = "error";
            }
            else if (action == "block")
            {
                (flash, flashType) = await _adminDashboardService.BlockUserAsync(adminId, userId, reason);
            }
            else if (action == "unblock")
            {
                (flash, flashType) = await _adminDashboardService.UnblockUserAsync(adminId, userId);
            }
            else
            {
                flash = "Invalid User ID provided.";
                flashType = "error";
            }

            var model = await _adminDashboardService.BuildSuspendAsync();
            model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
            model.Flash = flash;
            model.FlashType = flashType;
            return View("AdminDashboard", model);
        }

        if (page == "events")
        {
            string flash;
            string flashType;
            var eventIdStr = Request.Form["event_id"].ToString();
            if (action == "delete_event" && long.TryParse(eventIdStr, out var eventId) && eventId > 0)
            {
                (flash, flashType) = await _adminDashboardService.DeleteEventAsync(adminId, eventId);
            }
            else
            {
                flash = "Invalid User ID provided.";
                flashType = "error";
            }

            var model = await _adminDashboardService.BuildEventsListAsync();
            model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
            model.Flash = flash;
            model.FlashType = flashType;
            return View("AdminDashboard", model);
        }

        if (page == "announcements")
        {
            var announcementIdStr = Request.Form["announcement_id"].ToString();
            var title = Request.Form["title"].ToString();
            var content = Request.Form["content"].ToString();
            var isActive = !string.IsNullOrEmpty(Request.Form["is_active"]);

            string flash;
            string flashType;

            if (action == "add")
            {
                (flash, flashType) = await _adminDashboardService.AddAnnouncementAsync(adminId, title, content, isActive);
            }
            else if (action == "edit" && long.TryParse(announcementIdStr, out var announcementId) && announcementId > 0)
            {
                (flash, flashType) = await _adminDashboardService.EditAnnouncementAsync(adminId, announcementId, title, content, isActive);
            }
            else if (action == "delete" && long.TryParse(announcementIdStr, out var deleteId) && deleteId > 0)
            {
                (flash, flashType) = await _adminDashboardService.DeleteAnnouncementAsync(adminId, deleteId);
            }
            else
            {
                flash = "Title and Content are required.";
                flashType = "error";
            }

            var model = await _adminDashboardService.BuildAnnouncementsListAsync();
            model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
            model.Flash = flash;
            model.FlashType = flashType;
            return View("AdminDashboard", model);
        }

        if (page == "promotion")
        {
            string flash;
            string flashType = "info";

            if (action == "promote_single")
            {
                var promoteUserIdStr = Request.Form["user_id"].ToString();
                if (long.TryParse(promoteUserIdStr, out var userId))
                    (flash, flashType) = await _adminDashboardService.PromoteSingleAsync(adminId, userId);
                else
                {
                    flash = "No users selected for promotion.";
                    flashType = "error";
                }
            }
            else if (action == "promote_bulk")
            {
                var ids = Request.Form["selected_users"].ToArray().Select(x => long.TryParse(x, out var id) ? id : 0).Where(x => x > 0);
                (flash, flashType) = await _adminDashboardService.PromoteBulkAsync(adminId, ids);
            }
            else if (action == "demote")
            {
                var demoteUserIdStr = Request.Form["user_id"].ToString();
                if (long.TryParse(demoteUserIdStr, out var userId))
                    (flash, flashType) = await _adminDashboardService.DemoteSingleAsync(adminId, userId);
                else
                {
                    flash = "No users selected for demotion.";
                    flashType = "error";
                }
            }
            else if (action == "demote_bulk")
            {
                var ids = Request.Form["selected_users"].ToArray().Select(x => long.TryParse(x, out var id) ? id : 0).Where(x => x > 0);
                (flash, flashType) = await _adminDashboardService.DemoteBulkAsync(adminId, ids);
            }
            else
            {
                flash = "No users selected for promotion.";
                flashType = "error";
            }

            var model = await _adminDashboardService.BuildPromotionAsync();
            model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
            model.Flash = flash;
            model.FlashType = flashType;
            return View("AdminDashboard", model);
        }

        if (page == "add_user")
        {
            var firstName = Request.Form["first_name"].ToString();
            var middleName = Request.Form["middle_name"].ToString();
            var lastName = Request.Form["last_name"].ToString();
            var email = Request.Form["email"].ToString();
            var roleIdStr = Request.Form["role_id"].ToString();
            var batchYearStr = Request.Form["batch_year"].ToString();

            string flash;
            string flashType;

            if (action == "add_user" && long.TryParse(roleIdStr, out var roleId) && int.TryParse(batchYearStr, out var batchYear))
            {
                (flash, flashType) = await _adminDashboardService.AddUserAsync(adminId, firstName, middleName, lastName, email, batchYear, roleId);
            }
            else
            {
                flash = "All fields required.";
                flashType = "error";
            }

            var model = await _adminDashboardService.BuildAddUserAsync();
            model.AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin";
            model.Flash = flash;
            model.FlashType = flashType;
            return View("AdminDashboard", model);
        }

        if (page == "bulk_insert")
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
            {
                var model = new AdminDashboardViewModel
                {
                    Page = "bulk_insert",
                    PageTitle = "Bulk User Insert",
                    AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin",
                    Flash = "File upload failed.",
                    FlashType = "error"
                };
                return View("AdminDashboard", model);
            }

            var selectedRole = Request.Form["bulk_role"].ToString();
            var batchYearStr = Request.Form["bulk_batch_year"].ToString();
            int? batchYear = null;
            if (int.TryParse(batchYearStr, out var by) && by > 0)
                batchYear = by;

            var loginUrl = $"{Request.Scheme}://{Request.Host}/Auth/Login";
            var result = await _bulkInsertService.ProcessBulkInsertAsync(adminId, file, loginUrl, selectedRole, batchYear);

            var flash = $"Process Complete. Created: {result.CreatedCount}. Skipped (duplicate): {result.SkippedDuplicateCount}. Email Failed: {result.EmailFailedCount}. Failed: {result.FailedCount}.";
            var modelResult = new AdminDashboardViewModel
            {
                Page = "bulk_insert",
                PageTitle = "Bulk User Insert",
                AdminEmail = HttpContext.Session.GetString("admin_email") ?? "admin",
                Flash = flash,
                FlashType = result.Errors.Count > 0 ? "error" : "info"
            };
            return View("AdminDashboard", modelResult);
        }

        return RedirectToAction("AdminDashboard", new { page });
    }

    [HttpGet("/Dashboard/UserDashboard")]
    
    public async Task<IActionResult> UserDashboard()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        if (!isLoggedIn)
            return Redirect("/Auth/Login");

        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard");

        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        var subRole = (HttpContext.Session.GetString("sub_role") ?? "").Trim().ToLowerInvariant();
        if (role != "user")
            return Redirect("/Auth/Login");

        return subRole switch
        {
            "student" => RedirectToAction("StudentDashboard"),
            "alumni" => RedirectToAction("AlumniDashboard"),
            "faculty" => RedirectToAction("FacultyDashboard"),
            _ => Redirect("/Auth/Login")
        };
    }

    public async Task<IActionResult> StudentDashboard()
    {
        if (!IsUserLoggedInWithRole("student"))
            return RedirectToRoleDashboard();

        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard");

        var model = await BuildUserDashboardModelAsync("student");
        if (model == null)
            return RedirectToAction("Login", "Auth");

        return View("StudentDashboard", model);
    }

    public async Task<IActionResult> AlumniDashboard()
    {
        if (!IsUserLoggedInWithRole("alumni"))
            return RedirectToRoleDashboard();

        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard");

        var model = await BuildUserDashboardModelAsync("alumni");
        if (model == null)
            return RedirectToAction("Login", "Auth");

        return View("AlumniDashboard", model);
    }

    public async Task<IActionResult> FacultyDashboard()
    {
        if (!IsUserLoggedInWithRole("faculty"))
            return RedirectToRoleDashboard();

        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard");

        var model = await BuildUserDashboardModelAsync("faculty");
        if (model == null)
            return RedirectToAction("Login", "Auth");

        return View("FacultyDashboard", model);
    }

    private async Task<UserDashboardViewModel?> BuildUserDashboardModelAsync(string requiredRole)
    {
        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return null;

        var roleName = await _authService.GetRoleNameByUserIdAsync(userId);
        var normalizedRole = (roleName ?? "").Trim().ToLowerInvariant();

        if (normalizedRole != requiredRole)
            return null;

        var userData = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where u.UserId == userId
            select new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
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
            ? profile.ProfilePhotoUrl!
            : $"https://placehold.co/100x100/6366f1/white?text={firstLetter}";

        var profileHeadline = !string.IsNullOrWhiteSpace(profile?.Headline)
            ? profile!.Headline!
            : CapitalizeFirstLetter(userData.Role);

        var facultyRoleId = await _context.Roles
            .Where(r => r.Role1.ToLower() == "faculty")
            .Select(r => (long?)r.RoleId)
            .FirstOrDefaultAsync() ?? 0;

        var upcomingEvents = new List<UserDashboardEventItem>();
        if (facultyRoleId > 0)
        {
            var events = await _context.Events
                .Where(e => e.RoleId == facultyRoleId && e.StartTime >= DateTime.Now)
                .OrderBy(e => e.StartTime)
                .Take(3)
                .ToListAsync();

            foreach (var ev in events)
            {
                upcomingEvents.Add(new UserDashboardEventItem
                {
                    EventId = ev.EventId,
                    Title = ev.Title,
                    StartTimeText = ev.StartTime.ToString("MMM d, h:mm tt")
                });
            }
        }

        var announcements = await _context.Announcements
            .Where(a => a.IsActive == true)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var announcementItems = new List<UserDashboardAnnouncementItem>();
        foreach (var ann in announcements)
        {
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ann.Content));
            announcementItems.Add(new UserDashboardAnnouncementItem
            {
                AnnouncementId = ann.AnnouncementId,
                Title = ann.Title,
                ContentBase64 = b64,
                CreatedAtText = ann.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        var acceptedConnections = await _context.Connections
            .Where(c => (c.RequesterId == userId || c.AddresseeId == userId) && c.Status == "accepted")
            .Select(c => c.RequesterId == userId ? c.AddresseeId : c.RequesterId)
            .Distinct()
            .ToListAsync();

        var posts = await _context.Posts
            .Where(p => p.UserId == userId || (p.UserId != null && acceptedConnections.Contains(p.UserId.Value)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var postAuthorIds = posts.Where(p => p.UserId.HasValue).Select(p => p.UserId!.Value).Distinct().ToList();
        var authorProfiles = await _context.Profiles
            .Where(p => p.UserId != null && postAuthorIds.Contains(p.UserId.Value))
            .ToListAsync();

        var authors = await _context.Users
            .Where(u => postAuthorIds.Contains(u.UserId))
            .ToListAsync();

        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId && f.FollowingId != null)
            .Select(f => f.FollowingId!.Value)
            .ToListAsync();

        var postIds = posts.Select(p => p.PostId).ToList();
        var comments = await _context.Engagements
            .Where(e => e.PostId != null && postIds.Contains(e.PostId.Value) && e.EngagementType == "comment")
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var commentUserIds = comments.Where(c => c.UserId != null).Select(c => c.UserId!.Value).Distinct().ToList();
        var commentUsers = await _context.Users
            .Where(u => commentUserIds.Contains(u.UserId))
            .ToListAsync();
        var commentProfiles = await _context.Profiles
            .Where(p => p.UserId != null && commentUserIds.Contains(p.UserId.Value))
            .ToListAsync();

        var postItems = new List<UserDashboardPostItem>();
        foreach (var post in posts)
        {
            var authorId = post.UserId ?? 0;
            var author = authors.FirstOrDefault(a => a.UserId == authorId);
            var authorProfile = authorProfiles.FirstOrDefault(p => p.UserId == authorId);
            var authorName = $"{author?.FirstName} {author?.LastName}".Trim();

            var authorPhoto = !string.IsNullOrWhiteSpace(authorProfile?.ProfilePhotoUrl)
                ? authorProfile!.ProfilePhotoUrl!
                : "https://placehold.co/50x50";

            var contentEncoded = HtmlEncoder.Default.Encode(post.Content);
            var contentHtml = contentEncoded.Replace("\n", "<br>");

            var postItem = new UserDashboardPostItem
            {
                PostId = post.PostId,
                AuthorId = authorId,
                AuthorName = authorName,
                AuthorProfilePhoto = authorPhoto,
                CreatedAtText = post.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ContentHtml = contentHtml,
                PostType = post.PostType,
                PhotoUrl = post.PhotoUrl,
                LikesCount = post.LikesCount,
                CommentsCount = post.CommentsCount,
                SharesCount = post.SharesCount,
                IsFollowing = followingIds.Contains(authorId)
            };

            var postComments = comments.Where(c => c.PostId == post.PostId).ToList();
            foreach (var comment in postComments)
            {
                var cUserId = comment.UserId ?? 0;
                var cUser = commentUsers.FirstOrDefault(u => u.UserId == cUserId);
                var cProfile = commentProfiles.FirstOrDefault(p => p.UserId == cUserId);
                var cName = $"{cUser?.FirstName} {cUser?.LastName}".Trim();
                var cPhoto = !string.IsNullOrWhiteSpace(cProfile?.ProfilePhotoUrl)
                    ? cProfile!.ProfilePhotoUrl!
                    : "https://placehold.co/30x30";

                postItem.Comments.Add(new UserDashboardCommentItem
                {
                    PostId = post.PostId,
                    CommenterName = cName,
                    CommenterPhoto = cPhoto,
                    Content = HtmlEncoder.Default.Encode(comment.Content ?? "")
                });
            }

            postItems.Add(postItem);
        }

        return new UserDashboardViewModel
        {
            UserId = userData.UserId,
            UserRole = normalizedRole,
            UserName = $"{userData.FirstName} {userData.LastName}".Trim(),
            ProfilePhoto = profilePhoto,
            ProfileHeadline = profileHeadline,
            CurrentPage = "UserDashboard",
            UpcomingEvents = upcomingEvents,
            Announcements = announcementItems,
            FeedPosts = postItems
        };
    }

    private static string CapitalizeFirstLetter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        if (trimmed.Length == 1)
            return trimmed.ToUpperInvariant();

        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1).ToLowerInvariant();
    }

    private bool IsUserLoggedInWithRole(string requiredSubRole)
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        if (!isLoggedIn)
            return false;

        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        var subRole = (HttpContext.Session.GetString("sub_role") ?? "").Trim().ToLowerInvariant();

        return role == "user" && subRole == requiredSubRole;
    }

    private IActionResult RedirectToRoleDashboard()
    {
        var isLoggedIn = string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase);
        if (!isLoggedIn)
            return RedirectToAction("Login", "Auth");

        var role = (HttpContext.Session.GetString("role") ?? "").Trim().ToLowerInvariant();
        var subRole = (HttpContext.Session.GetString("sub_role") ?? "").Trim().ToLowerInvariant();

        if (role == "admin")
            return RedirectToAction("AdminDashboard");

        return subRole switch
        {
            "student" => RedirectToAction("StudentDashboard"),
            "alumni" => RedirectToAction("AlumniDashboard"),
            "faculty" => RedirectToAction("FacultyDashboard"),
            _ => RedirectToAction("Login", "Auth")
        };
    }
}

