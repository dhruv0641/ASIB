using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface INotificationsService
{
    Task<NotificationsPageViewModel?> BuildNotificationsPageModelAsync(long userId);
    Task MarkAllAsReadAsync(long userId);
}
