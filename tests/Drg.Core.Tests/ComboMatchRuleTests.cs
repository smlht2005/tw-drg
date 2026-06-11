using Drg.Core.Engine;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例,鎖定 rddi0001 combo_AX/BX/CX 的「回傳決策」段(item_type × CNT/num → 成立與否)。
public class ComboMatchRuleTests
{
    [Theory]
    [InlineData("A", 1, true)]
    [InlineData("A", 0, false)]
    [InlineData("A2", 3, true)]
    [InlineData("A3", 0, true)]    // A3/A4 反向:CNT==0 才成立
    [InlineData("A4", 2, false)]
    [InlineData("AX", 1, false)]   // 未知 → 不成立
    public void MatchA(string item, int cnt, bool expected)
        => ComboMatchRule.MatchA(item, cnt).Should().Be(expected);

    [Theory]
    [InlineData("B", 1, 0, true)]
    [InlineData("B", 0, 0, false)]
    [InlineData("B3", 0, 0, true)]      // B3/B7:CNT==0
    [InlineData("B7", 2, 0, false)]
    [InlineData("B6", 2, 2, true)]      // B6:CNT>0 且 num==CNT
    [InlineData("B6", 2, 1, false)]
    [InlineData("B8", 1, 0, true)]      // B8:CNT>0 且 num==0
    [InlineData("B8", 1, 1, false)]
    [InlineData("B13", 3, 1, true)]     // B13:0<num<CNT
    [InlineData("B13", 3, 0, false)]
    [InlineData("B13", 3, 3, false)]
    public void MatchB(string item, int cnt, int num, bool expected)
        => ComboMatchRule.MatchB(item, cnt, num).Should().Be(expected);

    [Theory]
    [InlineData("C", 1, true)]
    [InlineData("C1", 0, false)]
    [InlineData("C2", 2, true)]        // C2:CNT>1
    [InlineData("C2", 1, false)]
    [InlineData("C3", 0, true)]        // C3:CNT==0
    [InlineData("C3", 1, false)]
    public void MatchC(string item, int cnt, bool expected)
        => ComboMatchRule.MatchC(item, cnt).Should().Be(expected);
}
