using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> BuildDashboardAsync();
    Task<AdminDashboardViewModel> BuildVerificationAsync();
    Task<AdminDashboardViewModel> BuildSuspendAsync();
    Task<AdminDashboardViewModel> BuildAllUsersListAsync(string filter);
    Task<AdminDashboardViewModel?> BuildAllUsersDetailAsync(long userId);
    Task<AdminDashboardViewModel> BuildEventsListAsync();
    Task<AdminDashboardViewModel?> BuildEventDetailsAsync(long eventId);
    Task<AdminDashboardViewModel> BuildAnnouncementsListAsync();
    Task<AdminDashboardViewModel?> BuildAnnouncementEditAsync(long announcementId, bool isAddView);
    Task<AdminDashboardViewModel> BuildAdminLogAsync(string? startDate, string? endDate);
    Task<AdminDashboardViewModel> BuildPromotionAsync();
    Task<AdminDashboardViewModel> BuildAddUserAsync();
    Task<(string Flash, string FlashType)> BlockUserAsync(long adminId, long userId, string? reason);
    Task<(string Flash, string FlashType)> UnblockUserAsync(long adminId, long userId);
    Task<(string Flash, string FlashType)> DeleteEventAsync(long adminId, long eventId);
    Task<(string Flash, string FlashType)> AddAnnouncementAsync(long adminId, string title, string content, bool isActive);
    Task<(string Flash, string FlashType)> EditAnnouncementAsync(long adminId, long announcementId, string title, string content, bool isActive);
    Task<(string Flash, string FlashType)> DeleteAnnouncementAsync(long adminId, long announcementId);
    Task<(string Flash, string FlashType)> PromoteSingleAsync(long adminId, long userId);
    Task<(string Flash, string FlashType)> PromoteBulkAsync(long adminId, IEnumerable<long> userIds);
    Task<(string Flash, string FlashType)> DemoteSingleAsync(long adminId, long userId);
    Task<(string Flash, string FlashType)> DemoteBulkAsync(long adminId, IEnumerable<long> userIds);
    Task<(string Flash, string FlashType)> AddUserAsync(long adminId, string firstName, string middleName, string lastName, string email, int batchYear, long roleId);
}
