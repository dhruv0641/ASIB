namespace ASIB.Models.ViewModels;

public class AdminDashboardViewModel
{
    public string Page { get; set; } = "dashboard";
    public string PageTitle { get; set; } = "Admin Panel";
    public string AdminEmail { get; set; } = "admin";
    public string Flash { get; set; } = "";
    public string FlashType { get; set; } = "info";

    public int ActiveCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public int BlockedCount { get; set; }
    public int StudentCount { get; set; }
    public int FacultyCount { get; set; }
    public int AlumniCount { get; set; }

    public List<AdminVerificationUser> VerificationList { get; set; } = new();
    public List<AdminUserSummary> ActiveUsers { get; set; } = new();
    public List<AdminUserSummary> BlockedUsers { get; set; } = new();

    public string AllUsersFilter { get; set; } = "all";
    public string AllUsersView { get; set; } = "list";
    public List<AdminAllUserRow> AllUsersList { get; set; } = new();
    public AdminUserDetails? UserDetails { get; set; }

    public string EventsView { get; set; } = "list";
    public List<AdminEventListItem> EventsList { get; set; } = new();
    public AdminEventDetails? EventDetails { get; set; }

    public string AnnouncementsView { get; set; } = "list";
    public List<AdminAnnouncementItem> Announcements { get; set; } = new();
    public AdminAnnouncementItem? AnnouncementEdit { get; set; }

    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public AdminActionReport? ReportData { get; set; }
    public List<AdminActionLogItem> AdminLogs { get; set; } = new();

    public List<AdminPromotionEligibleItem> EligibleStudents { get; set; } = new();
    public List<AdminPromotedAlumniItem> PromotedAlumni { get; set; } = new();
    public List<AdminPromotionManageUser> PromotionManageUsers { get; set; } = new();
    public List<int> PromotionBatchYears { get; set; } = new();

    public List<AdminRoleItem> RolesForForm { get; set; } = new();
}

public class AdminVerificationUser
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public int VerificationStatus { get; set; }
    public string RequestedRole { get; set; } = "";
    public string EnrollmentNumber { get; set; } = "";
}

public class AdminUserSummary
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}

public class AdminAllUserRow
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string EnrollmentNumber { get; set; } = "";
    public string Role { get; set; } = "";
    public int VerificationStatus { get; set; }
}

public class AdminUserDetails
{
    public AdminUserDetailsUser User { get; set; } = new();
    public AdminUserDetailsProfile? Profile { get; set; }
    public int FollowingCount { get; set; }
    public int ConnectionCount { get; set; }
    public int EventCount { get; set; }
    public List<AdminUserPost> Posts { get; set; } = new();
}

public class AdminUserDetailsUser
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string ContactNumber { get; set; } = "";
    public string Role { get; set; } = "";
    public string BatchYear { get; set; } = "";
    public string EnrollmentNumber { get; set; } = "";
    public int VerificationStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Address { get; set; } = "";
}

public class AdminUserDetailsProfile
{
    public string Headline { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Skills { get; set; } = "";
    public string Experience { get; set; } = "";
}

public class AdminUserPost
{
    public string Content { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
}

public class AdminEventListItem
{
    public long EventId { get; set; }
    public string Title { get; set; } = "";
    public string CreatorFirstName { get; set; } = "";
    public string CreatorLastName { get; set; } = "";
    public DateTime? StartTime { get; set; }
}

public class AdminEventDetails
{
    public AdminEventDetailsEvent Event { get; set; } = new();
    public List<AdminEventAttendee> Attendees { get; set; } = new();
}

public class AdminEventDetailsEvent
{
    public long EventId { get; set; }
    public string Title { get; set; } = "";
    public string CreatorFirstName { get; set; } = "";
    public string CreatorLastName { get; set; } = "";
    public long CreatorId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string MeetingUrl { get; set; } = "";
    public string Description { get; set; } = "";
}

public class AdminEventAttendee
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}

public class AdminAnnouncementItem
{
    public long AnnouncementId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsActive { get; set; }
    public string AdminEmail { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
}

public class AdminActionReport
{
    public int ApproveUser { get; set; }
    public int RejectUser { get; set; }
    public int BlockUser { get; set; }
    public int Total { get; set; }
    public string StartDate { get; set; } = "";
    public string EndDate { get; set; } = "";
}

public class AdminActionLogItem
{
    public DateTime? ActionTime { get; set; }
    public string ActionType { get; set; } = "";
    public long? TargetUserId { get; set; }
    public long? TargetEventId { get; set; }
    public string Reason { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string UserFirstName { get; set; } = "";
    public string UserLastName { get; set; } = "";
}

public class AdminPromotionEligibleItem
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int BatchYear { get; set; }
    public string EnrollmentNumber { get; set; } = "";
}

public class AdminPromotedAlumniItem
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int BatchYear { get; set; }
    public DateTime? PromotedAt { get; set; }
}

public class AdminPromotionManageUser
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int BatchYear { get; set; }
    public string EnrollmentNumber { get; set; } = "";
    public long RoleId { get; set; }
    public string Role { get; set; } = "";
}

public class AdminRoleItem
{
    public long RoleId { get; set; }
    public string Role { get; set; } = "";
}

public class AdminPromotedAlumniRow
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int? BatchYear { get; set; }
    public DateTime? PromotedAt { get; set; }
}
