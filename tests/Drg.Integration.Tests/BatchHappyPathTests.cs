using System.Text;
using Drg.Core;
using Drg.Core.Configuration;
using Drg.Core.Engine;
using Drg.Core.Io;
using Drg.Data;
using FluentAssertions;
using Xunit;

namespace Drg.Integration.Tests;

/// <summary>T028:讀 → 分組 → 寫 端到端 happy path,用真實 ClaimCsvReader + DrgGrouper + ResultWriter
/// 對全合法輸入跑完整管線。需 icd10.sqlite(Phase A 遷移),缺則略過。
/// 分組正確性由 Parity 測試保證;此處驗證「串接 + 全筆輸出 + 版本標註」。</summary>
public sealed class BatchHappyPathTests
{
    private const string RulesetName = "Tw-DRG 115/01/01 (v3.4.20)";

    private static string? FindUp(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var c = Path.Combine(dir.FullName, fileName);
            if (File.Exists(c)) return c;
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>組一列 legacy 固定欄位 CSV(0..54):主診斷 col7、主手術 col12、其餘留空。</summary>
    private static string Row(string pid, string sex, string cm, string inDate, string birthday, string outDate, string part)
    {
        var f = new string[55];
        for (var i = 0; i < 55; i++) f[i] = "";
        f[0] = "HOSP01"; f[1] = "11501"; f[2] = pid; f[3] = "0001"; f[4] = sex;
        f[5] = inDate; f[6] = birthday;
        f[7] = cm;                       // 主診斷
        f[17] = "1"; f[18] = outDate; f[19] = "100000"; f[20] = part;
        return string.Join(",", f);
    }

    [SkippableFact]
    public void Read_group_write_emits_all_rows_with_drg_and_version()
    {
        var dbPath = FindUp("icd10.sqlite");
        Skip.If(dbPath is null, "icd10.sqlite 不存在;先執行 Phase A 遷移");

        var inPath = Path.GetTempFileName();
        var outPath = Path.GetTempFileName();
        File.WriteAllLines(inPath, new[]
        {
            Row("A100000001", "M", "A0221", "20260105", "19800101", "20260110", "001"),  // 期望 MDC01 / DRG 02004
            Row("A100000002", "M", "A1850", "20260105", "19800101", "20260110", "001"),  // 期望 MDC02 / DRG 047
        });

        try
        {
            var factory = new DbConnectionFactory(DbProvider.Sqlite, $"Data Source={dbPath}");
            var ruleset = new RulesetRepository(factory).Load(RulesetName);
            var coder = new BatchCoder(
                new ClaimCsvReader(),
                new DrgGrouper(new CandidateRepository(factory)),
                new ResultWriter());

            var job = coder.Run(inPath, outPath, ruleset, new DrgOptions { BatchMax = 1000 });

            job.TotalCount.Should().Be(2);
            job.CodedCount.Should().Be(2);          // 兩筆皆合法
            job.ErrorCount.Should().Be(0);
            job.RulesetVersion.Should().Be(RulesetName);   // FR-013

            var lines = File.ReadAllLines(outPath, Encoding.UTF8);
            lines.Should().HaveCount(3);            // 表頭 + 2 筆(零丟棄,FR-007)
            lines[0].Should().Contain("DRG").And.Contain("規則版本");
            lines[1].Should().Contain("02004").And.Contain(RulesetName);
            lines[2].Should().Contain("047").And.Contain(RulesetName);
        }
        finally { File.Delete(inPath); File.Delete(outPath); }
    }
}
