using ASIB.Core.Interfaces;
using ASIB.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ASIB.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// GET: Display login form
    /// </summary>
    [HttpGet("")]
    [HttpGet("auth/login")]
    [HttpGet("/login")]
    
    public async Task<IActionResult> Login()
    {
        // Redirect if already logged in
        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard", "Dashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (long.TryParse(userIdStr, out var userId))
        {
            var mustChange = await _authService.IsPasswordChangeRequiredAsync(userId);
            if (mustChange)
                return RedirectToAction("ChangePassword");

            var roleName = await _authService.GetRoleNameByUserIdAsync(userId);
            return RedirectToRoleDashboard(roleName);
        }

        // Get remember me cookies
        var emailCookie = Request.Cookies["login_email"] ?? "";
        ViewBag.EmailCookie = emailCookie;
        ViewBag.RememberMe = !string.IsNullOrEmpty(emailCookie);

        return View("Login");
    }

    /// <summary>
    /// POST: Process login
    /// </summary>
    [HttpPost("auth/login")]
    [HttpPost("/login")]
    
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return View("Login");

        // Try admin login first
        var adminResponse = await _authService.ValidateAdminAsync(request.Email, request.Password);
        if (adminResponse.Success)
        {
            var admin = await _authService.GetAdminByEmailAsync(request.Email);
            if (admin == null)
            {
                ViewBag.Error = "❌ No account found with this email.";
                ViewBag.ErrorType = "error";
                ViewBag.EmailCookie = request.Email;
                ViewBag.RememberMe = request.RememberMe;
                return View("Login");
            }

            HttpContext.Session.SetString("admin_id", admin.AdminId.ToString());
            HttpContext.Session.SetString("admin_email", admin.Email);
            HttpContext.Session.SetString("primary_role", "Admin");
            HttpContext.Session.SetString("sub_role", "Admin");
            HttpContext.Session.SetString("is_logged_in", "true");
            HttpContext.Session.SetString("role", "Admin");
            HttpContext.Session.SetString("sub_role", "Admin");
            HttpContext.Session.SetString("auth_user_id", admin.AdminId.ToString());

            HandleRememberMe(request);
            return RedirectToAction("AdminDashboard", "Dashboard");
        }

        // Try user login
        var userResponse = await _authService.ValidateUserAsync(request.Email, request.Password);
        
        if (userResponse.Success)
        {
            // Get user ID and store in session
            var user = await _authService.GetUserByEmailAsync(request.Email);
            if (user != null)
            {
                HttpContext.Session.SetString("user_id", user.UserId.ToString());
                HttpContext.Session.SetString("user_email", request.Email);
                HttpContext.Session.SetString("primary_role", "User");
                HttpContext.Session.SetString("is_logged_in", "true");
                HttpContext.Session.SetString("role", "User");
                HttpContext.Session.SetString("auth_user_id", user.UserId.ToString());

                HandleRememberMe(request);
                var roleName = await _authService.GetRoleNameByUserIdAsync(user.UserId);
                HttpContext.Session.SetString("sub_role", roleName ?? "");

                var mustChange = await _authService.IsPasswordChangeRequiredAsync(user.UserId);
                if (mustChange)
                {
                    HttpContext.Session.SetString("must_change_password", "true");
                    return RedirectToAction("ChangePassword");
                }

                return RedirectToRoleDashboard(roleName);
            }
        }

        // Login failed
        ViewBag.Error = userResponse.Message;
        ViewBag.ErrorType = userResponse.ErrorType;
        ViewBag.EmailCookie = request.Email;
        ViewBag.RememberMe = request.RememberMe;

        return View("Login");
    }

    /// <summary>
    /// GET: Display registration form
    /// </summary>
    [HttpGet("auth/register")]
    [HttpGet("/register")]
    public async Task<IActionResult> Register()
    {
        // Redirect if already logged in
        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard", "Dashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (long.TryParse(userIdStr, out var userId))
        {
            var roleName = await _authService.GetRoleNameByUserIdAsync(userId);
            return RedirectToRoleDashboard(roleName);
        }

        // Get roles from database
        var roles = await _authService.GetRolesAsync();
        ViewBag.Roles = roles;

        return View("Register");
    }

    /// <summary>
    /// POST: Process registration
    /// </summary>
    [HttpPost("auth/register")]
    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Redirect if already logged in
        if (HttpContext.Session.GetString("admin_id") != null)
            return RedirectToAction("AdminDashboard", "Dashboard");

        var userIdStr = HttpContext.Session.GetString("user_id");
        if (long.TryParse(userIdStr, out var userId))
        {
            var roleName = await _authService.GetRoleNameByUserIdAsync(userId);
            return RedirectToRoleDashboard(roleName);
        }

        var response = await _authService.CreateUserAsync(request);

        if (response.Success)
        {
            return RedirectToAction("RegisterSuccess");
        }

        // Return form with errors
        ViewBag.Errors = response.Errors;
        ViewBag.FormData = request;

        var roles = await _authService.GetRolesAsync();
        ViewBag.Roles = roles;

        return View("Register");
    }

    /// <summary>
    /// GET: Registration success page
    /// </summary>
    [HttpGet("auth/register-success")]
    [HttpGet("/register-success")]
    public IActionResult RegisterSuccess()
    {
        if (HttpContext.Session.GetString("admin_id") != null || HttpContext.Session.GetString("user_id") != null)
            return RedirectToAction("Login");

        return View("RegisterSuccess");
    }

    /// <summary>
    /// GET: Forgot password form
    /// </summary>
    [HttpGet("auth/forgot-password")]
    [HttpGet("/forgot-password")]
    public IActionResult ForgotPassword()
    {
        if (HttpContext.Session.GetString("admin_id") != null || HttpContext.Session.GetString("user_id") != null)
            return RedirectToAction("Login");

        return View("ForgotPassword");
    }

    /// <summary>
    /// POST: Logout
    /// </summary>
    [HttpPost("auth/logout")]
    [HttpGet("auth/logout")]
    [HttpPost("/logout")]
    [HttpGet("/logout")]

    public async Task<IActionResult> Logout()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        var userIdStr = HttpContext.Session.GetString("user_id");
        
        if (long.TryParse(userIdStr, out long userId))
        {
            // Update user status offline
            await _authService.LogoutUserAsync(userId);
        }

        // Clear session
        HttpContext.Session.Clear();

        // Clear cookies
        Response.Cookies.Delete("login_email");
        Response.Cookies.Delete("login_pass");

        return RedirectToAction("Login");
    }

    [HttpGet("auth/change-password")]
    [HttpGet("/change-password")]
    
    public IActionResult ChangePassword()
    {
        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out _))
            return RedirectToAction("Login");

        var mustChange = HttpContext.Session.GetString("must_change_password");
        if (!string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Login");

        return View("ChangePassword");
    }

    [HttpPost("auth/change-password")]
    [HttpPost("/change-password")]
    
    public async Task<IActionResult> ChangePasswordPost()
    {
        var userIdStr = HttpContext.Session.GetString("user_id");
        if (!long.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login");

        var mustChange = HttpContext.Session.GetString("must_change_password");
        if (!string.Equals(mustChange, "true", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Login");

        var password = Request.Form["password"].ToString();
        var confirm = Request.Form["password_confirm"].ToString();

        if (password.Length < 6)
        {
            ViewBag.Errors = new List<string> { "Password must be at least 6 characters." };
            return View("ChangePassword");
        }

        if (password != confirm)
        {
            ViewBag.Errors = new List<string> { "Passwords do not match." };
            return View("ChangePassword");
        }

        var changed = await _authService.ChangePasswordAsync(userId, password);
        if (!changed)
        {
            ViewBag.Errors = new List<string> { "Password update failed." };
            return View("ChangePassword");
        }

        HttpContext.Session.SetString("must_change_password", "false");
        var roleName = await _authService.GetRoleNameByUserIdAsync(userId);
        return RedirectToRoleDashboard(roleName);
    }

    /// <summary>
    /// Handle Remember Me checkbox
    /// </summary>
    private void HandleRememberMe(LoginRequest request)
    {
        if (request.RememberMe)
        {
            Response.Cookies.Append("login_email", request.Email, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
            Response.Cookies.Append("login_pass", request.Password, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
        }
        else
        {
            Response.Cookies.Delete("login_email");
            Response.Cookies.Delete("login_pass");
        }
    }

    private IActionResult RedirectToRoleDashboard(string? roleName)
    {
        var role = (roleName ?? "").Trim().ToLowerInvariant();

        return role switch
        {
            "student" => RedirectToAction("StudentDashboard", "Dashboard"),
            "alumni" => RedirectToAction("AlumniDashboard", "Dashboard"),
            "faculty" => RedirectToAction("FacultyDashboard", "Dashboard"),
            _ => RedirectToAction("Login")
        };
    }
}

