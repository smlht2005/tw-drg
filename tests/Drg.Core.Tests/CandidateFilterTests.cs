using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 combo_drg_yyy RowFilter 的 marks 段:CC_MARK / AGE_MARK(7 組)/ LIVE_MARK / DEP。
public class CandidateFilterTests
{
    private static GroupingContext Ctx(int ages = 40, int days = 1000, string tran = "1",
        string ccMark = "N", string depFlag = "P", string filterMdc = "05") => new()
    {
        CmCodes = new string[20],
        OpCodes = new string[20],
        Sex = "M",
        PartMark = "001",
        OutDate = "2026/01/10",
        Ages = ages,
        Days = days,
        TranCode = tran,
        CcMark = ccMark,
        DepFlag = depFlag,
        FilterMdc = filterMdc,
    };

    private static MdcDrgXicd Row(string drg = "D", string mdcNo = "05", string? dep = "P", string? combo = null,
        string? live = null, string? item = null, string? cc = "X", string? ageMark = "N",
        string? a18 = null, string? a36 = null, string? a41 = null, string? a565 = null,
        string? a2y = null, string? a28d = null, string? a2d = null)
        => new(drg, mdcNo, 0, 0f, dep, combo, live, item, cc, a18, a36, a41, a565, a2y, a28d, a2d, ageMark, null, null);

    // ---- CC_MARK ----
    [Fact] public void Cc_X_always_passes() => CandidateFilter.MarksMatch(Row(cc: "X"), Ctx()).Should().BeTrue();

    [Fact]
    public void Cc_matches_record_found_flag()
    {
        // ctx.CcMark="Y" → cc1="Y";列 CC_MARK="Y" → 過
        CandidateFilter.MarksMatch(Row(cc: "Y"), Ctx(ccMark: "M")).Should().BeTrue();
        // 列 CC_MARK="N" 但本案有 CC(cc1="Y")→ 不符
        CandidateFilter.MarksMatch(Row(cc: "N"), Ctx(ccMark: "M")).Should().BeFalse();
    }

    [Fact]
    public void Cc_bypass_by_drg228_or_combo()
    {
        CandidateFilter.MarksMatch(Row(cc: "N", drg: "228", item: "B"), Ctx(ccMark: "M")).Should().BeTrue();
        CandidateFilter.MarksMatch(Row(cc: "N", combo: "63"), Ctx(ccMark: "M")).Should().BeTrue();
    }

    // ---- AGE_MARK ----
    [Fact] public void Age_mark_N_passes() => CandidateFilter.MarksMatch(Row(ageMark: "N"), Ctx(ages: 10)).Should().BeTrue();

    [Fact]
    public void Age_mark_Y_passes_when_one_criterion_holds()
    {
        // 成人(>=18)且 AGE_18Y='Y'
        CandidateFilter.MarksMatch(Row(ageMark: "Y", a18: "Y"), Ctx(ages: 20)).Should().BeTrue();
        // 20 歲不符 AGE_18Y='N',其餘條件皆空 → 不過
        CandidateFilter.MarksMatch(Row(ageMark: "Y", a18: "N"), Ctx(ages: 20)).Should().BeFalse();
    }

    [Fact]
    public void Age_2d_uses_days()
    {
        CandidateFilter.MarksMatch(Row(ageMark: "Y", a2d: "Y"), Ctx(days: 1)).Should().BeTrue();   // <=2 天
        CandidateFilter.MarksMatch(Row(ageMark: "Y", a2d: "Y"), Ctx(days: 30)).Should().BeFalse();
    }

    // ---- LIVE_MARK ----
    [Fact] public void Live_null_passes() => CandidateFilter.MarksMatch(Row(live: null), Ctx()).Should().BeTrue();

    [Theory]
    [InlineData("N", "4", true)]    // 死亡列 + 轉歸 4
    [InlineData("N", "1", false)]
    [InlineData("Y", "1", true)]    // 存活列 + 轉歸非 4
    [InlineData("Y", "4", false)]
    public void Live_mark_rules(string live, string tran, bool expected)
        => CandidateFilter.MarksMatch(Row(live: live), Ctx(tran: tran)).Should().Be(expected);

    // ---- DEP ----
    [Fact]
    public void Dep_must_match_flag()
    {
        CandidateFilter.MarksMatch(Row(dep: "P"), Ctx(depFlag: "P")).Should().BeTrue();
        CandidateFilter.MarksMatch(Row(dep: "P"), Ctx(depFlag: "M")).Should().BeFalse();
    }

    [Fact]
    public void Dep_ignored_for_mdc15() =>
        CandidateFilter.MarksMatch(Row(dep: "P"), Ctx(depFlag: "M", filterMdc: "15")).Should().BeTrue();

    [Fact]
    public void Mdc22_passes_on_combo_58_or_67_regardless_of_dep() =>
        CandidateFilter.MarksMatch(Row(dep: "P", combo: "58"), Ctx(depFlag: "M", filterMdc: "22")).Should().BeTrue();
}
