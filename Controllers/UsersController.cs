using ASIB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Controllers;

[Route("Users")]
public class UsersController : Controller
{
    private readonly AsibContext _context;

    public UsersController(AsibContext context)
    {
        _context = context;
    }

    [HttpGet("Search")]
    [HttpGet("/SearchUsers")]
    public async Task<IActionResult> Search([FromQuery(Name = "q")] string? query)
    {
        if (!TryGetUserId(out var currentUserId))
            return Unauthorized(new { error = "User not logged in. Please refresh." });

        var term = (query ?? string.Empty).Trim().ToLowerInvariant();
        if (term.Length < 1)
            return Json(Array.Empty<object>());

        var users = await (
            from u in _context.Users
            join p in _context.Profiles on u.UserId equals p.UserId into up
            from p in up.DefaultIfEmpty()
            where u.UserId != currentUserId
            let fullName = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower()
            where (u.FirstName ?? "").ToLower().Contains(term)
               || (u.LastName ?? "").ToLower().Contains(term)
               || fullName.Contains(term)
               || ((p != null ? p.Skills : "") ?? "").ToLower().Contains(term)
            orderby u.FirstName, u.LastName
            select new
            {
                user_id = u.UserId,
                first_name = u.FirstName,
                last_name = u.LastName,
                profile_photo_url = p != null ? p.ProfilePhotoUrl : null,
                headline = p != null ? p.Headline : null
            }
        ).Take(10).ToListAsync();

        var result = users.Select(u => new
        {
            u.user_id,
            u.first_name,
            u.last_name,
            profile_photo_url = string.IsNullOrWhiteSpace(u.profile_photo_url)
                ? $"https://placehold.co/40x40/6366f1/white?text={GetInitial(u.first_name)}"
                : u.profile_photo_url,
            full_name = $"{u.first_name} {u.last_name}".Trim(),
            headline = string.IsNullOrWhiteSpace(u.headline) ? "No headline" : u.headline
        });

        return Json(result);
    }

    private bool TryGetUserId(out long userId)
    {
        var userIdString = HttpContext.Session.GetString("user_id");
        return long.TryParse(userIdString, out userId) && userId > 0;
    }

    private static string GetInitial(string? firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return "U";
        return firstName.Trim().Substring(0, 1).ToUpperInvariant();
    }
}
