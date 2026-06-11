using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 mdc_chk_yyy:24 → 25 → 1–23 優先序與性別分流(B/T)。
public class MdcCheckTests
{
    private static string[] Cm(params string[] vals)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < vals.Length ? vals[i] : "";
        return a;
    }

    private static GroupingContext Ctx(string[] cm, string sex = "M", string sexArr = "") => new()
    {
        CmCodes = cm,
        OpCodes = Cm(),
        Sex = sex,
        PartMark = "001",
        OutDate = "2026/01/10",
        SexArr = sexArr,
    };

    private static GroupingRuleset Rs(PdxMdc[]? pdx = null, Xicd[]? xicd = null) => new()
    {
        Version = "test",
        PdxMdc = pdx ?? [],
        Xicd = xicd ?? [],
    };

    [Fact]
    public void Mdc24_requires_two_distinct_cc_groups()
    {
        var ctx = Ctx(Cm("TRAUMA", "SEC1"));
        var rs = Rs(pdx:
        [
            new PdxMdc("TRAUMA", "24", "C1", null),
            new PdxMdc("SEC1", "24", "C2", null),
        ]);
        MdcCheck.Run(ctx, rs);
        ctx.Mdc.Should().Be("24");
    }

    [Fact]
    public void Mdc24_fails_with_single_cc_group_falls_through()
    {
        var ctx = Ctx(Cm("TRAUMA", "SEC1"));
        var rs = Rs(pdx:
        [
            new PdxMdc("TRAUMA", "24", "C1", null),
            new PdxMdc("SEC1", "24", "C1", null),   // 同一 CC 群 → distinct < 2
            new PdxMdc("TRAUMA", "08", null, null), // 落到 1–23
        ]);
        MdcCheck.Run(ctx, rs);
        ctx.Mdc.Should().Be("08");
    }

    [Fact]
    public void Mdc25_when_principal_in_25_and_prm_chk_3()
    {
        var ctx = Ctx(Cm("HIV"));
        var rs = Rs(
            pdx: [new PdxMdc("HIV", "25", null, null)],
            xicd: [new Xicd("1", "HIV", null, null, "3", null, null)]);
        MdcCheck.Run(ctx, rs);
        ctx.Mdc.Should().Be("25");
    }

    [Theory]
    [InlineData("B", "M", "12")]
    [InlineData("B", "F", "13")]
    [InlineData("T", "M", "11")]
    [InlineData("T", "F", "13")]
    public void Sex_arr_routes_to_reproductive_mdc(string sexArr, string sex, string expected)
    {
        var ctx = Ctx(Cm("X"), sex: sex, sexArr: sexArr);
        MdcCheck.Run(ctx, Rs());
        ctx.Mdc.Should().Be(expected);
    }

    [Fact]
    public void Mdc1to23_by_principal_diagnosis()
    {
        var ctx = Ctx(Cm("DX1"));
        var rs = Rs(pdx: [new PdxMdc("DX1", "05", null, null)]);
        MdcCheck.Run(ctx, rs);
        ctx.Mdc.Should().Be("05");
    }

    [Fact]
    public void Mdc1to23_prefers_combined_principal_plus_secondary()
    {
        var ctx = Ctx(Cm("A", "B"));
        var rs = Rs(pdx:
        [
            new PdxMdc("A+B", "06", null, null),   // 組合碼優先
            new PdxMdc("A", "09", null, null),     // 主診斷單獨
        ]);
        MdcCheck.Run(ctx, rs);
        ctx.Mdc.Should().Be("06");
    }

    [Fact]
    public void No_match_yields_empty_mdc()
    {
        var ctx = Ctx(Cm("ZZZ"));
        MdcCheck.Run(ctx, Rs());
        ctx.Mdc.Should().BeEmpty();
    }
}
