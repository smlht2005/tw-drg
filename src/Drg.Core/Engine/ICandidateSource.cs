using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>combo_drg 候選列來源接縫:主編排(<c>DrgGrouper</c>)逐筆取候選 join 列,
/// 但 Core 不得反向相依 Drg.Data。故在 Core 定義介面,由 Drg.Data 的 CandidateRepository 實作。
/// 對應 legacy prepareDRG_XICD / RDDT_MDC_DRG_XICD(_00)。</summary>
public interface ICandidateSource
{
    /// <summary>主編排候選表:主視圖(依 MDC + 病歷碼)∪ NotIn(依 MDC)∪ UN(全)。</summary>
    IReadOnlyList<MdcDrgXicd> LoadForMdc(string mdc, IReadOnlyCollection<string> codes);

    /// <summary>"00" 視圖候選(依病歷碼);對應 RDDT_MDC_DRG_XICD_00。</summary>
    IReadOnlyList<MdcDrgXicd> Load00(IReadOnlyCollection<string> codes);
}
