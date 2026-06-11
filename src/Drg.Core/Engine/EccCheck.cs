using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>合併症/併發症(CC/MCC)分級,移植自 rddi0001 ecc_chk_yyy + get_ecc_yyy。
/// 依序判定 MCC(M)→ T → CC(Y),命中即停;設定 ctx.CcMark 與 ctx.CcCode(貢獻欄位標記)。</summary>
public static class EccCheck
{
    public static void Run(GroupingContext ctx, GroupingRuleset rs)
    {
        ctx.CcMark = "N";

        // Pass 1:MCC(主診斷直查群組 "MCC";次診斷走 TYPE "2")
        var found = false;
        for (var i = 0; i < 20; i++)
        {
            if (string.IsNullOrWhiteSpace(ctx.CmCodes[i])) continue;
            if (i == 0 ? GetEcc(ctx.CmCodes[0], "", "MCC", rs)
                       : GetEcc(ctx.CmCodes[0], ctx.CmCodes[i], "2", rs))
            {
                ctx.CcMark = "M";
                ctx.CcCode[i] = '1';
                found = true;
            }
        }
        if (found) return;

        // Pass 2:TYPE "3" → 標記 "T"
        Array.Fill(ctx.CcCode, '0');
        for (var i = 1; i < 20; i++)
        {
            if (ctx.CmCodes[i].Length > 0 && GetEcc(ctx.CmCodes[0], ctx.CmCodes[i], "3", rs))
            {
                ctx.CcMark = "T";
                ctx.CcCode[i] = '1';
                found = true;
            }
        }
        if (found) return;

        // Pass 3:CC(主診斷直查群組 "CC";次診斷走 TYPE "1")
        Array.Fill(ctx.CcCode, '0');
        for (var i = 0; i < 20; i++)
        {
            if (ctx.CmCodes[i].Length == 0) continue;
            if (i == 0 ? GetEcc(ctx.CmCodes[0], "", "CC", rs)
                       : GetEcc(ctx.CmCodes[0], ctx.CmCodes[i], "1", rs))
            {
                ctx.CcMark = "Y";
                ctx.CcCode[i] = '1';
            }
        }
    }

    // get_ecc_yyy:true = 該碼構成 CC/MCC(legacy 回 0);false = 不構成或被同群排除(legacy 回 -1)。
    private static bool GetEcc(string code, string codeX, string actType, GroupingRuleset rs)
    {
        if (actType is "MCC" or "CC")
        {
            return rs.EccGroup.Any(g =>
                (g.IcdNoGroup ?? "").Trim() == actType && (g.IcdNo ?? "").Trim() == code.Trim());
        }

        var groups = rs.Ecc
            .Where(e => (e.Type ?? "").Trim() == actType && (e.IcdNo1 ?? "").Trim() == codeX.Trim())
            .Select(e => (e.IcdNoGroup ?? "").Trim())
            .ToList();

        if (groups.Count > 0)
        {
            // 主診斷與次診斷落在同一排除群 → 不計 CC
            if (rs.EccGroup.Any(g =>
                    (g.IcdNo ?? "").Trim() == code.Trim() && groups.Contains((g.IcdNoGroup ?? "").Trim())))
            {
                return false;
            }
            return true;
        }

        // 無對應群:僅當存在 "9999" 萬用列(不限主診斷)時成立
        return rs.Ecc.Any(e =>
            (e.Type ?? "").Trim() == actType && (e.IcdNo1 ?? "").Trim() == codeX.Trim()
            && (e.IcdNoGroup ?? "").Trim() == "9999");
    }
}
