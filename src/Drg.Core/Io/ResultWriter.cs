using System.Globalization;
using System.Text;
using CsvHelper;
using Drg.Core.Models;

namespace Drg.Core.Io;

/// <summary>
/// 編碼結果輸出為 CSV(FR-014):改採 UTF-8 取代 legacy BIG5,中文表頭沿用 legacy 欄位別名。
/// 含 BOM —— 中文表頭在 Excel 直接開啟才不會亂碼(legacy 用 BIG5 規避此問題,UTF-8 須靠 BOM)。
/// 全筆輸出、含錯誤列(零丟棄,FR-007);DRG/MDC/CC 數值與 legacy 一致(原則 II)。
/// </summary>
public sealed class ResultWriter : IResultWriter
{
    public void Write(string path, IEnumerable<CodingResult> results)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var header in new[] { "列號", "DRG", "MDC", "併發症註記", "併發症碼", "錯誤註記", "檔案格式錯誤備註" })
            csv.WriteField(header);
        csv.NextRecord();

        foreach (var r in results)
        {
            csv.WriteField(r.RowNum);
            csv.WriteField(r.Drg);
            csv.WriteField(r.Mdc);
            csv.WriteField(r.CcMark);
            csv.WriteField(r.CcCode);
            csv.WriteField(r.ErrNo);
            csv.WriteField(r.ErrDesc);
            csv.NextRecord();
        }
    }
}
