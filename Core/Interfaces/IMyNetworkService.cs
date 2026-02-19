using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface IMyNetworkService
{
    Task<MyNetworkPageViewModel?> BuildMyNetworkPageModelAsync(long currentUserId);
}
