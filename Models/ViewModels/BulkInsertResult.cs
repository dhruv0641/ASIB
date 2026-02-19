namespace ASIB.Models.ViewModels;

public class BulkInsertResult
{
    public int CreatedCount { get; set; }
    public int SkippedDuplicateCount { get; set; }
    public int EmailFailedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
