using Drg.Core.Engine;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

// 鎖定 legacy DRGService.convertSex 的行為。
public class SexResolverTests
{
    [Theory]
    [InlineData("M", "A123456789", "M")]   // 已是有效值 → 沿用
    [InlineData("f", "A123456789", "F")]   // 大小寫正規化
    [InlineData("X", "A123456789", "X")]
    public void Keeps_valid_sex(string sex, string pid, string expected)
        => SexResolver.Resolve(sex, pid).Should().Be(expected);

    [Theory]
    [InlineData("", "A123456789", "M")]    // 第 2 碼 '1' → M
    [InlineData("", "B223456789", "F")]    // 第 2 碼 '2' → F
    [InlineData("", "TC01234567", "M")]    // 第 2 碼 'C' → M(舊式居留證)
    [InlineData("", "FD01234567", "F")]    // 第 2 碼 'D' → F
    [InlineData("Z", "A223456789", "F")]   // 無效性別仍回退身分證推導
    public void Derives_from_pid_second_char(string sex, string pid, string expected)
        => SexResolver.Resolve(sex, pid).Should().Be(expected);

    [Theory]
    [InlineData("", "A")]                  // 不足 2 碼 → X
    [InlineData("", "")]
    public void Defaults_to_x(string sex, string pid)
        => SexResolver.Resolve(sex, pid).Should().Be("X");
}
