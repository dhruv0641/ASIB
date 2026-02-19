using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface IMessageService
{
    Task<MessagePageViewModel?> BuildMessagePageModelAsync(long userId);
    Task<bool> IsUserVerifiedAsync(long userId);
}
