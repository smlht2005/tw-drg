using Drg.Core.Configuration;
using Drg.Core.Engine;
using Drg.Core.Io;
using Drg.Core.Models;
using Drg.Core.Ruleset;

namespace Drg.Core;

/// <summary>
/// 批次協調:讀 → 分組 → 寫。以 <see cref="BatchJob.BatchId"/> 隔離每次執行(對應 legacy USER_GUID),
/// 記錄起訖時間(FR-012)。逐筆獨立(FR-016 single)、全筆輸出零丟棄(FR-007)。
/// </summary>
public sealed class BatchCoder(IClaimReader reader, IDrgGrouper grouper, IResultWriter writer)
{
    public BatchJob Run(string inputPath, string outputPath, GroupingRuleset ruleset, DrgOptions options)
    {
        var job = new BatchJob
        {
            InputRef = inputPath,
            OutputRef = outputPath,
            RulesetVersion = ruleset.Version,   // FR-013 可追溯
            StartedAt = DateTimeOffset.Now,
            Status = BatchStatus.Running,
        };

        var results = new List<CodingResult>();
        foreach (var claim in reader.Read(inputPath))
        {
            if (job.TotalCount >= options.BatchMax)
                throw new BatchTooLargeException(options.BatchMax);   // SC-004

            var result = grouper.Group(claim, ruleset);  // 逐筆獨立;驗證/錯誤標註於 US2
            results.Add(result);
            job.TotalCount++;
            if (string.IsNullOrEmpty(result.ErrDesc)) job.CodedCount++;
            else job.ErrorCount++;
        }

        writer.Write(outputPath, results, ruleset.Version);   // 零丟棄:全筆輸出;每列標註版本(FR-013)
        job.EndedAt = DateTimeOffset.Now;
        job.Status = BatchStatus.Completed;
        return job;
    }
}

/// <summary>單批筆數超過 <c>DRG_BATCH_MAX</c> 上限(SC-004)。</summary>
public sealed class BatchTooLargeException(int max)
    : Exception($"批次筆數超過上限 {max}(可由環境變數 DRG_BATCH_MAX 調整)");
