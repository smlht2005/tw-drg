namespace Drg.Core.Engine;

/// <summary>drg_chk_yyy 等價(rddi0001 4386–4400):掃 drgCode[beg..end) 的小型比對工具。
/// 主編排唯一呼叫處為 drg_chk_yyy(H_TEMP_DRG, "", 0, 2, 1)——判定 TEMP_DRG[0..1] 是否「已產出任何 DRG」
/// (chkType=1:出現任一不等於 target 者即回 2)。回 0 表整段皆等於 target(此例即皆為空)。</summary>
public static class DrgCheck
{
    public static int Check(string[] drgCode, string target, int beg, int end, int chkType)
    {
        for (var i = beg; i < end; i++)
        {
            if (drgCode[i] == target && chkType == 0) return 2;
            if (drgCode[i] != target && chkType == 1) return 2;
        }
        return 0;
    }
}
