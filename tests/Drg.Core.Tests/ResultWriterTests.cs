using System.Text;
using Drg.Core.Io;
using Drg.Core.Models;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

public class ResultWriterTests
{
    [Fact]
    public void Writes_utf8_with_chinese_header_and_one_row_per_result()
    {
        var results = new[]
        {
            new CodingResult { RowNum = 1, Drg = "50701", Mdc = "08", CcMark = "M", CcCode = new string('0', 20), ErrNo = "" },
            new CodingResult { RowNum = 2, Drg = "", Mdc = "", CcMark = "", CcCode = new string('0', 20), ErrNo = "", ErrDesc = "入院日期格式錯誤" },
        };

        var path = Path.GetTempFileName();
        try
        {
            new ResultWriter().Write(path, results, "Tw-DRG 115/01/01 (v3.4.20)");

            var bytes = File.ReadAllBytes(path);
            bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });   // UTF-8 BOM,利 Excel 開啟中文表頭(FR-014)

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            lines.Should().HaveCount(3);                       // 表頭 + 2 筆,零丟棄
            lines[0].Should().Contain("DRG").And.Contain("MDC").And.Contain("併發症註記").And.Contain("規則版本");
            lines[1].Should().Contain("50701").And.Contain("M").And.Contain("Tw-DRG 115/01/01 (v3.4.20)");
            lines[2].Should().Contain("入院日期格式錯誤");   // 錯誤列照樣輸出
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Quotes_values_containing_separators()
    {
        var results = new[]
        {
            new CodingResult { RowNum = 1, ErrDesc = "格式錯誤,欄位不足" },
        };

        var path = Path.GetTempFileName();
        try
        {
            new ResultWriter().Write(path, results, "v1");
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            lines[1].Should().Contain("\"格式錯誤,欄位不足\"");
        }
        finally { File.Delete(path); }
    }
}
