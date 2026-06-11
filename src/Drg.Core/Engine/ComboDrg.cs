using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_drg_yyy 等價(移植自 rddi0001 1748–1919):從候選列(CandidateRepository 載入)
/// 篩出該案的 DRG 候選 — 套 marks(<see cref="CandidateFilter"/> ②③④⑤)+ opflag ICD/ITEM_TYPE 條件 ⑥
/// + MDC08 手術/非手術組特例 + <see cref="ComboXicd"/> 交叉複核,dedup 後回傳候選 TREE_DRG 清單交給 TreeSelector。
/// 純函式 — 候選列載入由 orchestrator(T026/Drg.Data)負責,本層不碰 DB(避免 Core→Data 反向相依)。
/// 註:ComboXicd 計數目前以 MdcDrgXicd 候選列近似;RDDT_DRG_XICD 專屬表待補,完整 DRG 一致性待 oracle。</summary>
public sealed class ComboDrg
{
    // MDC08(肌肉骨骼)同一 DRG 分非手術組 / 手術組,需回查 RDDT_PDX_MDC 的 OP 旗標(1882–1899)。
    private static readonly string[] Mdc08NonOp =
    [
        "21002", "21003", "21102", "21103", "21203", "21204", "21205", "21206", "21803", "21903",
        "22005", "22006", "21804", "21904", "22007", "22008", "22302", "22403", "22404", "22503",
        "22504", "22902",
    ];

    private static readonly string[] Mdc08Op =
    [
        "21001", "21101", "21201", "21202", "21801", "21901", "22001", "22002", "21802", "21902",
        "22003", "22004", "22301", "22401", "22402", "22501", "22502", "22901",
    ];

    /// <param name="candidates">該案的候選 join 列(主視圖∪NotIn∪UN、含 _00);marks/⑥/COMBO 計數皆於其上。</param>
    /// <param name="cm">H_CM_CODE(診斷碼,opflag=2 比對用)。</param>
    /// <param name="op">ICDOP_TAB(手術碼,含 '+' 組合碼,opflag=1 比對用)。</param>
    public IReadOnlyList<string> Generate(
        IReadOnlyList<MdcDrgXicd> candidates, string[] cm, string[] op,
        GroupingContext ctx, GroupingRuleset rs)
    {
        var counter = new ComboCounter(candidates, cm, op, rs.Xicd);
        var xicd = new ComboXicd(counter, ctx);

        // ①(TREE_MDC_NO)由查詢層已篩;此處套 ②③④⑤(marks)+ ⑥(opflag ICD/ITEM_TYPE)。
        var rows = candidates
            .Where(r => CandidateFilter.MarksMatch(r, ctx) && OpFlagMatch(r, cm, op, ctx))
            .Select(r => (
                TreeDrg: r.TreeDrg.Trim(),
                MdcNo: r.TreeMdcNo.Trim(),
                r.TreeNo,
                r.TreeWgt,
                Dep: (r.Dep ?? "").Trim(),
                ComboNo: (r.ComboNo ?? "").Trim()))
            .Distinct()
            .OrderByDescending(t => t.TreeDrg)
            .ToList();

        var result = new List<string>();
        foreach (var (treeDrg, mdcNo, _, _, _, comboNo) in rows)
        {
            if (mdcNo == "08" && !Mdc08Ok(treeDrg, cm, rs)) continue;
            if (xicd.Check(treeDrg, comboNo) == 0) result.Add(xicd.MappedTreeDrg);   // case 5 可能改寫候選 DRG
        }
        return result;
    }

    // ⑥ opflag 條件(1815–1857):opflag=2 比診斷碼(A/C 系列)、opflag=1 比手術碼(B 系列)。
    private static bool OpFlagMatch(MdcDrgXicd r, string[] cm, string[] op, GroupingContext ctx)
    {
        var item = (r.ItemType ?? "").Trim();
        var icd = (r.IcdCode ?? "").Trim();
        var plus = (r.IcdCodePlus ?? "").Trim();
        var combo = (r.ComboNo ?? "").Trim();

        if (ctx.OpFlag == 2)
        {
            if (combo is "73" or "74" or "75") return true;
            var cmSet = Trimmed(cm);
            if (item is "A" or "A1" or "A2" or "C" or "C1" or "C2") return cmSet.Contains(icd);
            if (item is "A3" or "C3") return !cmSet.Contains(icd);
            return false;
        }

        // opflag == 1:手術碼分單碼(single)與含 '+' 組合碼(plusSet)兩路。
        if (item == "B5") return true;
        var single = op.Where(o => !string.IsNullOrWhiteSpace(o) && !o.Contains('+')).Select(o => o.Trim()).ToHashSet();
        var plusSet = op.Where(o => !string.IsNullOrWhiteSpace(o) && o.Contains('+')).Select(o => o.Trim()).ToHashSet();
        if (single.Count == 0 && plusSet.Count == 0) throw new InvalidOperationException("in_ICDOP_TAB無值");

        if (item is "B" or "B1" or "B2" or "B4" or "B6" or "B13")
            return single.Contains(icd) || plusSet.Contains(plus);
        if (item is "B3" or "B7" or "B8")
            return !single.Contains(icd) && !plusSet.Contains(plus);
        return false;
    }

    // MDC08 候選列複核:非手術組需 PDX(CM[0]) 對應 MDC08 且 OP≠Y;手術組需 OP=Y。其餘不特判。
    private static bool Mdc08Ok(string treeDrg, string[] cm, GroupingRuleset rs)
    {
        var pdx = cm.Length > 0 ? cm[0].Trim() : "";
        if (Array.IndexOf(Mdc08NonOp, treeDrg) >= 0)
            return rs.PdxMdc.Any(p => p.IcdNo.Trim() == pdx && p.MdcCode.Trim() == "08" && (p.Op ?? "").Trim() != "Y");
        if (Array.IndexOf(Mdc08Op, treeDrg) >= 0)
            return rs.PdxMdc.Any(p => p.IcdNo.Trim() == pdx && p.MdcCode.Trim() == "08" && (p.Op ?? "").Trim() == "Y");
        return true;
    }

    private static HashSet<string> Trimmed(string[] codes) =>
        codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet();
}
