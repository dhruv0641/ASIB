using ASIB.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace ASIB.Core.Services;

public class UploadStorageService : IUploadStorageService
{
    private static readonly HashSet<string> ProfileImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mov",
        ".avi"
    };

    private readonly IWebHostEnvironment _env;

    public UploadStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<UploadSaveResult> SaveProfilePictureAsync(long userId, IFormFile file)
    {
        return await SaveSingleImageAsync("uploads/profile_pictures", userId, "profile", file);
    }

    public async Task<UploadSaveResult> SaveCoverPhotoAsync(long userId, IFormFile file)
    {
        return await SaveSingleImageAsync("uploads/cover_photos", userId, "cover", file);
    }

    public async Task<UploadSaveResult> SavePostImageAsync(long userId, long postId, IFormFile file, int index)
    {
        return await SaveIndexedFileAsync("uploads/post_images", userId, postId, "image", file, allowAnyType: false);
    }

    public async Task<UploadSaveResult> SavePostAttachmentAsync(long userId, long postId, IFormFile file, int index)
    {
        return await SaveIndexedFileAsync("uploads/post_attachments", userId, postId, "file", file, allowAnyType: true);
    }

    public async Task<UploadSaveResult> SaveTempAsync(IFormFile file)
    {
        var baseFolder = Path.Combine(_env.WebRootPath, "uploads", "temp", "uploads");
        Directory.CreateDirectory(baseFolder);
        var ext = NormalizeExtension(Path.GetExtension(file.FileName));
        var name = $"temp_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(baseFolder, name);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return new UploadSaveResult
        {
            Success = true,
            RelativePath = "/" + Path.Combine("uploads", "temp", "uploads", name).Replace("\\", "/")
        };
    }

    private async Task<UploadSaveResult> SaveSingleImageAsync(string rootFolder, long userId, string baseName, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return new UploadSaveResult { Success = false, Error = "No file uploaded." };

        if (!ProfileImageTypes.Contains(file.ContentType ?? ""))
            return new UploadSaveResult { Success = false, Error = "Invalid file type. Please upload JPG, PNG, GIF, or WEBP." };

        var ext = NormalizeExtension(Path.GetExtension(file.FileName));
        if (!ImageExtensions.Contains(ext))
            return new UploadSaveResult { Success = false, Error = "Invalid file type. Please upload JPG, PNG, GIF, or WEBP." };

        var folder = Path.Combine(_env.WebRootPath, rootFolder, userId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);

        foreach (var existing in Directory.GetFiles(folder, $"{baseName}.*"))
        {
            File.Delete(existing);
        }

        var fileName = $"{baseName}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return new UploadSaveResult
        {
            Success = true,
            RelativePath = "/" + Path.Combine(rootFolder, userId.ToString(CultureInfo.InvariantCulture), fileName).Replace("\\", "/")
        };
    }

    private async Task<UploadSaveResult> SaveIndexedFileAsync(string rootFolder, long userId, long postId, string prefix, IFormFile file, bool allowAnyType)
    {
        if (file == null || file.Length == 0)
            return new UploadSaveResult { Success = false, Error = "No file uploaded." };

        var ext = NormalizeExtension(Path.GetExtension(file.FileName));
        if (!allowAnyType && !ImageExtensions.Contains(ext) && !VideoExtensions.Contains(ext))
            return new UploadSaveResult { Success = false, Error = "Invalid file type." };

        var folder = Path.Combine(_env.WebRootPath, rootFolder, userId.ToString(CultureInfo.InvariantCulture), postId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(folder);

        var index = GetNextIndex(folder, prefix, ext);
        var fileName = $"{prefix}_{index}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return new UploadSaveResult
        {
            Success = true,
            RelativePath = "/" + Path.Combine(rootFolder, userId.ToString(CultureInfo.InvariantCulture), postId.ToString(CultureInfo.InvariantCulture), fileName).Replace("\\", "/")
        };
    }

    private static int GetNextIndex(string folder, string prefix, string ext)
    {
        var index = 1;
        while (File.Exists(Path.Combine(folder, $"{prefix}_{index}{ext}")))
            index++;
        return index;
    }

    private static string NormalizeExtension(string? ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
            return "";
        if (!ext.StartsWith(".", StringComparison.Ordinal))
            return "." + ext;
        return ext.ToLowerInvariant();
    }
}
