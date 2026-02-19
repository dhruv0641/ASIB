using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace ASIB.Core.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AsibContext _context;
    private readonly IConfiguration _configuration;

    public AdminDashboardService(AsibContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AdminDashboardViewModel> BuildDashboardAsync()
    {
        var activeCount = await CountUsersByStatusAsync(1);
        var pendingCount = await CountUsersByStatusAsync(0);
        var rejectedCount = await CountUsersByStatusAsync(-1);
        var blockedCount = await CountUsersByStatusAsync(2);
        var studentCount = await CountByRoleNameAsync("student");
        var facultyCount = await CountByRoleNameAsync("faculty");
        var alumniCount = await CountByRoleNameAsync("alumni");

        return new AdminDashboardViewModel
        {
            Page = "dashboard",
            PageTitle = "Dashboard",
            ActiveCount = activeCount,
            PendingCount = pendingCount,
            RejectedCount = rejectedCount,
            BlockedCount = blockedCount,
            StudentCount = studentCount,
            FacultyCount = facultyCount,
            AlumniCount = alumniCount
        };
    }

    public async Task<AdminDashboardViewModel> BuildVerificationAsync()
    {
        var list = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleRequested equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where u.VerificationStatus == 0 || u.VerificationStatus == -1
            orderby u.VerificationStatus ascending, u.UserId descending
            select new AdminVerificationUser
            {
                UserId = u.UserId,
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                ContactNumber = u.ContactNumber.ToString(),
                VerificationStatus = u.VerificationStatus ?? 0,
                RequestedRole = r != null ? r.Role1 : "",
                EnrollmentNumber = u.EnrollmentNumber ?? ""
            }
        ).ToListAsync();

        return new AdminDashboardViewModel
        {
            Page = "verification",
            PageTitle = "User Verification",
            VerificationList = list
        };
    }

    public async Task<AdminDashboardViewModel> BuildSuspendAsync()
    {
        var activeUsers = await FetchUsersAsync("active");
        var blockedUsers = await FetchUsersAsync("blocked");

        return new AdminDashboardViewModel
        {
            Page = "suspend",
            PageTitle = "Suspend / Block Users",
            ActiveUsers = activeUsers,
            BlockedUsers = blockedUsers
        };
    }

    public async Task<AdminDashboardViewModel> BuildAllUsersListAsync(string filter)
    {
        var normalized = (filter ?? "").Trim().ToLowerInvariant();
        var allowed = new HashSet<string> { "all", "active", "pending", "rejected", "blocked", "student", "faculty", "alumni" };
        if (!allowed.Contains(normalized))
            normalized = "all";

        var list = await FetchAllUsersAsync(normalized == "all" ? "" : normalized);

        return new AdminDashboardViewModel
        {
            Page = "all_users",
            PageTitle = "All Users",
            AllUsersFilter = normalized,
            AllUsersView = "list",
            AllUsersList = list
        };
    }

    public async Task<AdminDashboardViewModel?> BuildAllUsersDetailAsync(long userId)
    {
        var details = await FetchUserDetailsAsync(userId);
        if (details == null)
            return null;

        return new AdminDashboardViewModel
        {
            Page = "all_users",
            PageTitle = $"User Details: {details.User.FirstName} {details.User.LastName}".Trim(),
            AllUsersView = "detail",
            UserDetails = details
        };
    }

    public async Task<AdminDashboardViewModel> BuildEventsListAsync()
    {
        var list = await (
            from e in _context.Events
            join u in _context.Users on e.CreatedBy equals u.UserId into eu
            from u in eu.DefaultIfEmpty()
            orderby e.StartTime descending
            select new AdminEventListItem
            {
                EventId = e.EventId,
                Title = e.Title ?? "",
                CreatorFirstName = u != null ? (u.FirstName ?? "") : "",
                CreatorLastName = u != null ? (u.LastName ?? "") : "",
                StartTime = e.StartTime
            }
        ).ToListAsync();

        return new AdminDashboardViewModel
        {
            Page = "events",
            PageTitle = "Manage Events",
            EventsView = "list",
            EventsList = list
        };
    }

    public async Task<AdminDashboardViewModel?> BuildEventDetailsAsync(long eventId)
    {
        var eventEntity = await (
            from e in _context.Events
            join u in _context.Users on e.CreatedBy equals u.UserId into eu
            from u in eu.DefaultIfEmpty()
            where e.EventId == eventId
            select new AdminEventDetailsEvent
            {
                EventId = e.EventId,
                Title = e.Title ?? "",
                CreatorFirstName = u != null ? (u.FirstName ?? "") : "",
                CreatorLastName = u != null ? (u.LastName ?? "") : "",
                CreatorId = e.CreatedBy ?? 0,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                MeetingUrl = e.MeetingUrl ?? "",
                Description = e.Description ?? ""
            }
        ).FirstOrDefaultAsync();

        if (eventEntity == null)
            return null;

        var attendees = await (
            from er in _context.EventRequests
            join u in _context.Users on er.UserId equals u.UserId
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where er.EventId == eventId && er.Status == "approved"
            orderby u.LastName, u.FirstName
            select new AdminEventAttendee
            {
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                Role = r != null ? r.Role1 : ""
            }
        ).ToListAsync();

        return new AdminDashboardViewModel
        {
            Page = "events",
            PageTitle = $"Event Details: {eventEntity.Title}",
            EventsView = "detail",
            EventDetails = new AdminEventDetails
            {
                Event = eventEntity,
                Attendees = attendees
            }
        };
    }

    public async Task<AdminDashboardViewModel> BuildAnnouncementsListAsync()
    {
        var list = await (
            from a in _context.Announcements
            join ad in _context.Admins on a.AdminId equals ad.AdminId
            orderby a.CreatedAt descending
            select new AdminAnnouncementItem
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title ?? "",
                Content = a.Content ?? "",
                IsActive = a.IsActive == true,
                AdminEmail = ad.Email ?? "",
                CreatedAt = a.CreatedAt
            }
        ).ToListAsync();

        return new AdminDashboardViewModel
        {
            Page = "announcements",
            PageTitle = "Manage Announcements",
            AnnouncementsView = "list",
            Announcements = list
        };
    }

    public async Task<AdminDashboardViewModel?> BuildAnnouncementEditAsync(long announcementId, bool isAddView)
    {
        if (isAddView)
        {
            return new AdminDashboardViewModel
            {
                Page = "announcements",
                PageTitle = "Add Announcement",
                AnnouncementsView = "add",
                AnnouncementEdit = new AdminAnnouncementItem { IsActive = true }
            };
        }

        var item = await (
            from a in _context.Announcements
            join ad in _context.Admins on a.AdminId equals ad.AdminId
            where a.AnnouncementId == announcementId
            select new AdminAnnouncementItem
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title ?? "",
                Content = a.Content ?? "",
                IsActive = a.IsActive == true,
                AdminEmail = ad.Email ?? "",
                CreatedAt = a.CreatedAt
            }
        ).FirstOrDefaultAsync();

        if (item == null)
            return null;

        return new AdminDashboardViewModel
        {
            Page = "announcements",
            PageTitle = "Edit Announcement",
            AnnouncementsView = "edit",
            AnnouncementEdit = item
        };
    }

    public async Task<AdminDashboardViewModel> BuildAdminLogAsync(string? startDate, string? endDate)
    {
        var model = new AdminDashboardViewModel
        {
            Page = "admin_log",
            PageTitle = "Admin Action Log"
        };

        var hasDates = !string.IsNullOrWhiteSpace(startDate) && !string.IsNullOrWhiteSpace(endDate);
        if (hasDates)
        {
            if (DateTime.TryParse(startDate, out var startDt) && DateTime.TryParse(endDate, out var endDt) && endDt >= startDt)
            {
                var start = startDt.ToString("yyyy-MM-dd");
                var end = endDt.ToString("yyyy-MM-dd");
                model.StartDate = start;
                model.EndDate = end;

                model.ReportData = await GetActionReportAsync(start, end);
                model.AdminLogs = await FetchAdminLogAsync(start, end);
            }
            else
            {
                model.Flash = "Invalid date range provided. Please ensure end date is after start date.";
                model.FlashType = "error";
                model.AdminLogs = await FetchAdminLogAsync(null, null);
            }
        }
        else
        {
            model.AdminLogs = await FetchAdminLogAsync(null, null);
        }

        return model;
    }

    public async Task<AdminDashboardViewModel> BuildPromotionAsync()
    {
        var currentYear = DateTime.Now.Year;
        var cutoffYear = currentYear - 3;

        var eligibleStudents = await _context.Users
            .Where(u => u.RoleId == 1 && u.BatchYear != null && u.BatchYear <= cutoffYear && u.VerificationStatus == 1)
            .OrderBy(u => u.BatchYear)
            .Select(u => new AdminPromotionEligibleItem
            {
                UserId = u.UserId,
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                BatchYear = u.BatchYear ?? 0,
                EnrollmentNumber = u.EnrollmentNumber ?? ""
            })
            .ToListAsync();

        var promotedRows = await _context.AdminPromotedAlumniRows.FromSqlRaw(
            "SELECT u.user_id AS UserId, u.first_name AS FirstName, u.last_name AS LastName, u.email AS Email, u.batch_year AS BatchYear," +
            " (SELECT action_time FROM admin_actions WHERE target_user_id = u.user_id AND action_type = 'promote_user' ORDER BY action_time DESC LIMIT 1) AS PromotedAt" +
            " FROM users u WHERE u.role_id = 2 ORDER BY PromotedAt DESC"
        ).ToListAsync();

        var promotedAlumni = promotedRows.Select(r => new AdminPromotedAlumniItem
        {
            UserId = r.UserId,
            FirstName = r.FirstName ?? "",
            LastName = r.LastName ?? "",
            Email = r.Email ?? "",
            BatchYear = r.BatchYear ?? 0,
            PromotedAt = r.PromotedAt
        }).ToList();

        var manageUsers = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where u.VerificationStatus == 1
            orderby u.BatchYear descending, u.RoleId ascending
            select new AdminPromotionManageUser
            {
                UserId = u.UserId,
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                BatchYear = u.BatchYear ?? 0,
                EnrollmentNumber = u.EnrollmentNumber ?? "",
                RoleId = u.RoleId ?? 0,
                Role = r != null ? r.Role1 : ""
            }
        ).ToListAsync();

        var batchYears = manageUsers
            .Where(u => u.BatchYear > 0)
            .Select(u => u.BatchYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        return new AdminDashboardViewModel
        {
            Page = "promotion",
            PageTitle = "Student Promotions",
            EligibleStudents = eligibleStudents,
            PromotedAlumni = promotedAlumni,
            PromotionManageUsers = manageUsers,
            PromotionBatchYears = batchYears
        };
    }

    public async Task<AdminDashboardViewModel> BuildAddUserAsync()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.Role1)
            .Select(r => new AdminRoleItem
            {
                RoleId = r.RoleId,
                Role = r.Role1 ?? ""
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            Page = "add_user",
            PageTitle = "Add Single User",
            RolesForForm = roles
        };
    }

    public async Task<(string Flash, string FlashType)> BlockUserAsync(long adminId, long userId, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ($"A reason is required to block User #{userId}.", "error");

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE users SET verification_status = 2 WHERE user_id = {userId}"
        );
        if (affected > 0)
        {
            await LogAdminActionAsync(adminId, "block_user", "user", userId, null, reason.Trim());
            return ($"User #{userId} blocked.", "info");
        }

        return ($"User #{userId} already blocked or not found.", "info");
    }

    public async Task<(string Flash, string FlashType)> UnblockUserAsync(long adminId, long userId)
    {
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE users SET verification_status = 1 WHERE user_id = {userId}"
        );
        if (affected > 0)
        {
            await LogAdminActionAsync(adminId, "unblock_user", "user", userId, null, null);
            return ($"User #{userId} unblocked.", "info");
        }

        return ($"User #{userId} already active or not found.", "info");
    }

    public async Task<(string Flash, string FlashType)> DeleteEventAsync(long adminId, long eventId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM event_requests WHERE event_id = {eventId}"
            );

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM events WHERE event_id = {eventId}"
            );

            if (affected > 0)
            {
                await transaction.CommitAsync();
                await LogAdminActionAsync(adminId, "delete_event", "event", eventId, "Deleted event and associated requests.", null);
                return ($"Event #{eventId} and all associated requests have been deleted.", "info");
            }

            await transaction.RollbackAsync();
            return ($"Event #{eventId} not found or already deleted.", "info");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ($"Error deleting event: {ex.Message}", "error");
        }
    }

    public async Task<(string Flash, string FlashType)> AddAnnouncementAsync(long adminId, string title, string content, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return ("Title and Content are required.", "error");

        var announcement = new Announcement
        {
            AdminId = adminId,
            Title = title,
            Content = content,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        _context.Announcements.Add(announcement);
        var affected = await _context.SaveChangesAsync();

        if (affected > 0)
        {
            var newId = announcement.AnnouncementId;
            await LogAdminActionAsync(adminId, "add_announcement", "", 0, $"Added announcement #{newId}: {title}", null);
            return ("Announcement added successfully.", "info");
        }

        return ("DB Error: Insert failed.", "error");
    }

    public async Task<(string Flash, string FlashType)> EditAnnouncementAsync(long adminId, long announcementId, string title, string content, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return ("Title and Content are required.", "error");

        var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE announcements SET title = {title}, content = {content}, is_active = {(isActive ? 1 : 0)} WHERE announcement_id = {announcementId}"
        );

        if (affected > 0)
        {
            await LogAdminActionAsync(adminId, "edit_announcement", "", 0, $"Edited announcement #{announcementId}: {title}", null);
            return ($"Announcement #{announcementId} updated successfully.", "info");
        }

        return ("DB Error: Update failed.", "error");
    }

    public async Task<(string Flash, string FlashType)> DeleteAnnouncementAsync(long adminId, long announcementId)
    {
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM announcements WHERE announcement_id = {announcementId}"
        );

        if (affected > 0)
        {
            await LogAdminActionAsync(adminId, "delete_announcement", "", 0, $"Deleted announcement #{announcementId}", null);
            return ($"Announcement #{announcementId} deleted.", "info");
        }

        return ("DB Error: Delete failed.", "error");
    }

    public async Task<(string Flash, string FlashType)> PromoteSingleAsync(long adminId, long userId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE users SET role_id = 2 WHERE user_id = {userId}"
        );
        await LogAdminActionAsync(adminId, "promote_user", "user", userId, "Promoted to Alumni", null);
        return ($"User #{userId} successfully promoted to Alumni.", "info");
    }

    public async Task<(string Flash, string FlashType)> PromoteBulkAsync(long adminId, IEnumerable<long> userIds)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return ("No users selected for promotion.", "error");

        var count = 0;
        foreach (var uid in ids)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE users SET role_id = 2 WHERE user_id = {uid}"
            );
            await LogAdminActionAsync(adminId, "promote_user", "user", uid, "Bulk Promoted to Alumni", null);
            count++;
        }

        return ($"{count} users promoted to Alumni successfully.", "info");
    }

    public async Task<(string Flash, string FlashType)> DemoteSingleAsync(long adminId, long userId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE users SET role_id = 1 WHERE user_id = {userId}"
        );
        await LogAdminActionAsync(adminId, "demote_user", "user", userId, "Demoted back to Student", null);
        return ($"User #{userId} demoted to Student.", "info");
    }

    public async Task<(string Flash, string FlashType)> DemoteBulkAsync(long adminId, IEnumerable<long> userIds)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return ("No users selected for demotion.", "error");

        var count = 0;
        foreach (var uid in ids)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE users SET role_id = 1 WHERE user_id = {uid}"
            );
            await LogAdminActionAsync(adminId, "demote_user", "user", uid, "Bulk Demoted to Student", null);
            count++;
        }

        return ($"{count} users demoted to Student successfully.", "info");
    }

    public async Task<(string Flash, string FlashType)> AddUserAsync(long adminId, string firstName, string middleName, string lastName, string email, int batchYear, long roleId)
    {
        var defaultContact = "0000000000";
        var defaultAddress = "N/A";

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email) || roleId <= 0 || batchYear <= 0)
            return ("All fields required.", "error");

        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
            return ("Email already exists.", "error");

        var roleName = await _context.Roles
            .Where(r => r.RoleId == roleId)
            .Select(r => r.Role1)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(roleName))
            return ("All fields required.", "error");

        var normalizedRole = roleName.Trim().ToLowerInvariant();
        if (normalizedRole != "student" && normalizedRole != "alumni" && normalizedRole != "faculty")
            return ("All fields required.", "error");

        var plainPassword = GenerateRandomPassword(10);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        var user = new User
        {
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            RoleRequested = roleId,
            RoleId = roleId,
            VerificationStatus = 1,
            ContactNumber = long.Parse(defaultContact),
            Address = defaultAddress,
            BatchYear = batchYear,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var newId = user.UserId;
        await LogAdminActionAsync(adminId, "add_user", "user", newId, $"Created user: {email}", null);

        var flash = "User created successfully.";
        try
        {
            var mailResult = SendUserEmail(email, firstName, plainPassword);
            flash += mailResult ? " Email sent." : " Mail helper function not found.";
        }
        catch (Exception ex)
        {
            flash += " Email failed: " + ex.Message;
        }

        var reset = new PasswordReset
        {
            UserId = user.UserId,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            ExpiresAt = DateTime.Now.AddYears(1),
            Used = true,
            CreatedAt = DateTime.Now
        };
        _context.PasswordResets.Add(reset);
        await _context.SaveChangesAsync();

        return (flash, "info");
    }

    private async Task<List<AdminActionLogItem>> FetchAdminLogAsync(string? startDate, string? endDate)
    {
        var query =
            from aa in _context.AdminActions
            join a in _context.Admins on aa.AdminId equals a.AdminId
            join u in _context.Users on aa.TargetUserId equals u.UserId into au
            from u in au.DefaultIfEmpty()
            select new { aa, a, u };

        if (!string.IsNullOrWhiteSpace(startDate) && !string.IsNullOrWhiteSpace(endDate))
        {
            var start = DateTime.Parse(startDate + " 00:00:00");
            var end = DateTime.Parse(endDate + " 23:59:59");
            query = query.Where(x => x.aa.ActionTime >= start && x.aa.ActionTime <= end);
        }

        query = query.OrderByDescending(x => x.aa.ActionTime);

        if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
            query = query.Take(50);

        return await query.Select(x => new AdminActionLogItem
        {
            ActionTime = x.aa.ActionTime,
            ActionType = x.aa.ActionType ?? "",
            TargetUserId = x.aa.TargetUserId,
            TargetEventId = x.aa.TargetEventId,
            Reason = x.aa.Reason ?? "",
            AdminEmail = x.a.Email ?? "N/A",
            UserEmail = x.u != null ? (x.u.Email ?? "") : "",
            UserFirstName = x.u != null ? (x.u.FirstName ?? "") : "",
            UserLastName = x.u != null ? (x.u.LastName ?? "") : ""
        }).ToListAsync();
    }

    private async Task<AdminActionReport> GetActionReportAsync(string startDate, string endDate)
    {
        var start = DateTime.Parse(startDate + " 00:00:00");
        var end = DateTime.Parse(endDate + " 23:59:59");

        var rows = await _context.AdminActions
            .Where(a => a.ActionTime >= start && a.ActionTime <= end)
            .Where(a => a.ActionType == "approve_user" || a.ActionType == "reject_user" || a.ActionType == "block_user")
            .GroupBy(a => a.ActionType)
            .Select(g => new { ActionType = g.Key, Count = g.Count() })
            .ToListAsync();

        var report = new AdminActionReport
        {
            StartDate = startDate,
            EndDate = endDate
        };

        foreach (var row in rows)
        {
            if (row.ActionType == "approve_user")
            {
                report.ApproveUser = row.Count;
                report.Total += row.Count;
            }
            else if (row.ActionType == "reject_user")
            {
                report.RejectUser = row.Count;
                report.Total += row.Count;
            }
            else if (row.ActionType == "block_user")
            {
                report.BlockUser = row.Count;
                report.Total += row.Count;
            }
        }

        return report;
    }

    private async Task<int> CountUsersByStatusAsync(int status)
    {
        return await _context.Users.CountAsync(u => u.VerificationStatus == status);
    }

    private async Task<int> CountByRoleNameAsync(string roleName)
    {
        var roleNameLower = roleName.ToLowerInvariant();

        return await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId
            where r.Role1.ToLower() == roleNameLower && u.VerificationStatus == 1
            select u.UserId
        ).CountAsync();
    }

    private async Task<List<AdminUserSummary>> FetchUsersAsync(string filter)
    {
        var query =
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            select new { u, r };

        var filterValue = filter.Trim().ToLowerInvariant();
        if (filterValue == "active")
        {
            query = query.Where(x => x.u.VerificationStatus == 1);
        }
        else if (filterValue == "blocked")
        {
            query = query.Where(x => x.u.VerificationStatus == 2);
        }
        else if (filterValue == "pending")
        {
            query = query.Where(x => x.u.VerificationStatus == 0);
        }
        else if (filterValue == "rejected")
        {
            query = query.Where(x => x.u.VerificationStatus == -1);
        }
        else if (filterValue == "student" || filterValue == "faculty" || filterValue == "alumni")
        {
            query = query.Where(x => (x.r != null && x.r.Role1.ToLower() == filterValue));
        }

        return await query
            .OrderByDescending(x => x.u.UserId)
            .Select(x => new AdminUserSummary
            {
                UserId = x.u.UserId,
                FirstName = x.u.FirstName ?? "",
                LastName = x.u.LastName ?? "",
                Email = x.u.Email ?? "",
                Role = x.r != null ? x.r.Role1 : ""
            })
            .ToListAsync();
    }

    private async Task<List<AdminAllUserRow>> FetchAllUsersAsync(string filter)
    {
        var query =
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            select new { u, r };

        var filterValue = filter.Trim().ToLowerInvariant();
        if (filterValue == "active")
        {
            query = query.Where(x => x.u.VerificationStatus == 1);
        }
        else if (filterValue == "blocked")
        {
            query = query.Where(x => x.u.VerificationStatus == 2);
        }
        else if (filterValue == "pending")
        {
            query = query.Where(x => x.u.VerificationStatus == 0);
        }
        else if (filterValue == "rejected")
        {
            query = query.Where(x => x.u.VerificationStatus == -1);
        }
        else if (filterValue == "student" || filterValue == "faculty" || filterValue == "alumni")
        {
            query = query.Where(x => (x.r != null && x.r.Role1.ToLower() == filterValue));
        }

        return await query
            .OrderByDescending(x => x.u.UserId)
            .Select(x => new AdminAllUserRow
            {
                UserId = x.u.UserId,
                FirstName = x.u.FirstName ?? "",
                LastName = x.u.LastName ?? "",
                Email = x.u.Email ?? "",
                EnrollmentNumber = x.u.EnrollmentNumber ?? "",
                Role = x.r != null ? x.r.Role1 : "",
                VerificationStatus = x.u.VerificationStatus ?? 0
            })
            .ToListAsync();
    }

    private async Task<AdminUserDetails?> FetchUserDetailsAsync(long userId)
    {
        var user = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            where u.UserId == userId
            select new AdminUserDetailsUser
            {
                UserId = u.UserId,
                FirstName = u.FirstName ?? "",
                MiddleName = u.MiddleName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                ContactNumber = u.ContactNumber.ToString(),
                Role = r != null ? r.Role1 : "",
                BatchYear = u.BatchYear != null ? u.BatchYear.ToString() : "",
                EnrollmentNumber = u.EnrollmentNumber ?? "",
                VerificationStatus = u.VerificationStatus ?? 0,
                CreatedAt = u.CreatedAt,
                Address = u.Address ?? ""
            }
        ).FirstOrDefaultAsync();

        if (user == null)
            return null;

        var profileEntity = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var profile = profileEntity == null
            ? null
            : new AdminUserDetailsProfile
            {
                Headline = profileEntity.Headline ?? "",
                Bio = profileEntity.Bio ?? "",
                Skills = profileEntity.Skills ?? "",
                Experience = profileEntity.Experience ?? ""
            };

        var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == userId);
        var connectionCount = await _context.Connections.CountAsync(c => (c.RequesterId == userId || c.AddresseeId == userId) && c.Status == "accepted");
        var eventCount = await _context.EventRequests.CountAsync(e => e.UserId == userId && e.Status == "approved");

        var posts = await _context.Posts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new AdminUserPost
            {
                Content = p.Content ?? "",
                PhotoUrl = p.PhotoUrl ?? "",
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new AdminUserDetails
        {
            User = user,
            Profile = profile,
            FollowingCount = followingCount,
            ConnectionCount = connectionCount,
            EventCount = eventCount,
            Posts = posts
        };
    }

    private async Task LogAdminActionAsync(long adminId, string actionType, string targetType, long targetId, string? description, string? reason)
    {
        if (targetType == "user" && targetId > 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO admin_actions (admin_id, action_type, target_user_id, description, reason, action_time) VALUES ({adminId}, {actionType}, {targetId}, {description}, {reason}, NOW())"
            );
            return;
        }

        if (targetType == "event" && targetId > 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO admin_actions (admin_id, action_type, target_event_id, description, reason, action_time) VALUES ({adminId}, {actionType}, {targetId}, {description}, {reason}, NOW())"
            );
            return;
        }

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO admin_actions (admin_id, action_type, description, reason, action_time) VALUES ({adminId}, {actionType}, {description}, {reason}, NOW())"
        );
    }

    private static string GenerateRandomPassword(int length)
    {
        var bytes = new byte[(int)Math.Ceiling(length / 2.0)];
        RandomNumberGenerator.Fill(bytes);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return hex.Substring(0, length);
    }

    private static string GetWelcomeEmailBody(string firstName, string email, string password)
    {
        return @"
    <div style=""font-family: Arial, sans-serif; font-size: 14px; color: #333; line-height: 1.5;"">
        <p>Hello " + WebUtility.HtmlEncode(firstName) + @",</p>
        
        <p>Your account has been provisioned. Please find your access credentials below.</p>
        
        <div style=""margin: 15px 0; padding: 15px; background-color: #f8f9fa; border-left: 3px solid #004182;"">
            <p style=""margin: 0 0 5px 0;""><strong>Username:</strong> " + WebUtility.HtmlEncode(email) + @"</p>
            <p style=""margin: 0;""><strong>Password:</strong> " + WebUtility.HtmlEncode(password) + @"</p>
        </div>
        
        <p>Please keep these credentials secure.</p>
        
        <p style=""color: #888; font-size: 12px; margin-top: 30px;"">
            ASIB Admin System<br>
            <i>Automated Notification</i>
        </p>
    </div>";
    }

    private bool SendUserEmail(string toEmail, string firstName, string plainPassword)
    {
        var smtp = _configuration.GetSection("Smtp");
        var smtpHost = smtp["Host"] ?? "";
        var smtpUser = smtp["User"] ?? "";
        var smtpPass = smtp["Pass"] ?? "";
        var smtpPort = int.TryParse(smtp["Port"], out var p) ? p : 587;
        var mailFrom = smtp["From"] ?? "";
        var mailFromName = smtp["FromName"] ?? "";

        using var message = new MailMessage();
        message.From = new MailAddress(mailFrom, mailFromName);
        message.ReplyToList.Add(new MailAddress("asibbmiit@gmail.com", "ASIB Support"));
        message.To.Add(new MailAddress(toEmail, firstName));
        message.Subject = "Account Access Details";
        message.Body = GetWelcomeEmailBody(firstName, toEmail, plainPassword);
        message.IsBodyHtml = true;
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            $"Hello {firstName},\n\nYour account is ready.\n\nUsername: {toEmail}\nPassword: {plainPassword}",
            Encoding.UTF8,
            "text/plain"
        ));

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        client.Send(message);
        return true;
    }
}
