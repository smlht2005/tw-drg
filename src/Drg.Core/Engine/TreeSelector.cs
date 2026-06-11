using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>DRG 最終決選,移植自 rddi0001 tree_yyy。先對候選做等價 DRG 重映射,
/// 再從 RDDT_MDC_DRGWGT 取權重最高者(MDC15 取最小 TREE_NO;零權重列以 MED_AMT/AVG_EXP 回推)。</summary>
public static class TreeSelector
{
    public static string Select(IReadOnlyList<string> candidates, GroupingContext ctx, GroupingRuleset rs)
    {
        var drg = candidates.ToArray();
        Remap(drg);
        var set = drg.Where(d => d.Length > 0).ToHashSet();

        var bestNo = 200L;
        var bestWgt = 0d;
        var result = "";
        foreach (var r in rs.MdcDrgWgt)
        {
            var treeDrg = (r.TreeDrg ?? "").Trim();
            if (treeDrg.Length == 0 || !set.Contains(treeDrg)) continue;

            var treeNo = r.TreeNo;
            var wgt = r.TreeWgt;
            if (ctx.Mdc == "15")
            {
                if (treeNo < bestNo) { bestNo = treeNo; bestWgt = wgt; result = treeDrg; }
                continue;
            }
            if (wgt == 0d) wgt = (double)ctx.MedAmt / r.AvgExp;   // legacy:零權重以平均費用回推
            if (wgt > bestWgt || (wgt == bestWgt && treeNo < bestNo))
            {
                bestNo = treeNo;
                bestWgt = wgt;
                result = treeDrg;
            }
        }
        return result;
    }

    // tree_yyy 前段:同義 DRG 收斂,讓較細的代碼併入較粗的代碼。
    private static void Remap(string[] d)
    {
        for (var i = 0; i < d.Length; i++)
        {
            if (d[i] is "10402" or "10403") ReplaceAll(d, ["10409", "10410", "10404"], d[i]);
            else if (d[i] is "10409" or "10410") ReplaceAll(d, ["10404"], d[i]);

            if (d[i] is "10502" or "10503") ReplaceAll(d, ["10509", "10510", "10504"], d[i]);
            else if (d[i] is "10509" or "10510") ReplaceAll(d, ["10504"], d[i]);

            if (d[i] is "47701" or "47702" or "47703" or "47704")
                ReplaceAll(d, ["46801", "46802", "46803", "46804"], d[i]);

            if (d[i] is "28901" or "28902") ReplaceAll(d, ["290"], d[i]);
        }
    }

    private static void ReplaceAll(string[] d, string[] targets, string with)
    {
        for (var k = 0; k < d.Length; k++)
            if (Array.IndexOf(targets, d[k]) >= 0) d[k] = with;
    }
}
