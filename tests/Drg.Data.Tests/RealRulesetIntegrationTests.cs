using Drg.Core.Engine;
using Drg.Core.Ruleset;
using Drg.Data;
using FluentAssertions;
using Xunit;

namespace Drg.Data.Tests;

/// <summary>對由 icd10.sdf 遷移而來的真實 icd10.sqlite 跑整合測試(驗證欄位/NULL 對映與 LINQ 港口)。
/// 找不到 icd10.sqlite 時整類略過(衍生資料未納版控)。以 scripts/export_sdf.ps1 + tools/Drg.Migrate 產生。</summary>
public sealed class RealRulesetIntegrationTests
{
    private static string? FindDb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "icd10.sqlite");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    public static bool DbMissing => FindDb() is null;

    private static GroupingRuleset LoadRuleset()
    {
        var db = FindDb()!;
        var factory = new DbConnectionFactory(DbProvider.Sqlite, $"Data Source={db}");
        return new RulesetRepository(factory).Load("Tw-DRG 115/01/01 (v3.4.20)");
    }

    [SkippableFact]
    public void Ruleset_loads_real_reference_data()
    {
        Skip.If(DbMissing, "icd10.sqlite 不存在;先執行 scripts/export_sdf.ps1 + tools/Drg.Migrate");
        var rs = LoadRuleset();

        rs.Xicd.Should().HaveCount(222162);
        rs.PdxMdc.Should().HaveCount(72239);
        rs.MdcDrgWgt.Should().HaveCount(1068);
        rs.Ecc.Should().HaveCount(18129);
        rs.EccGroup.Should().HaveCount(509382);

        // NULL 對映:XICD 多數列的 SEX_NO 為 NULL(非空字串)
        rs.Xicd.Any(x => x.SexNo is null).Should().BeTrue();
    }

    [SkippableFact]
    public void MdcCheck_assigns_real_mdc_for_a_known_principal()
    {
        Skip.If(DbMissing, "icd10.sqlite 不存在");
        var rs = LoadRuleset();

        // 取一個單純映射到 MDC 01–23、且該主診斷不另外映射到 24/25 的真實 ICD 碼
        var by = rs.PdxMdc.ToLookup(p => p.IcdNo.Trim());
        var pick = rs.PdxMdc.First(p =>
            int.TryParse(p.MdcCode, out var m) && m is >= 1 and <= 23
            && by[p.IcdNo.Trim()].All(q => q.MdcCode is not ("24" or "25"))
            && !p.IcdNo.Contains('+'));

        var cm = new string[20];
        for (var i = 0; i < 20; i++) cm[i] = "";
        cm[0] = pick.IcdNo.Trim();

        var ctx = new GroupingContext
        {
            CmCodes = cm,
            OpCodes = new string[20],
            Sex = "M",
            PartMark = "001",
            OutDate = "2026/01/10",
        };
        MdcCheck.Run(ctx, rs);

        ctx.Mdc.Should().Be(pick.MdcCode.Trim());
    }
}
