using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_drg_yyy RowFilter 的「marks」段(移植自 1781–1813 行):
/// CC_MARK / AGE_MARK(7 組年齡條件)/ LIVE_MARK / DEP。純函式,與資料供裝無關——
/// 查詢層(逐筆參數化 SQL)負責 MDC 篩選與 ICD/ITEM_TYPE 比對,本層僅判定 marks。</summary>
public static class CandidateFilter
{
    public static bool MarksMatch(MdcDrgXicd row, GroupingContext ctx)
        => CcMatch(row, ctx) && AgeMatch(row, ctx) && LiveMatch(row, ctx) && DepMatch(row, ctx);

    // (TREE_DRG='228' and ITEM_TYPE='B') or COMBO_NO in (63,67,72) or (CC_MARK='X' or CC_MARK = H_CC_MARK_1)
    private static bool CcMatch(MdcDrgXicd r, GroupingContext ctx)
    {
        if (r.TreeDrg.Trim() == "228" && (r.ItemType ?? "").Trim() == "B") return true;
        if ((r.ComboNo ?? "").Trim() is "63" or "67" or "72") return true;

        var ccMark1 = ctx.CcMark == "N" ? "N" : "Y";   // H_CC_MARK_1:本案是否有 CC/MCC
        var cc = (r.CcMark ?? "").Trim();
        return cc == "X" || cc == ccMark1;
    }

    // AGE_MARK='N' → 不限;AGE_MARK='Y' → 7 組條件任一成立即過。
    private static bool AgeMatch(MdcDrgXicd r, GroupingContext ctx)
    {
        if ((r.AgeMark ?? "").Trim() != "Y") return true;

        var a = ctx.Ages;
        var d = ctx.Days;
        return Yn(r.Age18Y, a >= 18)
            || Yn(r.Age36Y, a >= 36)
            || Yn(r.Age41Y, a >= 41)
            || Yn(r.Age5Y65Y, a < 5 || a >= 65)
            || Yn(r.Age2Y, a >= 2)
            || Yn(r.Age28D, d >= 28)
            || Yn(r.Age2D, d <= 2);
    }

    // (cond and col='Y') or (not cond and col='N')
    private static bool Yn(string? col, bool cond)
    {
        var c = (col ?? "").Trim();
        return (cond && c == "Y") || (!cond && c == "N");
    }

    // (LIVE='N' and (tran='4' or (DRG='12701' and tran='A')))
    //  or (LIVE='Y' and tran<>'4' and not(DRG='12702' and tran='A'))
    //  or LIVE is null
    private static bool LiveMatch(MdcDrgXicd r, GroupingContext ctx)
    {
        if (r.LiveMark is null) return true;

        var live = r.LiveMark.Trim();
        var tran = ctx.TranCode.Trim();
        var drg = r.TreeDrg.Trim();
        if (live == "N") return tran == "4" || (drg == "12701" && tran == "A");
        if (live == "Y") return tran != "4" && !(drg == "12702" && tran == "A");
        return false;
    }

    // 非 MDC15:DEP 必須等於 v_depflag(MDC22 另放行 COMBO_NO 58/67);MDC15 不篩 DEP。
    private static bool DepMatch(MdcDrgXicd r, GroupingContext ctx)
    {
        var mdc = ctx.FilterMdc.Trim();
        if (mdc == "15") return true;

        var dep = (r.Dep ?? "").Trim();
        if (mdc == "22")
            return (r.ComboNo ?? "").Trim() is "58" or "67" || dep == ctx.DepFlag.Trim();
        return dep == ctx.DepFlag.Trim();
    }
}
