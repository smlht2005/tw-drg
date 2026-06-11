using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 Op_Code_Rtn_yyy_B8_2:'+' 組合手術碼以相異槽位全數命中才展開。
public class OpCodeExpanderTests
{
    // 40 槽 ICDOP_TAB,前段填入現有手術碼。
    private static string[] Tab(params string[] ops)
    {
        var a = new string[40];
        for (var i = 0; i < 40; i++) a[i] = i < ops.Length ? ops[i] : "";
        return a;
    }

    private static GroupingRuleset Rs(params string[] plusCombos) => new()
    {
        Version = "test",
        Xicd = plusCombos.Select(c => new Xicd("2", c, null, null, null, null, null)).ToArray(),
    };

    [Fact]
    public void Expands_when_all_components_present()
    {
        var tab = Tab("A", "B", "C");
        OpCodeExpander.Expand(tab, Rs("A+B")).Should().BeTrue();
        tab[20].Should().Be("A+B");
    }

    [Fact]
    public void No_expand_when_a_component_missing()
    {
        var tab = Tab("A", "C");
        OpCodeExpander.Expand(tab, Rs("A+B")).Should().BeFalse();
        tab[20].Should().BeEmpty();
    }

    [Fact]
    public void No_expand_when_fewer_present_than_components()
    {
        // present(1) < components(2) → 直接略過
        var tab = Tab("A");
        OpCodeExpander.Expand(tab, Rs("A+B")).Should().BeFalse();
    }

    [Fact]
    public void Duplicate_component_needs_distinct_slots()
    {
        OpCodeExpander.Expand(Tab("A", "X"), Rs("A+A")).Should().BeFalse();   // 只有一個 A
        var two = Tab("A", "A");
        OpCodeExpander.Expand(two, Rs("A+A")).Should().BeTrue();              // 兩個相異槽位
        two[20].Should().Be("A+A");
    }

    [Fact]
    public void Multiple_combos_fill_sequential_expansion_slots()
    {
        var tab = Tab("A", "B", "C", "D");
        OpCodeExpander.Expand(tab, Rs("A+B", "C+D")).Should().BeTrue();
        tab[20].Should().Be("A+B");
        tab[21].Should().Be("C+D");
    }

    [Fact]
    public void Non_plus_rows_ignored()
    {
        var tab = Tab("A", "B");
        var rs = new GroupingRuleset
        {
            Version = "test",
            Xicd = [new Xicd("2", "A", null, null, null, null, null)],   // 無 '+'
        };
        OpCodeExpander.Expand(tab, rs).Should().BeFalse();
    }

    [Fact]
    public void Three_component_combo()
    {
        var tab = Tab("A", "B", "C");
        OpCodeExpander.Expand(tab, Rs("A+B+C")).Should().BeTrue();
        tab[20].Should().Be("A+B+C");
    }
}
