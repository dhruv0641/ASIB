using ASIB.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class ViewPostController : Controller
{
    private readonly IViewPostService _viewPostService;

    public ViewPostController(IViewPostService viewPostService)
    {
        _viewPostService = viewPostService;
    }

    [HttpGet("/Dashboard/ViewPost")]
    [HttpGet("view_post")]
    
    public async Task<IActionResult> Index([FromQuery] int id = 0)
    {
        if (!IsUserLoggedIn())
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return Redirect("/Auth/Login");

        if (id <= 0)
            return Content("Invalid post ID.");

        var model = await _viewPostService.BuildViewPostPageModelAsync(userId, id);
        if (model == null)
            return Redirect("/Auth/Logout");

        if (model.Post == null)
            return Content("Post not found.");

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
}

