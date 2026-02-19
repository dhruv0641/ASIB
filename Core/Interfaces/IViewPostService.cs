using ASIB.Models.ViewModels;

namespace ASIB.Core.Interfaces;

public interface IViewPostService
{
    Task<ViewPostPageViewModel?> BuildViewPostPageModelAsync(long userId, int postId);
}
