using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>多手術 '+' 組合展開,移植自 rddi0001 Op_Code_Rtn_yyy(實際委派 _B8_2)。
/// 掃 RDDT_XICD 中 ICD_CODE 含 '+' 的組合碼;當其各元件能在現有手術碼表(icdopTab[0..19] 已填)
/// 以相異槽位全數命中時,將該組合碼展開寫入 icdopTab 的 20+ 區。就地修改 icdopTab(長度須 40)。
/// 回傳 true 表至少展開一筆(legacy v_op_wk == "*",後續 OR/UNF 判定沿用)。
/// 注意:legacy 過濾僅以 ICD_CODE 含 '+',不限 ICD_OP_TYPE(_B8 變體有限但未被呼叫)。</summary>
public static class OpCodeExpander
{
    public static bool Expand(string[] icdopTab, GroupingRuleset rs)
    {
        var vOpWk = false;
        var num = 0;
        foreach (var combo in rs.Xicd.Where(x => (x.IcdCode ?? "").Contains('+')))
        {
            var code = combo.IcdCode ?? "";
            var components = code.Split('+');
            var work = (string[])icdopTab.Clone();
            var present = work.Count(s => !string.IsNullOrWhiteSpace(s));
            if (present < components.Length) continue;

            var used = new List<int>();
            foreach (var text in components)
            {
                for (var idx = 0; idx < work.Length; idx++)
                {
                    if (!string.IsNullOrWhiteSpace(work[idx]) && !used.Contains(idx) && text == work[idx])
                    {
                        work[idx] = "";
                        used.Add(idx);
                        break;
                    }
                }
            }
            if (used.Count == components.Length)
            {
                vOpWk = true;
                icdopTab[num + 20] = code;
                num++;
            }
        }
        return vOpWk;
    }
}
