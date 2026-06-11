using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Drg.Core.Models;

namespace Drg.Core.Io;

/// <summary>
/// 固定欄位逗號分隔輸入 → <see cref="ClaimEncounter"/>,沿用 legacy 欄位位置(FR-014)。
/// 欄位對應依 legacy BatchIns_DRGTMP:診斷碼散落 col 7..11 + 25..39,手術碼 col 12..16 + 40..54。
/// 毀損列(欄位數不符)之容錯於 T033(US2)補強。
/// </summary>
public sealed class ClaimCsvReader : IClaimReader
{
    public IEnumerable<ClaimEncounter> Read(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            BadDataFound = null,
        });

        var row = 0;
        while (csv.Read())
        {
            row++;
            string F(int i) => csv.TryGetField<string>(i, out var v) ? (v ?? string.Empty).Trim() : string.Empty;

            var cm = new string[20];
            cm[0] = F(7);                                  // 主診斷
            for (var i = 1; i <= 4; i++) cm[i] = F(7 + i); // 次診斷 1..4  → col 8..11
            for (var i = 5; i <= 19; i++) cm[i] = F(25 + (i - 5)); // 次診斷 5..19 → col 25..39

            var op = new string[20];
            op[0] = F(12);                                 // 主手術
            for (var i = 1; i <= 4; i++) op[i] = F(12 + i);// 次手術 1..4  → col 13..16
            for (var i = 5; i <= 19; i++) op[i] = F(40 + (i - 5)); // 次手術 5..19 → col 40..54

            yield return new ClaimEncounter
            {
                RowNum = row,
                HospId = F(0), FeeYm = F(1), Pid = F(2), SeqNo = F(3), Sex = F(4),
                InDate = F(5), Birthday = F(6),
                TranCode = F(17), OutDate = F(18),
                MedAmt = long.TryParse(F(19), out var amt) ? amt : 0,
                PartCode = F(20),
                CmCodes = cm,
                OpCodes = op,
            };
        }
    }
}
