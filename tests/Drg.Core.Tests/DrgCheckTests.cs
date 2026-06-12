using Drg.Core.Engine;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 鎖定 rddi0001 drg_chk_yyy 的兩種 chk_type 與 beg/end 範圍語意。
public class DrgCheckTests
{
    private static string[] Drg(params string[] v)
    {
        var a = new string[5];
        for (var i = 0; i < 5; i++) a[i] = i < v.Length ? v[i] : "";
        return a;
    }

    // 主編排用法:chk_type=1 + target="" → 區段內出現任一非空即回 2(代表已產出 DRG)。
    [Fact]
    public void ChkType1_returns_2_when_any_differs_from_target()
        => DrgCheck.Check(Drg("A", ""), "", 0, 2, 1).Should().Be(2);

    [Fact]
    public void ChkType1_returns_0_when_all_equal_target()
        => DrgCheck.Check(Drg("", ""), "", 0, 2, 1).Should().Be(0);

    [Fact]
    public void ChkType0_returns_2_on_first_equal()
        => DrgCheck.Check(Drg("X", "Y"), "X", 0, 2, 0).Should().Be(2);

    [Fact]
    public void ChkType0_returns_0_when_none_equal()
        => DrgCheck.Check(Drg("A", "B"), "X", 0, 2, 0).Should().Be(0);

    [Fact]
    public void Range_beg_end_is_honored()
        => DrgCheck.Check(Drg("A", "", "C"), "", 1, 2, 1).Should().Be(0);   // 只看 index 1(空)
}
