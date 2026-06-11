using Drg.Core.Engine;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 合成案例(非官方 corpus),鎖定 legacy rddi0001 ages/months/days_cnt_yyy 的行為含其量化怪癖。
public class AgeCalculatorTests
{
    [Theory]
    [InlineData("2026/01/01", "1980/01/01", 46)]   // 整歲
    [InlineData("2026/06/11", "1980/06/12", 45)]   // 差一天未到生日 → 少一歲
    [InlineData("2026/06/11", "2026/06/11", 0)]    // 新生兒當日 → 0
    public void Years_matches_legacy(string now, string birth, int expected)
        => AgeCalculator.Years(now, birth).Should().Be(expected);

    [Theory]
    [InlineData("", "1980/01/01")]                 // 缺現在日 → legacy -1 → clamp 0
    [InlineData("2026/01/01", "")]                 // 缺基準日
    public void Years_missing_date_is_zero(string now, string birth)
        => AgeCalculator.Years(now, birth).Should().Be(0);

    [Theory]
    [InlineData("2026/06/11", "2026/02/01", 4)]    // 同年月差
    [InlineData("2026/01/11", "2026/06/01", 7)]    // 月份回繞:1-6=-5 → +12(legacy 怪癖,不跨年累計)
    [InlineData("", "2026/01/01", 0)]              // 缺日 → 0
    public void Months_matches_legacy(string now, string baseDate, int expected)
        => AgeCalculator.Months(now, baseDate).Should().Be(expected);

    [Theory]
    [InlineData("2026/01/10", "2026/01/05", 5)]
    [InlineData("2026/01/05", "2026/01/10", -5)]   // 可為負(legacy 不 clamp)
    [InlineData("2026/01/01", "", -1)]             // 缺基準日 → -1
    public void Days_matches_legacy(string now, string baseDate, int expected)
        => AgeCalculator.Days(now, baseDate).Should().Be(expected);
}
