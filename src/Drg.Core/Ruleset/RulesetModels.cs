namespace Drg.Core.Ruleset;

// 版次化的 Tw-DRG 參考規則集,對應 legacy RDDT_* 表(由 Drg.Data 載入)。

public sealed record PdxMdc(string IcdNo, string MdcCode, string? Cc, string? Op);

public sealed record MdcDrgWgt(
    string TreeMdcNo, string TreeDrg, int TreeNo, float TreeWgt, int AvgExp,
    string? Dep, string? ComboNo);

public sealed record Xicd(
    string IcdCode, string? IcdCodePlus, string? IcdOpType, string? PrmIcdChk,
    string? OrNor, string TreeDrg, string ComboNo, string ItemType);

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
