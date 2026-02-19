using ASIB.Core.Interfaces;
using ASIB.Core.Services;
using ASIB.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthentication()
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IBulkInsertService, BulkInsertService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMyNetworkService, MyNetworkService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();
builder.Services.AddScoped<IViewPostService, ViewPostService>();
builder.Services.AddScoped<IUploadStorageService, UploadStorageService>();

builder.Services.AddDbContext<AsibContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 23))
    )
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

    var isStatic = path.StartsWith("/css")
        || path.StartsWith("/js")
        || path.StartsWith("/lib")
        || path.StartsWith("/images")
        || path.StartsWith("/image")
        || path.StartsWith("/favicon")
        || path.StartsWith("/_framework")
        || path.StartsWith("/wwwroot");

    if (isStatic)
    {
        await next();
        return;
    }

    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";

    var isLogin = path == "/" || path == "/login" || path == "/auth/login";
    var isRegister = path == "/register" || path == "/auth/register";
    var isChangePassword = path == "/auth/change-password" || path == "/change-password";

    var adminId = context.Session.GetString("admin_id");
    var userId = context.Session.GetString("user_id");
    var isLoggedIn = string.Equals(context.Session.GetString("is_logged_in"), "true", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrEmpty(adminId)
        || !string.IsNullOrEmpty(userId);
    var role = (context.Session.GetString("role") ?? string.Empty).Trim().ToLowerInvariant();
    var isAdmin = !string.IsNullOrEmpty(adminId) || role == "admin";
    var mustChangePassword = string.Equals(context.Session.GetString("must_change_password"), "true", StringComparison.OrdinalIgnoreCase);

    if (!isLoggedIn)
    {
        if (!isLogin && !isRegister && path != "/auth/register-success" && path != "/register-success" && path != "/auth/forgot-password" && path != "/forgot-password")
        {
            context.Response.Redirect("/Auth/Login");
            return;
        }
    }
    else
    {
        if (mustChangePassword && !isChangePassword && path != "/logout" && path != "/auth/logout")
        {
            context.Response.Redirect("/Auth/ChangePassword");
            return;
        }

        if (isLogin || isRegister)
        {
            if (isAdmin)
            {
                context.Response.Redirect("/Dashboard/AdminDashboard");
            }
            else
            {
                context.Response.Redirect("/Dashboard/UserDashboard");
            }
            return;
        }

        if (isAdmin)
        {
            var isUserPage = path.StartsWith("/dashboard/studentdashboard")
                || path.StartsWith("/dashboard/alumnidashboard")
                || path.StartsWith("/dashboard/facultydashboard")
                || path.StartsWith("/dashboard/userdashboard")
                || path.StartsWith("/dashboard/event")
                || path.StartsWith("/dashboard/message")
                || path.StartsWith("/dashboard/mynetwork")
                || path.StartsWith("/dashboard/notifications")
                || path.StartsWith("/dashboard/viewpost")
                || path.StartsWith("/dashboard/userprofile")
                || path.StartsWith("/event")
                || path.StartsWith("/message")
                || path.StartsWith("/mynetwork")
                || path.StartsWith("/notifications")
                || path.StartsWith("/viewpost")
                || path.StartsWith("/userprofile");

            if (isUserPage)
            {
                context.Response.Redirect("/Dashboard/AdminDashboard");
                return;
            }
        }
        else
        {
            var isAdminPage = path.StartsWith("/dashboard/admindashboard");
            if (isAdminPage)
            {
                context.Response.Redirect("/Dashboard/UserDashboard");
                return;
            }
        }
    }

    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
