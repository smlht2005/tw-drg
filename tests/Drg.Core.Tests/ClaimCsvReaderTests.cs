using Drg.Core.Io;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

public class ClaimCsvReaderTests
{
    [Fact]
    public void Reads_fixed_columns_into_encounter()
    {
        var f = new string[55];
        for (var i = 0; i < 55; i++) f[i] = "";
        f[0] = "HOSP01"; f[1] = "11501"; f[2] = "A123456789"; f[3] = "0001"; f[4] = "M";
        f[5] = "20260105"; f[6] = "19800101";
        f[7] = "I2101"; f[8] = "E119"; f[25] = "N179";       // 診斷:主 + 次1 + 次5
        f[12] = "027034Z"; f[40] = "0210093";                // 手術:主 + 次5
        f[17] = "4"; f[18] = "20260110"; f[19] = "120000"; f[20] = "1";

        var path = Path.GetTempFileName();
        File.WriteAllText(path, string.Join(",", f) + "\n");
        try
        {
            var rows = new ClaimCsvReader().Read(path).ToList();

            rows.Should().HaveCount(1);
            var e = rows[0];
            e.HospId.Should().Be("HOSP01");
            e.Pid.Should().Be("A123456789");
            e.Sex.Should().Be("M");
            e.InDate.Should().Be("20260105");
            e.CmCodes[0].Should().Be("I2101");   // col 7  主診斷
            e.CmCodes[1].Should().Be("E119");    // col 8  次診斷1
            e.CmCodes[5].Should().Be("N179");    // col 25 次診斷5
            e.OpCodes[0].Should().Be("027034Z"); // col 12 主手術
            e.OpCodes[5].Should().Be("0210093"); // col 40 次手術5
            e.TranCode.Should().Be("4");
            e.OutDate.Should().Be("20260110");
            e.MedAmt.Should().Be(120000);
        }
        finally { File.Delete(path); }
    }
}
