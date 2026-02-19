using ASIB.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace ASIB.Core.Interfaces;

public interface IBulkInsertService
{
    Task<BulkInsertResult> ProcessBulkInsertAsync(long adminId, IFormFile file, string loginUrl, string roleName, int? batchYear);
}
