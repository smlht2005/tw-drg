namespace Drg.Core.Ruleset;

// 版次化的 Tw-DRG 參考規則集,對應 legacy RDDT_* 表(由 Drg.Data 載入)。

public sealed record PdxMdc(string IcdNo, string MdcCode, string? Cc, string? Op);

public sealed record MdcDrgWgt(
    string TreeMdcNo, string TreeDrg, int TreeNo, float TreeWgt, int AvgExp,
    string? Dep, string? ComboNo);

// RDDT_XICD_V:主/次診斷與手術碼的有效性/性別/年齡/排除檢核(errcode_chk_yyy 用)。
public sealed record Xicd(
    string? IcdOpType, string IcdCode, string? SexChk, string? AgeChk,
    string? PrmIcdChk, string? OrNor, string? SexNo);

// 註:combo_xicd_chk_yyy(T024)使用的 RDDT_DRG_XICD(TREE_DRG/COMBO_NO/ITEM_TYPE/
// ICD_CODE_PLUS,30 張分片表)為另一張表,屆時再以獨立 record 載入。

public sealed record Ecc(string Type, string IcdNo1, string IcdNoGroup);

public sealed record EccGroup(string IcdNo, string IcdNoGroup);

/// <summary>規則集快照,具版次(FR-013)。</summary>
public sealed class GroupingRuleset
{
    public required string Version { get; init; }   // e.g. "Tw-DRG 115/01/01 (v3.4.20)"
    public IReadOnlyList<PdxMdc> PdxMdc { get; init; } = [];
    public IReadOnlyList<MdcDrgWgt> MdcDrgWgt { get; init; } = [];
    public IReadOnlyList<Xicd> Xicd { get; init; } = [];
    public IReadOnlyList<Ecc> Ecc { get; init; } = [];
    public IReadOnlyList<EccGroup> EccGroup { get; init; } = [];
}
