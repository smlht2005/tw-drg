using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>ICD-10-CM/PCS 驗證關卡,移植自 rddi0001 icd10cm_chk_yyy + errcode_chk_yyy + icdop_chk_yyy。
/// 對 20 個 CM 與 20 個 OP 碼逐一查 RDDT_XICD,回寫 ctx.ErrCode[0..39],
/// 回傳硬性錯誤累計(0 = 通過,>0 = CM 檢核失敗 → rddi1000_main 回 -1)。</summary>
public static class Icd10CmCheck
{
    public static int Run(GroupingContext ctx, GroupingRuleset rs)
    {
        var total = 0;
        for (var i = 0; i < 20; i++)
        {
            if (i == 0 || ctx.CmCodes[i].Length > 0)
            {
                total += ErrcodeChk(i == 0 ? ctx.CmCodes[i].Trim() : ctx.CmCodes[i], ctx, rs, i == 0 ? 1 : 2, out var e);
                ctx.ErrCode[i] = e;
            }
        }
        for (var j = 0; j < 20; j++)
        {
            if (j == 0 || ctx.OpCodes[j].Length > 0)
            {
                total += ErrcodeChk(ctx.OpCodes[j], ctx, rs, j == 0 ? 3 : 4, out var e);
                ctx.ErrCode[j + 20] = e;
            }
        }
        return total;
    }

    // proc_type:1=主 CM、2=次 CM、3=主 OP、4=次 OP
    private static int ErrcodeChk(string icd, GroupingContext ctx, GroupingRuleset rs, int procType, out char errCode)
    {
        if (ctx.CmCodes[0].Length == 0 && procType == 1) { errCode = 'E'; return 1; }
        if (ctx.OpCodes[0].Length == 0 && procType == 3)
        {
            errCode = IcdopChk("ICD_OP", "", 1, 2, ctx) == 2 ? 'R' : '0';
            return 0;
        }

        var icdOpType = procType is 1 or 2 ? "1" : "2";
        var row = rs.Xicd.FirstOrDefault(r =>
            (r.IcdOpType ?? "").Trim() == icdOpType && (r.IcdCode ?? "").Trim() == icd.Trim());
        if (row is null) { errCode = 'Z'; return 1; }

        var sexChk = Nz((row.SexChk ?? "").Trim().ToUpperInvariant());
        var ageChk = Nz((row.AgeChk ?? "").Trim());
        ctx.PrmIcdChk = Nz((row.PrmIcdChk ?? "").Trim());   // H_PRM_ICD_CHK:供 mdc25 沿用
        var prm = ctx.PrmIcdChk;
        var sexNo = Nz((row.SexNo ?? "").Trim());

        if (procType == 1)
        {
            ctx.SexArr = sexChk;
            var cmList = CmList(ctx);
            var row2 = rs.Xicd.FirstOrDefault(r =>
                (r.IcdOpType ?? "").Trim() == "1" && r.SexNo != null && cmList.Contains(r.IcdCode));
            if (row2 is not null) ctx.VSexNo = (row2.SexNo ?? "").Trim().ToUpperInvariant();
        }

        if (procType == 1)
        {
            if (prm == "1") { errCode = '1'; return 0; }
            if (prm == "2") { errCode = '2'; return 0; }
        }
        if (procType is 1 or 2)
        {
            if (prm == "3") { errCode = '3'; return 0; }
            if (prm == "4") { errCode = '4'; return 0; }
            if (prm == "6") { errCode = '6'; return 0; }
        }
        if (procType is 3 or 4)
        {
            if (icd == "3965" || prm == "7") { errCode = '7'; return 0; }
            if (icd == "3761" || prm[0] == '9') { errCode = '9'; return 0; }
        }
        if (procType is 1 or 2)
        {
            if (prm[0] == '8') { errCode = '8'; return 0; }
            if (prm == "5") { errCode = '5'; return 0; }
            if (procType == 1 && Chk5ComboHit(ctx, rs)) { errCode = '5'; return 0; }
        }
        if (procType == 1)
        {
            if (prm == "A") { errCode = 'A'; return 1; }
            if (prm == "X") { errCode = 'X'; return 1; }
        }
        if (procType is 1 or 2)
        {
            if (ctx.PartMark != "903")
            {
                if (ctx.Ages != 0 && ageChk == "H") { errCode = 'H'; return 1; }
                if ((ctx.Ages > 17 || ctx.Ages < 0) && ageChk == "I") { errCode = 'I'; return 1; }
                if (AgeCalculator.Days(ctx.OutDate, "2025/01/01") < 0)
                {
                    if ((ctx.Ages != 0 || ctx.Months >= 3) && ageChk == "U") { errCode = 'U'; return 1; }
                    if (ctx.Ages == 0 && ctx.Months < 3 && ageChk == "W") { errCode = 'W'; return 1; }
                }
            }
            if (ageChk == "J")
            {
                if (AgeCalculator.Days(ctx.OutDate, "2025/01/01") < 0)
                {
                    if (ctx.Ages < 12 || ctx.Ages > 55 || ctx.Sex != "F") { errCode = 'J'; return 0; }
                }
                else if (ctx.Ages < 9 || ctx.Ages > 64 || ctx.Sex != "F") { errCode = 'J'; return 0; }
            }
            if (AgeCalculator.Days(ctx.OutDate, "2025/01/01") < 0)
            {
                if ((ctx.Ages < 14 || ctx.Ages > 119) && ageChk == "K") { errCode = 'K'; return 1; }
            }
            else if ((ctx.Ages < 15 || ctx.Ages > 124) && ageChk == "K") { errCode = 'K'; return 1; }
        }
        if (ctx.PartMark != "903" && sexNo != "Y" && ctx.VSexNo != "Y")
        {
            if (ctx.Sex != "M" && sexChk == "M") { errCode = 'M'; return 1; }
            if (ctx.Sex != "F" && sexChk == "F") { errCode = 'F'; return 1; }
        }
        if (procType == 1)
        {
            if (ctx.PartMark != "903" && sexNo != "Y" && sexChk == "B" && ctx.Sex == "X") { errCode = 'S'; return 1; }
            if (prm == "O") { errCode = 'O'; return 1; }
            if (prm == "V" && IcdopChk("ICD10CM", "", 1, 2, ctx) != 2) { errCode = 'V'; return 1; }
            if (prm == "N") { errCode = 'N'; return 1; }
            if (prm == "Q") { errCode = 'Q'; return 0; }
        }
        if (prm == "X" && procType is 2 or 3 or 4) { errCode = 'T'; return 0; }

        errCode = '0';
        return 0;
    }

