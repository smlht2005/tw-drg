using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,釘住 ComboXicd 分派器代表性 COMBO_NO(粗錯偵測;完整 DRG 一致性待 oracle)。
public class ComboXicdTests
{
    private static string[] Cm(params string[] v)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < v.Length ? v[i] : "";
        return a;
    }

    private static MdcDrgXicd Row(string icd, string treeDrg, string combo, string item)
        => new(treeDrg, "05", 0, 0, null, combo, null, item, "X", null, null, null, null, null, null, null, "N", icd, null);

    private static GroupingContext Ctx(int days = 40, string ccMark = "N") => new()
    {
        CmCodes = Cm("PDX"),
        OpCodes = new string[20],
        Sex = "M",
        PartMark = "001",
        OutDate = "2026/01/10",
        Days = days,
        CcMark = ccMark,
    };

    private static ComboXicd Make(MdcDrgXicd[] cands, string[] cm)
        => new(new ComboCounter(cands, cm), Ctx());

    [Fact]
    public void Combo1_dispatches_to_AX_A()
    {
        var x = Make([Row("PDX", "05010", "1", "A")], Cm("PDX"));
        x.Check("05010", "1").Should().Be(0);       // 主診斷命中 A → 成立
    }

    [Fact]
    public void Combo1_no_match_returns_negative()
    {
        var x = Make([Row("OTHER", "05010", "1", "A")], Cm("PDX"));
        x.Check("05010", "1").Should().NotBe(0);     // 主診斷未命中 → 不成立
    }

    [Fact]
    public void Combo12_dispatches_to_CX_C()
    {
        var x = Make([Row("PDX", "05010", "12", "C")], Cm("PDX"));
        x.Check("05010", "12").Should().Be(0);
    }

    [Fact]
    public void Combo73_uses_days_threshold()
    {
        new ComboXicd(new ComboCounter([], Cm("PDX")), Ctx(days: 10)).Check("X", "73").Should().NotBe(0);  // <28
        new ComboXicd(new ComboCounter([], Cm("PDX")), Ctx(days: 30)).Check("X", "73").Should().Be(0);     // >=28
    }

    [Fact]
    public void Unknown_combo_is_not_matched()
    {
        var x = Make([], Cm("PDX"));
        x.Check("05010", "2").Should().Be(9);        // 缺號 → 預設不成立
    }

    [Fact]
    public void Maps_tree_drg_passthrough_by_default()
    {
        var x = Make([Row("PDX", "05010", "1", "A")], Cm("PDX"));
        x.Check("05010", "1");
        x.MappedTreeDrg.Should().Be("05010");        // 非 case 5 → 原樣
    }
}
