using System.Text;
using System.Text.Json;
using Drg.Core.Engine;
using Drg.Core.Models;
using Drg.Core.Ruleset;
using Drg.Data;
using FluentAssertions;
using Xunit;

namespace Drg.Parity.Tests;

/// <summary>對 legacy-oracle 語料(tools/LegacyOracle 由真實 rddi1000_main 產生)驗證已建模組:
/// Icd10CmCheck → EccCheck → MdcCheck 串成「至 MDC 為止」的部分管線,比對真實 MDC / CC_MARK。
/// DRG / combo 尚未實作,故此處不比對 DRG。需 icd10.sqlite(Phase A 遷移)與語料,缺則略過。</summary>
public sealed class OracleParityTests
{
    private sealed record OracleCase(
        string Name, string[] Cm, string[] Op, string Sex, string Birthday, string InDate,
        string OutDate, string PartCode, string TranCode, int MedAmt, int Age,
        string Drg, string Mdc, string CcMark, string Err);

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

    private static string Slash(string yyyyMMdd) =>
        $"{yyyyMMdd.Substring(0, 4)}/{yyyyMMdd.Substring(4, 2)}/{yyyyMMdd.Substring(6, 2)}";

    private static string[] Pad(string[] codes)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < codes.Length ? codes[i].Trim() : "";
        return a;
    }

    [SkippableFact]
    public void Pipeline_to_mdc_matches_legacy_oracle()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "GoldenCorpus", "legacy_oracle.json");
        var dbPath = FindUp("icd10.sqlite");
        Skip.If(!File.Exists(corpusPath), "legacy_oracle.json 不存在;先以 tools/LegacyOracle 產生語料");
        Skip.If(dbPath is null, "icd10.sqlite 不存在;先執行 Phase A 遷移");

        var cases = JsonSerializer.Deserialize<List<OracleCase>>(
            File.ReadAllText(corpusPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var rs = new RulesetRepository(new DbConnectionFactory(DbProvider.Sqlite, $"Data Source={dbPath}"))
            .Load("Tw-DRG 115/01/01 (v3.4.20)");

        var failures = new StringBuilder();
        foreach (var c in cases)
        {
            var now = c.InDate == "00000000" ? Slash(c.Birthday) : Slash(c.InDate);
            var baseDate = Slash(c.Birthday);   // part_code != 903

            var ctx = new GroupingContext
            {
                CmCodes = Pad(c.Cm),
                OpCodes = Pad(c.Op),
                Sex = c.Sex,
                PartMark = c.PartCode,
                OutDate = Slash(c.OutDate),
                TranCode = c.TranCode,
                MedAmt = c.MedAmt,
                Ages = AgeCalculator.Years(now, baseDate),
                Months = AgeCalculator.Months(now, baseDate),
                Days = AgeCalculator.Days(now, baseDate),
            };

            var total = Icd10CmCheck.Run(ctx, rs);
            string actualMdc, actualCc;
            if (total == 0)
            {
                EccCheck.Run(ctx, rs);
                MdcCheck.Run(ctx, rs);
                actualMdc = ctx.Mdc;
                actualCc = ctx.CcMark;
            }
            else
            {
                actualMdc = "";          // 硬性 CM 檢核失敗 → legacy 不產 MDC
                actualCc = c.CcMark;     // 此情形不比對 CC
            }

            if (actualMdc != c.Mdc || actualCc != c.CcMark)
                failures.AppendLine(
                    $"{c.Name} cm={string.Join(",", c.Cm)}: MDC 期望 '{c.Mdc}' 得 '{actualMdc}';" +
                    $" CC 期望 '{c.CcMark}' 得 '{actualCc}'(err1={ctx.ErrCode[0]}, total={total})");
        }

        failures.ToString().Should().BeEmpty($"與 legacy oracle 應一致,但有不符:\n{failures}");
    }

    [SkippableFact]
    public void Full_grouping_matches_legacy_drg()
    {
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "GoldenCorpus", "legacy_oracle.json");
        var dbPath = FindUp("icd10.sqlite");
        Skip.If(!File.Exists(corpusPath), "legacy_oracle.json 不存在;先以 tools/LegacyOracle 產生語料");
        Skip.If(dbPath is null, "icd10.sqlite 不存在;先執行 Phase A 遷移");

        var cases = JsonSerializer.Deserialize<List<OracleCase>>(
            File.ReadAllText(corpusPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var factory = new DbConnectionFactory(DbProvider.Sqlite, $"Data Source={dbPath}");
        var rs = new RulesetRepository(factory).Load("Tw-DRG 115/01/01 (v3.4.20)");
        var grouper = new DrgGrouper(new CandidateRepository(factory));

        var failures = new StringBuilder();
        foreach (var c in cases)
        {
            var claim = new ClaimEncounter
            {
                CmCodes = Pad(c.Cm),
                OpCodes = Pad(c.Op),
                Sex = c.Sex,
                Birthday = c.Birthday,
                InDate = c.InDate,
                OutDate = c.OutDate,
                PartCode = c.PartCode,
                TranCode = c.TranCode,
                MedAmt = c.MedAmt,
            };

            var r = grouper.Group(claim, rs);
            if (r.Drg != c.Drg || r.Mdc != c.Mdc || r.CcMark != c.CcMark)
                failures.AppendLine(
                    $"{c.Name} cm={string.Join(",", c.Cm)}: DRG 期望 '{c.Drg}' 得 '{r.Drg}';" +
                    $" MDC 期望 '{c.Mdc}' 得 '{r.Mdc}';CC 期望 '{c.CcMark}' 得 '{r.CcMark}'");
        }

        failures.ToString().Should().BeEmpty($"DRGGrouper 應與 legacy oracle 一致,但有不符:\n{failures}");
    }
}
