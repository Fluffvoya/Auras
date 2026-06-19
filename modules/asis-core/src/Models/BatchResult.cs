namespace ASIS.Core.Models;

public class BatchResult
{
    public int TotalCount => Items.Count;
    public int SuccessCount => Items.Count(i => i.IsSuccess);
    public int FailureCount => Items.Count(i => !i.IsSuccess);
    public List<BatchItemResult> Items { get; set; } = new();
}

public class BatchItemResult
{
    public Guid FileId { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
}
