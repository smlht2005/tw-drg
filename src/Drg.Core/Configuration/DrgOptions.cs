namespace Drg.Core.Configuration;

/// <summary>執行期設定。機敏一律經環境變數注入,不內嵌於碼/設定(憲章原則 III)。</summary>
public sealed class DrgOptions
{
    public const string DbConnectionEnv = "DRG_DB_CONNECTION";
    public const string BatchMaxEnv = "DRG_BATCH_MAX";
    public const int DefaultBatchMax = 10;                              // SC-004,可由環境變數覆寫
    public const string RulesetVersion = "Tw-DRG 115/01/01 (v3.4.20)"; // FR-015

    public string? DbConnectionString { get; init; }
    public int BatchMax { get; init; } = DefaultBatchMax;

    public static DrgOptions FromEnvironment()
    {
        var conn = Environment.GetEnvironmentVariable(DbConnectionEnv);
        var maxRaw = Environment.GetEnvironmentVariable(BatchMaxEnv);
        var max = int.TryParse(maxRaw, out var m) && m > 0 ? m : DefaultBatchMax;
        return new DrgOptions { DbConnectionString = conn, BatchMax = max };
    }
}
