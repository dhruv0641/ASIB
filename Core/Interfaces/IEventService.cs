using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface IEventService
{
    Task<EventPageViewModel?> BuildEventPageModelAsync(long userId, string? view);
    Task<string?> GetUserRoleAsync(long userId);
    Task<int?> GetUserRoleIdAsync(long userId);
    Task CreateEventAsync(long userId, int roleId, string title, string description, string startTime, string? endTime, string meetingUrl);
    Task DeleteEventAsync(int eventId, long userId);
    Task ManageRequestAsync(int requestId, string newStatus);
    Task RequestToJoinAsync(int eventId, long userId);
}
