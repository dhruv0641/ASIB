using ASIB.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class EventController : Controller
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet("/Dashboard/Event")]
    [HttpGet("event")]
    
    public async Task<IActionResult> Index([FromQuery] string? view)
    {
        if (!IsUserLoggedIn())
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return Redirect("/Auth/Login");

        var role = await _eventService.GetUserRoleAsync(userId);
        if (role == null)
            return Redirect("/Auth/Logout");

        var isFaculty = role.Trim().ToLowerInvariant() == "faculty";
        var isScheduleView = string.Equals(view, "schedule", StringComparison.OrdinalIgnoreCase);

        if (!isFaculty && !isScheduleView)
            return Redirect("/Dashboard/Event?view=schedule");

        var model = await _eventService.BuildEventPageModelAsync(userId, view);
        if (model == null)
            return Redirect("/Auth/Logout");

        return View("Index", model);
    }

    [HttpPost("/Dashboard/Event")]
    [HttpPost("event")]
    
    public async Task<IActionResult> IndexPost([FromQuery] string? view)
    {
        if (!IsUserLoggedIn())
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return Redirect("/Auth/Login");

        var role = await _eventService.GetUserRoleAsync(userId);
        if (role == null)
            return Redirect("/Auth/Logout");

        var isFaculty = role.Trim().ToLowerInvariant() == "faculty";
        var action = Request.Form["action"].ToString();

        if (isFaculty)
        {
            if (action == "create_event")
            {
                var title = Request.Form["title"].ToString();
                var description = Request.Form["description"].ToString();
                var startTime = Request.Form["start_time"].ToString();
                var endTime = Request.Form["end_time"].ToString();
                var meetingUrl = Request.Form["meeting_url"].ToString();

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(startTime))
                {
                    var roleId = await _eventService.GetUserRoleIdAsync(userId) ?? 0;
                    await _eventService.CreateEventAsync(userId, roleId, title, description, startTime, string.IsNullOrEmpty(endTime) ? null : endTime, meetingUrl);
                    return Redirect("/Dashboard/Event");
                }
            }

            if (action == "delete_event")
            {
                var eventIdStr = Request.Form["event_id"].ToString();
                if (int.TryParse(eventIdStr, out var eventId))
                {
                    await _eventService.DeleteEventAsync(eventId, userId);
                    return Redirect("/Dashboard/Event");
                }
            }

            if (action == "manage_request")
            {
                var requestIdStr = Request.Form["request_id"].ToString();
                var newStatus = Request.Form["new_status"].ToString();
                if ((newStatus == "approved" || newStatus == "declined") && int.TryParse(requestIdStr, out var requestId))
                {
                    await _eventService.ManageRequestAsync(requestId, newStatus);
                    return Redirect("/Dashboard/Event");
                }
            }
        }
        else
        {
            if (action == "request_to_join")
            {
                var eventIdStr = Request.Form["event_id"].ToString();
                if (int.TryParse(eventIdStr, out var eventId) && eventId > 0)
                {
                    await _eventService.RequestToJoinAsync(eventId, userId);
                    return Redirect("/Dashboard/Event?view=schedule");
                }
            }
        }

        return await Index(view);
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
}

