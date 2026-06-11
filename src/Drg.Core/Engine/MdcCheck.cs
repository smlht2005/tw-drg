using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>MDC(主要診斷類別)指派,移植自 rddi0001 mdc_chk_yyy + mdc24/25/1to23_chk_yyy。
/// 優先序:MDC 24(多重外傷)→ 25(HIV)→ 1–23;皆不成立則 ctx.Mdc = ""。</summary>
public static class MdcCheck
{
    public static void Run(GroupingContext ctx, GroupingRuleset rs)
    {
        if (!Mdc24(ctx, rs) && !Mdc25(ctx, rs) && !Mdc1To23(ctx, rs))
            ctx.Mdc = "";
    }

    // mdc_exists_yyy:主診斷是否落在指定 MDC。
    private static bool MdcExists(string mdc, GroupingContext ctx, GroupingRuleset rs)
        => rs.PdxMdc.Any(p => (p.IcdNo ?? "").Trim() == ctx.CmCodes[0].Trim() && (p.MdcCode ?? "").Trim() == mdc);

    // mdc24_chk_yyy:主診斷屬 24 + 有次診斷 + MDC24 內出現 ≥2 個不同 CC 群。
    private static bool Mdc24(GroupingContext ctx, GroupingRuleset rs)
    {
        if (!MdcExists("24", ctx, rs)) return false;
        if (!ctx.CmCodes.Skip(1).Any(c => c.Length > 0)) return false;   // icdop_chk(ICD10CM, index>=1)

        var cm = ctx.CmCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
        var distinctCc = rs.PdxMdc
            .Where(p => cm.Contains(p.IcdNo) && p.MdcCode == "24" && p.Cc != null)
            .Select(p => p.Cc)
            .Distinct()
            .Count();
        if (distinctCc < 2) return false;

        ctx.Mdc = "24";
        return true;
    }

    // mdc25_chk_yyy:主診斷屬 25,且任一 CM 在 XICD 的 PRM_ICD_CHK == "3"。
    // 注意:ctx.PrmIcdChk 跨迭代/呼叫保留(忠實對應 legacy H_PRM_ICD_CHK 之 host 變數行為)。
    private static bool Mdc25(GroupingContext ctx, GroupingRuleset rs)
    {
        if (!MdcExists("25", ctx, rs)) return false;

        for (var i = 0; i < 20; i++)
        {
            if (ctx.CmCodes[i].Trim() == "") continue;

            var row = rs.Xicd.FirstOrDefault(x =>
                (x.IcdOpType ?? "").Trim() == "1" && (x.IcdCode ?? "").Trim() == ctx.CmCodes[i].Trim());
            if (row is not null) ctx.PrmIcdChk = (row.PrmIcdChk ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(ctx.PrmIcdChk)) ctx.PrmIcdChk = " ";

            if (ctx.PrmIcdChk == "3") { ctx.Mdc = "25"; return true; }
        }
        return false;
    }

    // mdc1to23_chk_yyy:性別分流(B/T)優先,否則主+次組合碼或主診斷查 MDC 1–23。
    private static bool Mdc1To23(GroupingContext ctx, GroupingRuleset rs)
    {
        ctx.Mdc = "";

        if (ctx.SexArr == "B")
        {
            ctx.Mdc = ctx.Sex != "F" ? "12" : "13";
            return true;
        }
        if (ctx.SexArr == "T")
        {
            ctx.Mdc = ctx.Sex.Length > 0 && ctx.Sex[0] != 'F' ? "11" : "13";
            return true;
        }

        PdxMdc? row = null;
        for (var i = 1; i < 20; i++)
        {
            if (ctx.CmCodes[i] == "") continue;
            var combo = ctx.CmCodes[0] + "+" + ctx.CmCodes[i];
            row = rs.PdxMdc.FirstOrDefault(p => (p.IcdNo ?? "").Trim() == combo && InMdc1To23(p.MdcCode));
            if (row is not null) break;
        }
        row ??= rs.PdxMdc.FirstOrDefault(p =>
            (p.IcdNo ?? "").Trim() == ctx.CmCodes[0].Trim() && InMdc1To23(p.MdcCode));

        ctx.Mdc = "";
        if (row is not null) { ctx.Mdc = (row.MdcCode ?? "").Trim(); return true; }
        return false;
    }

    // legacy 用 Convert.ToInt32(MDC_CODE) 1..23;以 TryParse 容錯非數值列(實機資料皆為數值)。
    private static bool InMdc1To23(string? mdc) => int.TryParse(mdc, out var n) && n is >= 1 and <= 23;
}
