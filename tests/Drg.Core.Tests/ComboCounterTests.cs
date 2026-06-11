using Drg.Core.Engine;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 combo_AX / combo_CX 的計數(對候選列計 distinct ICD_CODE)+ ComboMatchRule。
// 注意 A3/A4/C3 為「反向」:候選列含該碼(CNT>0)反而不成立;CNT==0 才成立。
public class ComboCounterTests
{
    private static string[] Cm(params string[] v)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < v.Length ? v[i] : "";
        return a;
    }

    private static MdcDrgXicd Row(string icd, string treeDrg = "05010", string combo = "01", string item = "A", string? plus = null)
        => new(treeDrg, "05", 0, 0, null, combo, null, item, "X", null, null, null, null, null, null, null, "N", icd, plus);

    private static Xicd Op2(string icd, string? orNor = "Y", string? prm = null)
        => new("2", icd, null, null, prm, orNor, null);

    [Fact]
    public void ComboA_principal_match()
    {
        var cands = new[] { Row("PDX", item: "A"), Row("PDX", item: "A3") };
        var c = new ComboCounter(cands, Cm("PDX", "SEC"));
        c.ComboA("05010", "01", "A").Should().BeTrue();    // 主診斷在 A 列 → CNT>0
        c.ComboA("05010", "01", "A3").Should().BeFalse();  // 主診斷在 A3 列 → CNT>0 → A3(反向)不成立
    }

    [Fact]
    public void ComboA_no_principal_match()
    {
        var cands = new[] { Row("OTHER", item: "A"), Row("OTHER", item: "A3") };
        var c = new ComboCounter(cands, Cm("PDX"));
        c.ComboA("05010", "01", "A").Should().BeFalse();   // 主診斷未在 A 列 → CNT==0
        c.ComboA("05010", "01", "A3").Should().BeTrue();   // 主診斷未在 A3 列 → CNT==0 → A3 反向成立
    }

    [Fact]
    public void ComboA_secondary_for_A2_A4()
    {
        // A2/A4 比對「次診斷」(主診斷以外)
        var cands = new[] { Row("SEC1", item: "A2"), Row("SEC2", item: "A2"), Row("SEC1", item: "A4") };
        var c = new ComboCounter(cands, Cm("PDX", "SEC1", "SEC2"));
        c.ComboA("05010", "01", "A2").Should().BeTrue();   // 兩個次診斷命中 A2 → CNT=2>0
        c.ComboA("05010", "01", "A4").Should().BeFalse();  // 次診斷命中 A4 → CNT>0 → A4 反向不成立
    }

    [Fact]
    public void ComboC_counts_cm_matches()
    {
        var cands = new[] { Row("D1", item: "C"), Row("D2", item: "C") };
        var c = new ComboCounter(cands, Cm("D1", "D2"));
        c.ComboC("05010", "01", "C").Should().BeTrue();    // CNT=2>0
    }

    [Fact]
    public void ComboC2_needs_more_than_one()
    {
        var two = new[] { Row("D1", item: "C2"), Row("D2", item: "C2") };
        new ComboCounter(two, Cm("D1", "D2")).ComboC("05010", "01", "C2").Should().BeTrue();    // CNT=2>1

        var one = new[] { Row("D1", item: "C2") };
        new ComboCounter(one, Cm("D1")).ComboC("05010", "01", "C2").Should().BeFalse();         // CNT=1 不足
    }

    [Fact]
    public void ComboB5_counts_or_procedures_from_xicd()
    {
        var c = new ComboCounter([], Cm("PDX"), opCodes: Op("OP1"), xicd: [Op2("OP1")]);
        c.ComboB("05010", "01", "B5").Should().BeTrue();   // OR 手術命中 → CNT>0
        new ComboCounter([], Cm("PDX"), opCodes: Op("OPX"), xicd: [Op2("OP1")])
            .ComboB("05010", "01", "B5").Should().BeFalse();
    }

    [Fact]
    public void ComboB_else_matches_op_against_candidate_icd_and_plus()
    {
        var cands = new[] { Row("OP1", item: "B"), Row("ZZ", item: "B", plus: "A+B") };
        var c = new ComboCounter(cands, Cm("PDX"), opCodes: Op("OP1", "A+B"));
        c.ComboB("05010", "01", "B").Should().BeTrue();    // ICD_CODE 命中非+OP、ICD_CODE_PLUS 命中+OP
    }

    [Fact]
    public void ComboB3_is_reverse()
    {
        // B3:候選無命中(CNT==0)才成立
        var c = new ComboCounter([Row("OTHER", item: "B3")], Cm("PDX"), opCodes: Op("OP1"));
        c.ComboB("05010", "01", "B3").Should().BeTrue();
        new ComboCounter([Row("OP1", item: "B3")], Cm("PDX"), opCodes: Op("OP1"))
            .ComboB("05010", "01", "B3").Should().BeFalse();
    }

    [Fact]
    public void ComboB6_requires_num_equals_cnt()
    {
        // B6:每個 OR 手術 OP 都要在候選列(num==CNT)
        var cands = new[] { Row("OP1", item: "B6", plus: null) };
        var c = new ComboCounter(cands, Cm("PDX"), opCodes: Op("OP1"), xicd: [Op2("OP1")]);
        c.ComboB("05010", "01", "B6").Should().BeTrue();
    }

    private static string[] Op(params string[] v)
    {
        var a = new string[40];
        for (var i = 0; i < 40; i++) a[i] = i < v.Length ? v[i] : "";
        return a;
    }

    [Fact]
    public void Filters_by_tree_drg_and_combo_and_item()
    {
        var cands = new[]
        {
            Row("PDX", treeDrg: "05010", combo: "01", item: "A"),
            Row("PDX", treeDrg: "09999", combo: "01", item: "A"),   // 不同 TREE_DRG → 不計
        };
        var c = new ComboCounter(cands, Cm("PDX"));
        c.ComboA("05010", "01", "A").Should().BeTrue();
        c.ComboA("05010", "02", "A").Should().BeFalse();   // COMBO_NO 不符
    }
}
