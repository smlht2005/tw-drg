using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,釘住 combo_drg_yyy 等價:marks(借 CandidateFilter)+ opflag ⑥ + MDC08 特例 + 串 ComboXicd +
// dedup/排序。完整 DRG 一致性待 oracle(含 OP 外科案)。
public class ComboDrgTests
{
    // CcMark='X'/AgeMark='N'/LiveMark=null → marks 預設全過;mdcNo/dep/plus 視測試覆寫。
    private static MdcDrgXicd Row(string treeDrg, string combo, string item, string icd,
        string mdcNo = "05", string? dep = null, string? plus = null)
        => new(treeDrg, mdcNo, 0, 0, dep, combo, null, item, "X",
            null, null, null, null, null, null, null, "N", icd, plus);

    private static GroupingContext Ctx(int opFlag, int days = 40, string depFlag = "", string filterMdc = "05") => new()
    {
        CmCodes = new string[20],
        OpCodes = new string[20],
        Sex = "M",
        PartMark = "001",
        OutDate = "2026/01/10",
        Days = days,
        OpFlag = opFlag,
        DepFlag = depFlag,
        FilterMdc = filterMdc,
    };

    private static GroupingRuleset Rs(params PdxMdc[] pdx) => new() { Version = "t", PdxMdc = pdx };

    // ── opflag=2(診斷導向)⑥ ──

    [Fact]
    public void OpFlag2_A_in_cm_is_collected()
    {
        var got = new ComboDrg().Generate([Row("05010", "1", "A", "PDX")], ["PDX"], [], Ctx(2), Rs());
        got.Should().Equal("05010");
    }

    [Fact]
    public void OpFlag2_A_not_in_cm_is_excluded()
    {
        new ComboDrg().Generate([Row("05010", "1", "A", "OTHER")], ["PDX"], [], Ctx(2), Rs())
            .Should().BeEmpty();
    }

    [Fact]
    public void OpFlag2_A3_in_cm_is_excluded()   // A3 反向:命中 CM 反而出局
    {
        new ComboDrg().Generate([Row("05010", "1", "A3", "PDX")], ["PDX"], [], Ctx(2), Rs())
            .Should().BeEmpty();
    }

    // ── opflag=1(手術導向)⑥ ──

    [Fact]
    public void OpFlag1_B_in_op_is_collected()
    {
        var got = new ComboDrg().Generate([Row("05010", "5", "B", "0SRC0JZ")], [], ["0SRC0JZ"], Ctx(1), Rs());
        got.Should().Equal("05010");
    }

    [Fact]
    public void OpFlag1_B3_in_op_is_excluded()   // B3 反向:命中手術碼反而出局
    {
        new ComboDrg().Generate([Row("05010", "5", "B3", "0SRC0JZ")], [], ["0SRC0JZ"], Ctx(1), Rs())
            .Should().BeEmpty();
    }

    [Fact]
    public void OpFlag1_B5_is_unconditional_and_does_not_throw_on_empty_op()
    {
        var got = new ComboDrg().Generate([Row("05010", "73", "B5", "x")], ["PDX"], [], Ctx(1), Rs());
        got.Should().Equal("05010");   // B5 短路 → 不 throw;combo73 days>=28 → ComboXicd 0
    }

    [Fact]
    public void OpFlag1_throws_when_no_op_codes()
    {
        var act = () => new ComboDrg().Generate([Row("05010", "5", "B", "0SRC0JZ")], [], [], Ctx(1), Rs());
        act.Should().Throw<InvalidOperationException>();
    }

    // ── MDC08 手術/非手術組特例 ──

    [Fact]
    public void Mdc08_nonOp_excluded_when_no_matching_pdx()
    {
        new ComboDrg().Generate([Row("21002", "73", "A", "x", mdcNo: "08")], ["PDX"], [], Ctx(2, filterMdc: "08"), Rs())
            .Should().BeEmpty();
    }

    [Fact]
    public void Mdc08_nonOp_collected_when_pdx_op_not_Y()
    {
        var rs = Rs(new PdxMdc("PDX", "08", null, "N"));
        new ComboDrg().Generate([Row("21002", "73", "A", "x", mdcNo: "08")], ["PDX"], [], Ctx(2, filterMdc: "08"), rs)
            .Should().Equal("21002");
    }

    // ── dedup / 排序 / marks 整合 ──

    [Fact]
    public void Distinct_candidates_sorted_tree_drg_desc()
    {
        MdcDrgXicd[] cands =
        [
            Row("05010", "73", "A", "x"),
            Row("05020", "73", "A", "x"),
            Row("05010", "73", "A", "x"),   // 重複 → 收斂
        ];
        new ComboDrg().Generate(cands, ["PDX"], [], Ctx(2), Rs())
            .Should().Equal("05020", "05010");
    }

    [Fact]
    public void Dep_mismatch_is_excluded_by_marks()
    {
        new ComboDrg().Generate([Row("05010", "73", "A", "x", dep: "P")], ["PDX"], [], Ctx(2, depFlag: "M", filterMdc: "05"), Rs())
            .Should().BeEmpty();
    }
}
