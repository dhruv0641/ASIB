using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (string.Equals(HttpContext.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase))
        {
            var role = (HttpContext.Session.GetString("role") ?? string.Empty).Trim().ToLowerInvariant();
            if (role == "admin" || HttpContext.Session.GetString("admin_id") != null)
            {
                return RedirectToAction("AdminDashboard", "Dashboard");
            }

            return RedirectToAction("UserDashboard", "Dashboard");
        }

        return RedirectToAction("Login", "Auth");
    }
}
