using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace ASIB.Core.Services;

public class EventService : IEventService
{
    private readonly AsibContext _context;

    public EventService(AsibContext context)
    {
        _context = context;
    }

    public async Task<EventPageViewModel?> BuildEventPageModelAsync(long userId, string? view)
    {
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
                u.RoleId,
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
            : userData.Role;

        var userRole = (userData.Role ?? "").Trim().ToLowerInvariant();
        var isFaculty = userRole == "faculty";
        var isScheduleView = string.Equals(view, "schedule", StringComparison.OrdinalIgnoreCase);

        var model = new EventPageViewModel
        {
            IsFaculty = isFaculty,
            IsScheduleView = isScheduleView,
            PageTitle = isFaculty ? "Manage Events" : "Event Schedule",
            UserId = userId,
            UserName = $"{userData.FirstName} {userData.LastName}",
            ProfilePhoto = profilePhoto,
            ProfileHeadline = profileHeadline,
            CurrentPage = "Event"
        };

        if (isFaculty)
        {
            var events = await _context.Events
                .Where(e => e.CreatedBy == userId)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();

            foreach (var ev in events)
            {
                var item = new FacultyEventItem
                {
                    EventId = ev.EventId,
                    Title = ev.Title,
                    StartDateText = ev.StartTime.ToString("MMM d, yyyy")
                };

                var requests = await (
                    from r in _context.EventRequests
                    join u in _context.Users on r.UserId equals u.UserId
                    where r.EventId == ev.EventId
                    orderby r.RequestedAt descending
                    select new
                    {
                        r.RequestId,
                        r.Status,
                        u.FirstName,
                        u.LastName
                    }
                ).ToListAsync();

                var approvedCount = 0;
                foreach (var req in requests)
                {
                    if (req.Status == "approved")
                        approvedCount++;
                    if (req.Status == "pending")
                    {
                        item.PendingRequests.Add(new FacultyPendingRequestItem
                        {
                            RequestId = req.RequestId,
                            FirstName = req.FirstName ?? "",
                            LastName = req.LastName ?? ""
                        });
                    }
                }

                item.ApprovedCount = approvedCount;
                model.FacultyEvents.Add(item);
            }

            return model;
        }

        var facultyRoleId = await _context.Roles
            .Where(r => r.Role1.ToLower() == "faculty")
            .Select(r => (long?)r.RoleId)
            .FirstOrDefaultAsync() ?? 0;

        if (facultyRoleId <= 0)
        {
            model.FoundEvents = false;
            return model;
        }

        var eventsSchedule = await (
            from e in _context.Events
            join u in _context.Users on e.CreatedBy equals u.UserId
            join er in _context.EventRequests on new { E = e.EventId, U = userId } equals new { E = er.EventId, U = er.UserId } into erj
            from er in erj.DefaultIfEmpty()
            where e.RoleId == facultyRoleId
            orderby e.StartTime
            select new
            {
                e.EventId,
                e.Title,
                e.Description,
                e.StartTime,
                e.MeetingUrl,
                u.FirstName,
                u.LastName,
                RequestStatus = er != null ? er.Status : null
            }
        ).ToListAsync();

        var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, ist);
        var found = false;

        foreach (var ev in eventsSchedule)
        {
            var eventStart = ev.StartTime;
            if (eventStart >= now)
            {
                found = true;
                var isStartingSoon = false;
                var minutesUntilStart = 0;
                if (ev.RequestStatus == "approved" && eventStart > now)
                {
                    var interval = eventStart - now;
                    minutesUntilStart = (interval.Days * 24 * 60) + (interval.Hours * 60) + interval.Minutes;
                    if (minutesUntilStart <= 10)
                        isStartingSoon = true;
                }

                var descriptionHtml = !string.IsNullOrEmpty(ev.Description)
                    ? HtmlEncoder.Default.Encode(ev.Description).Replace("\n", "<br>")
                    : "No description provided.";

                var statusLabel = !string.IsNullOrEmpty(ev.RequestStatus)
                    ? char.ToUpperInvariant(ev.RequestStatus[0]) + ev.RequestStatus.Substring(1)
                    : "";

                model.ScheduleEvents.Add(new ScheduleEventItem
                {
                    EventId = ev.EventId,
                    Title = ev.Title,
                    DescriptionHtml = descriptionHtml,
                    StartTimeText = eventStart.ToString("ddd, MMM d, yyyy, h:mm tt"),
                    OrganizerName = $"{ev.FirstName} {ev.LastName}",
                    RequestStatus = ev.RequestStatus,
                    RequestStatusLabel = statusLabel,
                    IsStartingSoon = isStartingSoon,
                    MinutesUntilStart = minutesUntilStart,
                    MeetingUrl = ev.MeetingUrl
                });
            }
        }

        model.FoundEvents = found;
        return model;
    }

    public async Task<string?> GetUserRoleAsync(long userId)
    {
        return await _context.Users
            .Where(u => u.UserId == userId)
            .Join(_context.Roles, u => u.RoleId, r => r.RoleId, (u, r) => r.Role1)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetUserRoleIdAsync(long userId)
    {
        var roleId = await _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.RoleId)
            .FirstOrDefaultAsync();

        return roleId.HasValue ? (int?)roleId.Value : null;
    }

    public async Task CreateEventAsync(long userId, int roleId, string title, string description, string startTime, string? endTime, string meetingUrl)
    {
        var start = DateTime.Parse(startTime);
        DateTime? end = null;
        if (!string.IsNullOrEmpty(endTime))
            end = DateTime.Parse(endTime);

        var ev = new Event
        {
            CreatedBy = userId,
            RoleId = roleId,
            Title = title,
            Description = description,
            StartTime = start,
            EndTime = end,
            MeetingUrl = meetingUrl
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEventAsync(int eventId, long userId)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId && e.CreatedBy == userId);
        if (ev == null)
            return;

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
    }

    public async Task ManageRequestAsync(int requestId, string newStatus)
    {
        var req = await _context.EventRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);
        if (req == null)
            return;

        req.Status = newStatus;
        await _context.SaveChangesAsync();
    }

    public async Task RequestToJoinAsync(int eventId, long userId)
    {
        var exists = await _context.EventRequests.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (exists)
            return;

        var req = new EventRequest
        {
            EventId = eventId,
            UserId = userId,
            Status = "pending",
            RequestedAt = DateTime.Now
        };
        _context.EventRequests.Add(req);
        await _context.SaveChangesAsync();
    }
}

