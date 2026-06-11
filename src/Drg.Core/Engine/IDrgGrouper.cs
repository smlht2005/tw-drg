using Drg.Core.Models;
using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

public interface IDrgGrouper
{
    /// <summary>對單筆案件指派 DRG/MDC/CC(rddi1000_main 等價);實作於 US1(T026)。</summary>
    CodingResult Group(ClaimEncounter claim, GroupingRuleset ruleset);
}