    // chk5 特例:主+次診斷組合碼命中 PRM_ICD_CHK=5 且含 '+' 的 XICD 列。
    private static bool Chk5ComboHit(GroupingContext ctx, GroupingRuleset rs)
    {
        var chk52 = rs.Xicd
            .Where(x => (x.IcdOpType ?? "").Trim() == "1" && (x.PrmIcdChk ?? "").Trim() == "5")
            .ToList();
        if (!chk52.Any(x => !string.IsNullOrEmpty(x.IcdCode) && x.IcdCode.Contains('+'))) return false;

        var combo = ctx.CmCodes
            .Select((s, index) => (index, s))
            .Where(o => o.index >= 1)
            .Select(o => ctx.CmCodes[0] + o.s)
            .ToArray();
        return chk52.Any(x => !string.IsNullOrEmpty(x.IcdCode) && x.IcdCode.Contains('+') && combo.Contains(x.IcdCode));
    }

    // icdop_chk_yyy:自 startIdx 起(含)是否有符合條件之碼;命中回 2。
    private static int IcdopChk(string which, string target, int startIdx, int chkType, GroupingContext ctx)
    {
        var arr = which == "ICD10CM" ? ctx.CmCodes : ctx.OpCodes;
        for (var idx = startIdx; idx < arr.Length; idx++)
        {
            if (chkType == 1 && arr[idx] == target) return 2;
            if (chkType == 2 && arr[idx].Length > 0) return 2;
        }
        return 0;
    }

    // in_H_CM_CODE_Gen 等價:非空白 CM 碼(已 Trim);全空回 ["NothingInArray"]。
    private static List<string> CmList(GroupingContext ctx)
    {
        var list = ctx.CmCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
        return list.Count == 0 ? ["NothingInArray"] : list;
    }

    private static string Nz(string s) => string.IsNullOrWhiteSpace(s) ? " " : s;
}
