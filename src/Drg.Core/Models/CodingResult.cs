namespace Drg.Core.Models;

/// <summary>編碼結果(輸出),對應一筆 <see cref="ClaimEncounter"/>。</summary>
public sealed class CodingResult
{
    public int RowNum { get; init; }
    public string Drg { get; set; } = "";              // 含哨兵碼 XXX/YYY/ZZZ/WWW/GGG/HHH
    public string Mdc { get; set; } = "";
    public string CcMark { get; set; } = "";           // M / T / Y / N
    public string CcCode { get; set; } = new('0', 20); // 位元圖:貢獻併發症之診斷位置標 1
    public string ErrNo { get; set; } = "";
    public string ErrDesc { get; set; } = "";          // 中文錯誤原因(驗證未過時)
}
