namespace Drg.Core.Models;

public enum BatchStatus { Received, Running, Completed, Failed }

/// <summary>一次批次執行。<see cref="BatchId"/> 用於資料隔離(對應 legacy USER_GUID)。</summary>
public sealed class BatchJob
{
    public Guid BatchId { get; init; } = Guid.NewGuid();
    public string InputRef { get; init; } = "";
    public string OutputRef { get; init; } = "";
    public string RulesetVersion { get; set; } = "";   // FR-013 可追溯
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Received;
    public int TotalCount { get; set; }
    public int CodedCount { get; set; }
    public int ErrorCount { get; set; }
}
