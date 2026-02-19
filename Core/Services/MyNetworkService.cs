using ASIB.Core.Interfaces;
using ASIB.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ASIB.Core.Services;

public class MyNetworkService : IMyNetworkService
{
    private readonly ASIB.Models.AsibContext _context;

    public MyNetworkService(ASIB.Models.AsibContext context)
    {
        _context = context;
    }

    public async Task<MyNetworkPageViewModel?> BuildMyNetworkPageModelAsync(long currentUserId)
    {
        var userData = await (
            from u in _context.Users
            join r in _context.Roles on u.RoleId equals r.RoleId into ur
            from r in ur.DefaultIfEmpty()
            join p in _context.Profiles on u.UserId equals p.UserId into up
            from p in up.DefaultIfEmpty()
            where u.UserId == currentUserId
            select new
            {
                u.FirstName,
                u.LastName,
                Role = r != null ? r.Role1 : "",
                Headline = p != null ? p.Headline : null,
                ProfilePhotoUrl = p != null ? p.ProfilePhotoUrl : null
            }
        ).FirstOrDefaultAsync();

        if (userData == null)
            return null;

        var userName = $"{userData.FirstName} {userData.LastName}";
        var profileHeadline = userData.Headline ?? CapitalizeFirstLetter(userData.Role);
        var firstLetter = !string.IsNullOrWhiteSpace(userData.FirstName)
            ? userData.FirstName.Trim().Substring(0, 1).ToUpperInvariant()
            : "U";
        var profilePhoto = !string.IsNullOrWhiteSpace(userData.ProfilePhotoUrl)
            ? userData.ProfilePhotoUrl!
            : $"https://placehold.co/100x100/6366f1/white?text={firstLetter}";

        var connectionsCount = await _context.Connections.CountAsync(c =>
            (c.RequesterId == currentUserId || c.AddresseeId == currentUserId) && c.Status == "accepted");

        var followingCount = await _context.Follows.CountAsync(f => f.FollowerId == currentUserId);
        var followersCount = await _context.Follows.CountAsync(f => f.FollowingId == currentUserId);
        var totalFollows = followingCount + followersCount;

        var eventsCount = await _context.Events.CountAsync(e => e.CreatedBy == currentUserId);
        var invitationCount = await _context.Connections.CountAsync(c => c.AddresseeId == currentUserId && c.Status == "pending");

        var suggestionsSql = @"
            SELECT u.user_id AS UserId, u.first_name AS FirstName, u.last_name AS LastName, 
                   p.headline AS Headline, p.profile_photo_url AS ProfilePhotoUrl, r.role AS Role
            FROM users u
            LEFT JOIN profiles p ON u.user_id = p.user_id
            LEFT JOIN role r ON u.role_id = r.role_id
            WHERE u.user_id != {0} 
              AND u.verification_status = 1
              AND NOT EXISTS (
                    SELECT 1 FROM connections c
                    WHERE (c.requester_id = {0} AND c.addressee_id = u.user_id)
                       OR (c.requester_id = u.user_id AND c.addressee_id = {0})
              )
            ORDER BY RAND() LIMIT 10";

        var suggestionRows = await _context.MyNetworkSuggestionRows
            .FromSqlRaw(suggestionsSql, currentUserId)
            .ToListAsync();

        var suggestions = new List<MyNetworkSuggestionItem>();
        foreach (var row in suggestionRows)
        {
            var suggFirstLetter = !string.IsNullOrWhiteSpace(row.FirstName)
                ? row.FirstName.Trim().Substring(0, 1).ToUpperInvariant()
                : "U";
            var displayPhoto = !string.IsNullOrWhiteSpace(row.ProfilePhotoUrl)
                ? row.ProfilePhotoUrl!
                : $"https://placehold.co/80x80/6366f1/white?text={suggFirstLetter}";
            var displayHeadline = row.Headline ?? CapitalizeFirstLetter(row.Role) ?? "New User";
            var displayName = $"{row.FirstName} {row.LastName}";

            suggestions.Add(new MyNetworkSuggestionItem
            {
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Headline = row.Headline,
                ProfilePhotoUrl = row.ProfilePhotoUrl,
                Role = row.Role,
                DisplayPhoto = displayPhoto,
                DisplayHeadline = displayHeadline,
                DisplayName = displayName
            });
        }

        return new MyNetworkPageViewModel
        {
            CurrentUserId = currentUserId,
            UserName = userName,
            ProfileHeadline = profileHeadline,
            ProfilePhoto = profilePhoto,
            ConnectionsCount = connectionsCount,
            FollowingCount = followingCount,
            FollowersCount = followersCount,
            TotalFollows = totalFollows,
            EventsCount = eventsCount,
            InvitationCount = invitationCount,
            Suggestions = suggestions,
            CurrentPage = "MyNetwork"
        };
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

