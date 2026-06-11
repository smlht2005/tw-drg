using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>哨兵碼早退偵測,移植自 rddi0001 rddi1000_main 第 673–725 行(mdc_chk 之後、combo_drg 之前)。
/// 命中即整筆早退(legacy return 0,DRG 為哨兵碼);皆不命中回 null,交由後續外科/內科分流。
/// 來源 RDDT_XICD_Collection_* 子表均為 RDDT_XICD 的 in-memory 過濾(ICD_OP_TYPE / PRM_ICD_CHK)。</summary>
public static class SentinelCheck
{
    public static string? Run(GroupingContext ctx, GroupingRuleset rs)
    {
        // MDC 19/20 精神科 → XXX
        if (ctx.Mdc == "19" || ctx.Mdc == "20") return "XXX";

        // YYY:Type1_Chk1(ICD_OP_TYPE=1 且 PRM_ICD_CHK=1)命中主診斷 CM[0]
        var pdx = ctx.CmCodes[0].Trim();
        if (rs.Xicd.Any(x => T(x.IcdOpType) == "1" && T(x.PrmIcdChk) == "1" && T(x.IcdCode) == pdx))
            return "YYY";

        // ZZZ/WWW/GGG 比對全部非空 CM 碼(legacy in_H_CM_CODE 去引號清單)
        var cm = ctx.CmCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet();

        // ZZZ:Type1 且 PRM_ICD_CHK ∈ {3,4}
        if (rs.Xicd.Any(x => T(x.IcdOpType) == "1" && (U(x.PrmIcdChk) is "3" or "4") && cm.Contains(T(x.IcdCode))))
            return "ZZZ";
        // WWW:Type1_Chk6(PRM_ICD_CHK=6)
        if (rs.Xicd.Any(x => T(x.IcdOpType) == "1" && U(x.PrmIcdChk) == "6" && cm.Contains(T(x.IcdCode))))
            return "WWW";
        // GGG:Type1_Chk8(PRM_ICD_CHK=8)
        if (rs.Xicd.Any(x => T(x.IcdOpType) == "1" && U(x.PrmIcdChk) == "8" && cm.Contains(T(x.IcdCode))))
            return "GGG";

        // HHH:ophhh_chk_yyy()(子宮/輸卵管等手術組合 + 出院日版本切換)
        if (OpHhhCheck(ctx)) return "HHH";

        return null;
    }

    private static string T(string? s) => (s ?? "").Trim();
    private static string U(string? s) => (s ?? "").Trim().ToUpperInvariant();

    // ophhh_chk_yyy:依出院日 2020/07/01 前後切換的手術碼組合判定,命中回 true(legacy return 0)。
    private static bool OpHhhCheck(GroupingContext ctx)
    {
        var op = ctx.OpCodes;
        bool Has(string[] set) => op.Any(o => !string.IsNullOrWhiteSpace(o) && set.Contains(o));
        var afterCut = AgeCalculator.Days(ctx.OutDate, "2020/07/01") >= 0;   // 出院日 ≥ 2020/07/01

        int num = 0, num2 = 0, num3 = 0;
        if (!afterCut && (Has(Inner) || Has(Inner2))) num = 1;
        if ((num == 1 || afterCut) && Has(Inner4)) num2 = 1;
        if (Has(Inner5)) num3 = 1;

        if (!afterCut)
        {
            if (num == 1 && num2 == 1 && num3 == 1) return true;
        }
        else if (num2 == 1 && num3 == 1) return true;

        if (num3 == 0 && Has(Inner7)) num3 = 1;
        if (num3 == 1)
        {
            if (Has(Inner3) && Has(Inner6)) return true;
        }
        return false;
    }

    private static readonly string[] Inner = { "0UT90ZZ", "0UT94ZZ", "0UTC0ZZ", "0UTC4ZZ" };
    private static readonly string[] Inner2 = { "0UT97ZZ", "0UT98ZZ", "0UTC7ZZ", "0UTC8ZZ" };
    private static readonly string[] Inner3 = { "0US90ZZ", "0US94ZZ" };
    private static readonly string[] Inner4 = { "0USG0ZZ", "0USG4ZZ", "0USGXZZ", "0USG7ZZ", "0USG8ZZ" };
    private static readonly string[] Inner5 =
    {
        "0JQC0ZZ", "0JQC3ZZ", "0JUC07Z", "0JUC0JZ", "0JUC0KZ", "0JUC37Z", "0JUC3JZ", "0JUC3KZ", "0UUG07Z", "0UUG0JZ",
        "0UUG0KZ", "0UUG47Z", "0UUG4JZ", "0UUG4KZ", "0UUG77Z", "0UUG7JZ", "0UUG7KZ", "0UUG87Z", "0UUG8JZ", "0UUG8KZ"
    };
    private static readonly string[] Inner6 =
    {
        "0ULF7DZ", "0ULF7ZZ", "0ULF8DZ", "0ULF8ZZ", "0UMF0ZZ", "0UMF4ZZ", "0UNF0ZZ", "0UNF3ZZ", "0UNF4ZZ", "0UNF7ZZ",
        "0UNF8ZZ", "0UQF0ZZ", "0UQF3ZZ", "0UQF4ZZ", "0UQF7ZZ", "0UQF8ZZ", "0USF0ZZ", "0USF4ZZ", "0UTF0ZZ", "0UTF4ZZ",
        "0UTF7ZZ", "0UTF8ZZ", "0UUF07Z", "0UUF0JZ", "0UUF0KZ", "0UUF47Z", "0UUF4JZ", "0UUF4KZ", "0UUF77Z", "0UUF7JZ",
        "0UUF7KZ", "0UUF87Z", "0UUF8JZ", "0UUF8KZ"
    };
    private static readonly string[] Inner7 =
    {
        "0U7G0DZ", "0U7G0ZZ", "0U7G3DZ", "0U7G3ZZ", "0U7G4DZ", "0U7G4ZZ", "0UMG0ZZ", "0UMG4ZZ", "0UNG0ZZ", "0UNG3ZZ",
        "0UNG4ZZ", "0UQG0ZZ", "0UQG3ZZ", "0UQG4ZZ", "0UQG7ZZ", "0UQG8ZZ", "0UQGXZZ", "0UQG0ZZ", "0UQG3ZZ", "0UQG4ZZ",
        "0UQG7ZZ", "0UQG8ZZ", "0UQGXZZ", "0WQN0ZZ", "0WQN3ZZ", "0WQN4ZZ", "0WQNXZZ"
    };
}
