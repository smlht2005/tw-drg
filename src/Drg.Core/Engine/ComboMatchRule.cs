namespace Drg.Core.Engine;

/// <summary>combo_AX / combo_BX / combo_CX 的「回傳決策」段(移植自 rddi0001 3972–3995 / 4175 / 4222–4243)。
/// 給定 ITEM_TYPE 與候選計數(CNT、num),判定該 COMBO 配方是否成立。
/// 與資料存取分離:CNT/num 由查詢層(C1b)對 RDDT_MDC_DRG_XICD_* / RDDT_XICD_V 計算。</summary>
public static class ComboMatchRule
{
    // combo_AX:A/A1/A2/A5/A6 需 CNT>0;A3/A4 反向需 CNT==0。
    public static bool MatchA(string itemType, int cnt) => itemType switch
    {
        "A" or "A1" or "A2" or "A5" or "A6" => cnt > 0,
        "A3" or "A4" => cnt == 0,
        _ => false,
    };

    // combo_BX(rddi0001 line 4175)。
    public static bool MatchB(string itemType, int cnt, int num) => itemType switch
    {
        "B" or "B1" or "B2" or "B4" or "B5" or "B11" or "B12" => cnt > 0,
        "B3" or "B7" => cnt == 0,
        "B6" => cnt > 0 && num == cnt,
        "B8" => cnt > 0 && num == 0,
        "B13" => num > 0 && num < cnt,
        _ => false,
    };

    // combo_CX:C/C1/C4 需 CNT>0;C2 需 CNT>1;C3 反向需 CNT==0。
    public static bool MatchC(string itemType, int cnt) => itemType switch
    {
        "C" or "C1" or "C4" => cnt > 0,
        "C2" => cnt > 1,
        "C3" => cnt == 0,
        _ => false,
    };
}
