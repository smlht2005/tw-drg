using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_AX / combo_BX / combo_CX 的計數實作(移植自 rddi0001),在 per-record 候選列
/// (CandidateRepository 載入)+ RDDT_XICD 上計數,再套 <see cref="ComboMatchRule"/> 判定。</summary>
public sealed class ComboCounter
{
    private readonly IReadOnlyList<MdcDrgXicd> _candidates;
    private readonly string[] _cm;
    private readonly string[] _op;          // ICDOP_TAB(含 Op_Code_Rtn 組合 '+' 項)
    private readonly IReadOnlyList<Xicd> _xicd;
    private readonly string _vOpWk;

    public ComboCounter(
        IReadOnlyList<MdcDrgXicd> candidates, string[] cmCodes,
        string[]? opCodes = null, IReadOnlyList<Xicd>? xicd = null, string vOpWk = "")
    {
        _candidates = candidates;
        _cm = cmCodes;
        _op = opCodes ?? [];
        _xicd = xicd ?? [];
        _vOpWk = vOpWk;
    }

    // combo_AX:A2/A4 比對次診斷(主診斷以外);其餘比對主診斷。CNT = distinct ICD_CODE。
    public bool ComboA(string treeDrg, string comboNo, string itemType)
    {
        IEnumerable<string> wanted = itemType is "A2" or "A4"
            ? _cm.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim())
            : [_cm[0].Trim()];
        var cnt = DistinctIcd(treeDrg, comboNo, itemType, wanted.ToHashSet());
        return ComboMatchRule.MatchA(itemType, cnt);
    }

    // combo_CX:比對所有 CM 碼。CNT = distinct ICD_CODE。
    public bool ComboC(string treeDrg, string comboNo, string itemType)
    {
        var set = _cm.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet();
        var cnt = DistinctIcd(treeDrg, comboNo, itemType, set);
        return ComboMatchRule.MatchC(itemType, cnt);
    }

    // combo_BX:四分支(B5 / B6·B8·B13 / 特例 COMBO / else)。
    public bool ComboB(string treeDrg, string comboNo, string itemType)
    {
        var cnt = 0;
        var num = 0;

        if (itemType == "B5")
        {
            var ops = OpSet();
            cnt = _xicd.Count(x => IsOrProc(x) && ops.Contains((x.IcdCode ?? "").Trim()));
            if (_vOpWk == "*") cnt++;
        }
        else if (itemType is "B6" or "B8" or "B13")
        {
            foreach (var op in _op)
            {
                if (string.IsNullOrEmpty(op)) continue;
                var code = op.Trim();
                var hit = _xicd.Where(x => (x.IcdCode ?? "").Trim() == code && IsOrProc(x))
                    .Select(x => (x.IcdCode ?? "").Trim()).Distinct().Count();
                if (hit > 0)
                {
                    cnt += hit;
                    num += B8NoPlus(code, treeDrg, comboNo, itemType) + B8Plus(code, treeDrg, comboNo, itemType);
                }
            }
        }
        else if (IsSpecialBCombo(itemType, comboNo))
        {
            var ops = OpSet();
            var matched = _candidates.Where(r => RowMatch(r, treeDrg, comboNo, itemType)
                && (ops.Contains((r.IcdCode ?? "").Trim()) || ops.Contains((r.IcdCodePlus ?? "").Trim())))
                .Select(r => (r.IcdCode ?? "").Trim());
            foreach (var icd in matched)
                cnt += _op.Count(o => o.Trim() == icd && o.Trim().Length > 0);   // legacy:num 累加,CNT 取其上界
        }
        else
        {
            var nonPlus = _op.Where(o => !string.IsNullOrWhiteSpace(o) && !o.Contains('+')).Select(o => o.Trim()).ToHashSet();
            var plus = _op.Where(o => !string.IsNullOrWhiteSpace(o) && o.Contains('+')).Select(o => o.Trim()).ToHashSet();
            cnt = _candidates.Where(r => RowMatch(r, treeDrg, comboNo, itemType)
                && (nonPlus.Contains((r.IcdCode ?? "").Trim()) || plus.Contains((r.IcdCodePlus ?? "").Trim())))
                .Select(r => ((r.IcdCode ?? "").Trim(), (r.IcdCodePlus ?? "").Trim()))
                .Distinct().Count();
        }

        return ComboMatchRule.MatchB(itemType, cnt, num);
    }

    // RDDT_XICD:OR 手術(ICD_OP_TYPE=2、OR_NOR=Y、PRM_ICD_CHK 為空或非 'X')。
    private static bool IsOrProc(Xicd x) =>
        (x.IcdOpType ?? "").Trim() == "2"
        && (x.PrmIcdChk is null || (x.PrmIcdChk ?? "").Trim() != "X")
        && (x.OrNor ?? "").Trim() == "Y";

    // prepareDRG_XICD_B8_NoPlus:候選列 ICD_CODE_PLUS 為 null 且 ICD_CODE=code。
    private int B8NoPlus(string code, string treeDrg, string comboNo, string itemType) =>
        _candidates.Any(r => r.IcdCodePlus is null && (r.IcdCode ?? "").Trim() == code
            && RowMatch(r, treeDrg, comboNo, itemType)) ? 1 : 0;

    // prepareDRG_XICD_B8_Plus:候選列 ICD_CODE_PLUS 非 null 且 (ICD_CODE=code 或 ICD_CODE_PLUS=code)。
    private int B8Plus(string code, string treeDrg, string comboNo, string itemType) =>
        _candidates.Any(r => r.IcdCodePlus is not null
            && ((r.IcdCode ?? "").Trim() == code || (r.IcdCodePlus ?? "").Trim() == code)
            && RowMatch(r, treeDrg, comboNo, itemType)) ? 1 : 0;

    private static bool IsSpecialBCombo(string item, string combo) =>
        (item == "B" && combo is "28" or "30" or "31" or "33" or "34" or "35" or "37")
        || (item == "B1" && combo is "51" or "89" or "91")
        || (item == "B4" && combo == "84");

    private HashSet<string> OpSet() =>
        _op.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToHashSet();

    private static bool RowMatch(MdcDrgXicd r, string treeDrg, string comboNo, string itemType) =>
        r.TreeDrg.Trim() == treeDrg && (r.ComboNo ?? "").Trim() == comboNo && (r.ItemType ?? "").Trim() == itemType;

    private int DistinctIcd(string treeDrg, string comboNo, string itemType, HashSet<string> wanted) =>
        _candidates
            .Where(r => RowMatch(r, treeDrg, comboNo, itemType) && wanted.Contains((r.IcdCode ?? "").Trim()))
            .Select(r => (r.IcdCode ?? "").Trim())
            .Distinct()
            .Count();
}
