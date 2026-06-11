using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_AX / combo_CX 的計數實作(移植自 rddi0001),在 per-record 候選列(CandidateRepository
/// 載入)上計 distinct ICD_CODE,再套 <see cref="ComboMatchRule"/> 判定。combo_BX(含 XICD/分片表/特例
/// COMBO 分支)後續加入。</summary>
public sealed class ComboCounter(IReadOnlyList<MdcDrgXicd> candidates, string[] cmCodes)
{
    // combo_AX:A2/A4 比對次診斷(主診斷以外);其餘比對主診斷。CNT = distinct ICD_CODE。
    public bool ComboA(string treeDrg, string comboNo, string itemType)
    {
        IEnumerable<string> wanted = itemType is "A2" or "A4"
            ? cmCodes.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim())
            : [cmCodes[0].Trim()];
        var set = wanted.ToHashSet();

        var cnt = DistinctIcd(treeDrg, comboNo, itemType, set);
        return ComboMatchRule.MatchA(itemType, cnt);
    }

    // combo_CX:比對所有 CM 碼。CNT = distinct ICD_CODE。
    public bool ComboC(string treeDrg, string comboNo, string itemType)
    {
        var set = cmCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToHashSet();
        var cnt = DistinctIcd(treeDrg, comboNo, itemType, set);
        return ComboMatchRule.MatchC(itemType, cnt);
    }

    // 候選列依 (TREE_DRG, COMBO_NO, ITEM_TYPE) 過濾、ICD_CODE ∈ wanted,計 distinct ICD_CODE。
    private int DistinctIcd(string treeDrg, string comboNo, string itemType, HashSet<string> wanted)
        => candidates
            .Where(r => r.TreeDrg.Trim() == treeDrg
                && (r.ComboNo ?? "").Trim() == comboNo
                && (r.ItemType ?? "").Trim() == itemType
                && wanted.Contains((r.IcdCode ?? "").Trim()))
            .Select(r => (r.IcdCode ?? "").Trim())
            .Distinct()
            .Count();
}
