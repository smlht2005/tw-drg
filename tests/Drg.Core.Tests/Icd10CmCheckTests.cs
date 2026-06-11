using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例(非官方 corpus),鎖定 rddi0001 icd10cm_chk_yyy / errcode_chk_yyy 的關卡行為。
public class Icd10CmCheckTests
{
    private static string[] Slots(params string[] vals)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < vals.Length ? vals[i] : "";
        return a;
    }

    private static GroupingContext Ctx(string[]? cm = null, string[]? op = null,
        string sex = "M", string part = "001", int ages = 40, int months = 0, string outDate = "2026/01/10")
        => new()
        {
            CmCodes = cm ?? Slots(),
            OpCodes = op ?? Slots(),
            Sex = sex,
            PartMark = part,
            OutDate = outDate,
            Ages = ages,
            Months = months,
        };

    private static GroupingRuleset Rs(params Xicd[] xicd) => new() { Version = "test", Xicd = xicd };

    [Fact]
    public void Missing_primary_cm_yields_E_and_fails()
    {
        var ctx = Ctx(cm: Slots(""));   // 主診斷空
        var total = Icd10CmCheck.Run(ctx, Rs());

        ctx.ErrCode[0].Should().Be('E');
        total.Should().BeGreaterThan(0);   // 硬性失敗
    }

    [Fact]
    public void Unknown_cm_code_yields_Z()
    {
        var ctx = Ctx(cm: Slots("X999"));
        Icd10CmCheck.Run(ctx, Rs());      // XICD 無對應列
        ctx.ErrCode[0].Should().Be('Z');
    }

    [Fact]
    public void Male_only_diagnosis_on_female_yields_M()
    {
        var ctx = Ctx(cm: Slots("MALEDX"), sex: "F");
        var rs = Rs(new Xicd("1", "MALEDX", SexChk: "M", AgeChk: null, PrmIcdChk: null, OrNor: null, SexNo: null));
        Icd10CmCheck.Run(ctx, rs);
        ctx.ErrCode[0].Should().Be('M');
    }

    [Fact]
    public void Prm_A_yields_A_and_fails()
    {
        var ctx = Ctx(cm: Slots("PRMA"));
        var rs = Rs(new Xicd("1", "PRMA", SexChk: null, AgeChk: null, PrmIcdChk: "A", OrNor: null, SexNo: null));
        var total = Icd10CmCheck.Run(ctx, rs);
        ctx.ErrCode[0].Should().Be('A');
        total.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Empty_primary_op_with_secondary_present_yields_R()
    {
        // OP[0] 空、OP[5] 有碼 → icdop_chk 命中 → 'R'(寫入 ErrCode[20])
        var ctx = Ctx(
            cm: Slots("OK"),
            op: Slots("", "", "", "", "", "0210093"));
        var rs = Rs(new Xicd("1", "OK", null, null, " ", null, null));
        Icd10CmCheck.Run(ctx, rs);
        ctx.ErrCode[20].Should().Be('R');
    }

    [Fact]
    public void Valid_record_passes_with_zero_marks()
    {
        var ctx = Ctx(cm: Slots("OK"));   // 無 OP
        var rs = Rs(new Xicd("1", "OK", null, null, " ", null, null));
        var total = Icd10CmCheck.Run(ctx, rs);

        total.Should().Be(0);
        ctx.ErrCode[0].Should().Be('0');
        ctx.ErrCode[20].Should().Be('0');   // 主 OP 空且無次 OP → '0'
    }
}
