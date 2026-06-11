namespace Drg.Core.Models;

/// <summary>住院申報案件(輸入)。沿用 legacy 固定欄位輸入;日期保留原始字串以忠實驗證 "0000/00/00" 等情形。</summary>
public sealed class ClaimEncounter
{
    public int RowNum { get; init; }
    public string HospId { get; init; } = "";
    public string FeeYm { get; init; } = "";          // YYYYMM
    public string Pid { get; init; } = "";            // PHI
    public string SeqNo { get; init; } = "";
    public string Sex { get; set; } = "";             // F/M/X;空白時由 Pid 推導(SexResolver,T019a)
    public string InDate { get; init; } = "";         // YYYYMMDD
    public string Birthday { get; init; } = "";
    public string OutDate { get; init; } = "";
    public string? ChildBirthday { get; init; }       // 新生兒就附母親案件用
    public string PartCode { get; init; } = "";       // 案件類別(903=新生兒)
    public string TranCode { get; init; } = "";       // 轉歸代碼(4=死亡)
    public long MedAmt { get; init; }

    /// <summary>診斷碼,index 0 = 主診斷。</summary>
    public string[] CmCodes { get; init; } = new string[20];

    /// <summary>手術碼,index 0 = 主手術。</summary>
    public string[] OpCodes { get; init; } = new string[20];
}
