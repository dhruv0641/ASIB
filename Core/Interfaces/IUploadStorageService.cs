using Microsoft.AspNetCore.Http;

namespace ASIB.Core.Interfaces;

public interface IUploadStorageService
{
    Task<UploadSaveResult> SaveProfilePictureAsync(long userId, IFormFile file);
    Task<UploadSaveResult> SaveCoverPhotoAsync(long userId, IFormFile file);
    Task<UploadSaveResult> SavePostImageAsync(long userId, long postId, IFormFile file, int index);
    Task<UploadSaveResult> SavePostAttachmentAsync(long userId, long postId, IFormFile file, int index);
    Task<UploadSaveResult> SaveTempAsync(IFormFile file);
}

public sealed class UploadSaveResult
{
    public bool Success { get; init; }
    public string? RelativePath { get; init; }
    public string? Error { get; init; }
}
