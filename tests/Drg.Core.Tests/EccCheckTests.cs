using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 ecc_chk_yyy / get_ecc_yyy:MCC → T → CC 三段優先序與「同群排除」。
public class EccCheckTests
{
    private static string[] Cm(params string[] vals)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < vals.Length ? vals[i] : "";
        return a;
    }

    private static GroupingContext Ctx(params string[] cm) => new()
    {
        CmCodes = Cm(cm),
        OpCodes = Cm(),
        Sex = "M",
        PartMark = "001",
        OutDate = "2026/01/10",
    };

    private static GroupingRuleset Rs(Ecc[]? ecc = null, EccGroup[]? group = null) => new()
    {
        Version = "test",
        Ecc = ecc ?? [],
        EccGroup = group ?? [],
    };

    [Fact]
    public void No_ecc_data_marks_N()
    {
        var ctx = Ctx("A001");
        EccCheck.Run(ctx, Rs());
        ctx.CcMark.Should().Be("N");
        ctx.CcCode.Should().OnlyContain(c => c == '0');
    }

    [Fact]
    public void Principal_in_mcc_group_marks_M()
    {
        var ctx = Ctx("MCCDX");
        var rs = Rs(group: [new EccGroup("MCCDX", "MCC")]);
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("M");
        ctx.CcCode[0].Should().Be('1');
    }

    [Fact]
    public void Secondary_type2_wildcard_marks_M_on_secondary()
    {
        var ctx = Ctx("PDX", "SEC");
        var rs = Rs(ecc: [new Ecc("2", "SEC", "9999")]);   // 9999 = 不限主診斷
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("M");
        ctx.CcCode[1].Should().Be('1');
        ctx.CcCode[0].Should().Be('0');
    }

    [Fact]
    public void Same_group_exclusion_yields_N()
    {
        // 次診斷屬群 G1,主診斷也在 G1 → 排除,不計 CC
        var ctx = Ctx("PDX", "SEC");
        var rs = Rs(
            ecc: [new Ecc("2", "SEC", "G1")],
            group: [new EccGroup("PDX", "G1")]);
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("N");
    }

    [Fact]
    public void Type3_secondary_marks_T()
    {
        var ctx = Ctx("PDX3", "SECT");
        var rs = Rs(ecc: [new Ecc("3", "SECT", "9999")]);
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("T");
        ctx.CcCode[1].Should().Be('1');
    }

    [Fact]
    public void Type1_secondary_marks_Y()
    {
        var ctx = Ctx("PDX1", "SECC");
        var rs = Rs(ecc: [new Ecc("1", "SECC", "9999")]);
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("Y");
        ctx.CcCode[1].Should().Be('1');
    }

    [Fact]
    public void Mcc_beats_cc_in_priority()
    {
        // 同筆同時命中 MCC(主診斷)與 CC(次診斷 type1)→ 結果取 MCC
        var ctx = Ctx("BOTH", "SECC");
        var rs = Rs(
            ecc: [new Ecc("1", "SECC", "9999")],
            group: [new EccGroup("BOTH", "MCC")]);
        EccCheck.Run(ctx, rs);
        ctx.CcMark.Should().Be("M");
    }
}
