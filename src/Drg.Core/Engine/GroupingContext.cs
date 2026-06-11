namespace Drg.Core.Engine;

/// <summary>單筆分組過程的可變狀態,對應 legacy rddi0001 的 H_* host 變數族。
/// 由 orchestrator(DrgGrouper)建立,貫穿各 _yyy 等價模組讀寫(取代 legacy 的靜態全域)。</summary>
public sealed class GroupingContext
{
    public required string[] CmCodes { get; init; }   // H_CM_CODE[20](已 Trim)
    public required string[] OpCodes { get; init; }   // H_OP_CODE[20]
    public required string Sex { get; init; }         // H_ID_SEX(SexResolver 結果)
    public required string PartMark { get; init; }    // H_PART_MARK(部分代碼,"903"=新生兒)
    public required string OutDate { get; init; }     // H_OUT_DATE "yyyy/MM/dd"
    public int Ages { get; set; }                     // H_ID_AGES
    public int Months { get; set; }                   // H_ID_MONTHS
    public int Days { get; set; }                     // H_ID_DAYS(住院/年齡日數,combo_drg 年齡條件用)
    public int MedAmt { get; init; }                  // H_MED_AMT(醫療費用,tree 權重以零權重列回推用)
    public string TranCode { get; init; } = "";       // H_TRAN_CODE(轉歸碼,combo_drg LIVE_MARK 用)
    public string Birthday { get; init; } = "";        // H_ID_BIRTHDAY "yyyy/MM/dd"(ComboXicd case 74)
    public string ChildBirthday { get; init; } = "";   // H_CHILD_BIRTHDAY(903 新生兒)
    public string DepFlag { get; set; } = "";         // v_depflag(P=外科 / M=內科)
    public string FilterMdc { get; set; } = "";       // v_MDC_1(候選查詢用 MDC,可為 00/UN/22…)

    // 跨模組輸出狀態
    public string SexArr { get; set; } = "";          // sex_arr(供 mdc_chk 性別分流)
    public string VSexNo { get; set; } = "";          // v_sex_no
    public char[] ErrCode { get; } = NewErr();        // H_ERR_CODE_1[40](CM 0–19 / OP 20–39)
    public string CcMark { get; set; } = "N";         // H_CC_MARK(N=無 / M=MCC / T / Y=CC)
    public char[] CcCode { get; } = NewCc();          // H_CC_CODE_1[20](標記貢獻 CC 的 CM 欄位)
    public string Mdc { get; set; } = "";             // H_MDC_1(主診斷類別)
    public string PrmIcdChk { get; set; } = "";       // H_PRM_ICD_CHK(errcode_chk 最後命中值,mdc25 沿用)

    private static char[] NewErr()
    {
        var a = new char[40];
        Array.Fill(a, ' ');
        return a;
    }

    private static char[] NewCc()
    {
        var a = new char[20];
        Array.Fill(a, '0');
        return a;
    }
}
