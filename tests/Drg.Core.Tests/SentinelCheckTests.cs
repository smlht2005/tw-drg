using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 rddi1000_main 哨兵早退區塊(673–725)+ ophhh_chk_yyy。
public class SentinelCheckTests
{
    private static string[] Arr(params string[] vals)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < vals.Length ? vals[i] : "";
        return a;
    }

    private static GroupingContext Ctx(string[]? cm = null, string[]? op = null, string mdc = "05",
        string outDate = "2026/01/10") => new()
    {
        CmCodes = cm ?? Arr(),
        OpCodes = op ?? Arr(),
        Sex = "M",
        PartMark = "001",
        OutDate = outDate,
        Mdc = mdc,
    };

    private static GroupingRuleset Rs(params Xicd[] xicd) => new()
    {
        Version = "test",
        Xicd = xicd,
    };

    [Theory]
    [InlineData("19")]
    [InlineData("20")]
    public void Mdc19_or_20_returns_XXX(string mdc)
        => SentinelCheck.Run(Ctx(mdc: mdc), Rs()).Should().Be("XXX");

    [Fact]
    public void Type1_chk1_hits_principal_returns_YYY()
    {
        var ctx = Ctx(cm: Arr("PDX", "SEC"));
        var rs = Rs(new Xicd("1", "PDX", null, null, "1", null, null));
        SentinelCheck.Run(ctx, rs).Should().Be("YYY");
    }

    [Fact]
    public void Yyy_only_matches_principal_not_secondary()
    {
        // PRM_ICD_CHK=1 規則只命中 CM[0];掛在次診斷不應觸發 YYY
        var ctx = Ctx(cm: Arr("PDX", "SEC"));
        var rs = Rs(new Xicd("1", "SEC", null, null, "1", null, null));
        SentinelCheck.Run(ctx, rs).Should().BeNull();
    }

    [Theory]
    [InlineData("3")]
    [InlineData("4")]
    public void Type1_prm_chk_3_or_4_returns_ZZZ(string prm)
    {
        var ctx = Ctx(cm: Arr("PDX", "HIT"));
        var rs = Rs(new Xicd("1", "HIT", null, null, prm, null, null));
        SentinelCheck.Run(ctx, rs).Should().Be("ZZZ");
    }

    [Fact]
    public void Type1_chk6_returns_WWW()
    {
        var ctx = Ctx(cm: Arr("PDX", "HIT"));
        var rs = Rs(new Xicd("1", "HIT", null, null, "6", null, null));
        SentinelCheck.Run(ctx, rs).Should().Be("WWW");
    }

    [Fact]
    public void Type1_chk8_returns_GGG()
    {
        var ctx = Ctx(cm: Arr("PDX", "HIT"));
        var rs = Rs(new Xicd("1", "HIT", null, null, "8", null, null));
        SentinelCheck.Run(ctx, rs).Should().Be("GGG");
    }

    [Fact]
    public void Sentinel_priority_yyy_before_zzz()
    {
        // 同一 CM 同時命中 chk1(YYY)與 chk3(ZZZ),依序 YYY 先返回
        var ctx = Ctx(cm: Arr("PDX"));
        var rs = Rs(
            new Xicd("1", "PDX", null, null, "1", null, null),
            new Xicd("1", "PDX", null, null, "3", null, null));
        SentinelCheck.Run(ctx, rs).Should().Be("YYY");
    }

    [Fact]
    public void No_xicd_hit_returns_null()
        => SentinelCheck.Run(Ctx(cm: Arr("PDX")), Rs()).Should().BeNull();

    // ophhh_chk_yyy:出院日 ≥ 2020/07/01 → num2(Inner4)+ num3(Inner5)同時命中即 HHH。
    [Fact]
    public void OpHhh_after_cutover_inner4_and_inner5_returns_HHH()
    {
        var ctx = Ctx(op: Arr("0USG0ZZ", "0JQC0ZZ"), outDate: "2021/03/01");
        SentinelCheck.Run(ctx, Rs()).Should().Be("HHH");
    }

    [Fact]
    public void OpHhh_after_cutover_missing_inner5_no_HHH()
    {
        var ctx = Ctx(op: Arr("0USG0ZZ"), outDate: "2021/03/01");
        SentinelCheck.Run(ctx, Rs()).Should().BeNull();
    }

    // 出院日 < 2020/07/01:Inner4+Inner5 還需 Inner(或 Inner2)湊 num=1 才成立。
    [Fact]
    public void OpHhh_before_cutover_requires_inner_too()
    {
        var withoutInner = Ctx(op: Arr("0USG0ZZ", "0JQC0ZZ"), outDate: "2020/01/01");
        SentinelCheck.Run(withoutInner, Rs()).Should().BeNull();

        var withInner = Ctx(op: Arr("0UT90ZZ", "0USG0ZZ", "0JQC0ZZ"), outDate: "2020/01/01");
        SentinelCheck.Run(withInner, Rs()).Should().Be("HHH");
    }

    // num3=0 退路:Inner7 補 num3,再要求 Inner3 + Inner6 同時命中。
    [Fact]
    public void OpHhh_inner7_path_requires_inner3_and_inner6()
    {
        var ctx = Ctx(op: Arr("0U7G0DZ", "0US90ZZ", "0ULF7DZ"), outDate: "2021/03/01");
        SentinelCheck.Run(ctx, Rs()).Should().Be("HHH");
    }
}
