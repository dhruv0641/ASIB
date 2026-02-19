using ASIB.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class MyNetworkController : Controller
{
    private readonly IMyNetworkService _myNetworkService;

    public MyNetworkController(IMyNetworkService myNetworkService)
    {
        _myNetworkService = myNetworkService;
    }

    [HttpGet("/Dashboard/MyNetwork")]
    [HttpGet("my_network")]
    
    public async Task<IActionResult> Index()
    {
        if (!IsUserLoggedIn())
            return Redirect("/Auth/Login");

        if (IsAdminLoggedIn())
            return Redirect("/Dashboard/AdminDashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var currentUserId))
            return Redirect("/Auth/Login");

        var model = await _myNetworkService.BuildMyNetworkPageModelAsync(currentUserId);
        if (model == null)
            return Redirect("/Auth/Logout");

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

