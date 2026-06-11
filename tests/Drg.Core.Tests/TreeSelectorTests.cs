using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 tree_yyy:DRG 等價重映射 + 權重決選(MDC15 取最小 TREE_NO)。
public class TreeSelectorTests
{
    private static GroupingContext Ctx(string mdc = "05", int medAmt = 0) => new()
    {
        CmCodes = new string[20],
        OpCodes = new string[20],
        Sex = "M",
        PartMark = "001",
        OutDate = "2026/01/10",
        Mdc = mdc,
        MedAmt = medAmt,
    };

    private static GroupingRuleset Rs(params MdcDrgWgt[] w) => new() { Version = "test", MdcDrgWgt = w };

    private static MdcDrgWgt W(string drg, int no, float wgt, int avgExp = 0, string mdc = "05")
        => new(mdc, drg, no, wgt, avgExp, null, null);

    [Fact]
    public void Picks_highest_weight()
    {
        var rs = Rs(W("A", 1, 2.0f), W("B", 2, 3.0f));
        TreeSelector.Select(["A", "B"], Ctx(), rs).Should().Be("B");
    }

    [Fact]
    public void Tie_weight_breaks_on_lowest_tree_no()
    {
        var rs = Rs(W("A", 5, 2.0f), W("B", 3, 2.0f));
        TreeSelector.Select(["A", "B"], Ctx(), rs).Should().Be("B");
    }

    [Fact]
    public void Mdc15_picks_lowest_tree_no_ignoring_weight()
    {
        var rs = Rs(W("A", 10, 9.0f, mdc: "15"), W("B", 2, 1.0f, mdc: "15"));
        TreeSelector.Select(["A", "B"], Ctx(mdc: "15"), rs).Should().Be("B");
    }

    [Fact]
    public void Zero_weight_row_uses_medamt_over_avgexp()
    {
        // A:零權重 → 500/100 = 5.0 > B 的 3.0
        var rs = Rs(W("A", 1, 0f, avgExp: 100), W("B", 2, 3.0f));
        TreeSelector.Select(["A", "B"], Ctx(medAmt: 500), rs).Should().Be("A");
    }

    [Fact]
    public void Remap_collapses_equivalent_drgs()
    {
        // 候選含 10402 → 10404 被改寫為 10402;即使 10404 權重較高也被抑制
        var rs = Rs(W("10402", 1, 1.0f), W("10404", 2, 9.0f));
        TreeSelector.Select(["10402", "10404"], Ctx(), rs).Should().Be("10402");
    }

    [Fact]
    public void No_matching_weight_row_yields_empty()
    {
        TreeSelector.Select(["ZZZ"], Ctx(), Rs(W("A", 1, 1.0f))).Should().BeEmpty();
    }
}
