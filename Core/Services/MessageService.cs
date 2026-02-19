using ASIB.Core.Interfaces;
using ASIB.Models;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Core.Services;

public class MessageService : IMessageService
{
    private readonly AsibContext _context;

    public MessageService(AsibContext context)
    {
        _context = context;
    }

    public async Task<MessagePageViewModel?> BuildMessagePageModelAsync(long userId)
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
                u.VerificationStatus,
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

        var roleText = userData.Role ?? "";
        var profileHeadline = !string.IsNullOrWhiteSpace(profile?.Headline)
            ? profile!.Headline!
            : CapitalizeFirstLetter(roleText);

        return new MessagePageViewModel
        {
            LoggedInUserId = userData.UserId,
            UserName = $"{userData.FirstName} {userData.LastName}",
            ProfilePhoto = profilePhoto,
            ProfileHeadline = profileHeadline,
            CurrentPage = "Message"
        };
    }

    public async Task<bool> IsUserVerifiedAsync(long userId)
    {
        var status = await _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.VerificationStatus)
            .FirstOrDefaultAsync();

        return status == 1;
    }

    private static string CapitalizeFirstLetter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        if (trimmed.Length == 1)
            return trimmed.ToUpperInvariant();

        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }
}

