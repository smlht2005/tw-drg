using Dapper;
using Drg.Core.Engine;
using Drg.Core.Ruleset;

namespace Drg.Data;

/// <summary>逐筆載入 combo_drg 候選 join 列(對應 legacy prepareDRG_XICD / RDDT_MDC_DRG_XICD_00)。
/// 參數化查詢、無使用者輸入串接(原則 III)。批次上限小(SC-004),per-record 查詢成本可接受。
/// 接縫 <see cref="ICandidateSource"/> 定義於 Core,供主編排 DrgGrouper 依賴(不反向相依 Drg.Data)。</summary>
public sealed class CandidateRepository(IDbConnectionFactory factory) : ICandidateSource
{
    // 全選原始欄位(保留 SQLite decltype → Dapper 正確映射為 string;勿用 CASE/CAST 等運算式欄位,
    // 否則 Microsoft.Data.Sqlite 對無 decltype 欄位預設回 byte[],positional record 物化失敗)。
    // CC_MARK ''/NULL→'X'、AGE_MARK ''/NULL→'N' 的 legacy 預設改於 C# 後處理(Default)。
    private const string Cols =
        "TREE_DRG AS TreeDrg, TREE_MDC_NO AS TreeMdcNo, TREE_NO AS TreeNo, TREE_WGT AS TreeWgt, " +
        "DEP AS Dep, COMBO_NO AS ComboNo, LIVE_MARK AS LiveMark, ITEM_TYPE AS ItemType, " +
        "CC_MARK AS CcMark, " +
        "AGE_18Y AS Age18Y, AGE_36Y AS Age36Y, AGE_41Y AS Age41Y, AGE_5Y_65Y AS Age5Y65Y, " +
        "AGE_2Y AS Age2Y, AGE_28D AS Age28D, AGE_2D AS Age2D, AGE_MARK AS AgeMark, " +
        "ICD_CODE AS IcdCode, ICD_CODE_PLUS AS IcdCodePlus";

    public IReadOnlyList<MdcDrgXicd> LoadForMdc(string mdc, IReadOnlyCollection<string> codes)
    {
        var codeList = Normalize(codes);
        using var conn = factory.Create();
        conn.Open();

        var rows = conn.Query<MdcDrgXicd>(
            $"SELECT {Cols} FROM RDDT_MDC_DRG_XICD_V " +
            "WHERE ICD_CODE = '+' OR (TREE_MDC_NO = @mdc AND (ICD_CODE = '*' OR ICD_CODE IN @codes))",
            new { mdc, codes = codeList }).AsList();

        rows.AddRange(conn.Query<MdcDrgXicd>(
            $"SELECT {Cols} FROM RDDT_MDC_DRG_XICD_NOTIN_V WHERE TREE_MDC_NO = @mdc", new { mdc }));

        rows.AddRange(conn.Query<MdcDrgXicd>(
            $"SELECT {Cols} FROM RDDT_MDC_DRG_XICD_UN_V"));

        return rows.Select(Default).ToList();
    }

    public IReadOnlyList<MdcDrgXicd> Load00(IReadOnlyCollection<string> codes)
    {
        var codeList = Normalize(codes);
        using var conn = factory.Create();
        conn.Open();
        return conn.Query<MdcDrgXicd>(
            $"SELECT {Cols} FROM RDDT_MDC_DRG_XICD_00_V " +
            "WHERE ICD_CODE = '+' OR ICD_CODE = '*' OR ICD_CODE IN @codes",
            new { codes = codeList }).Select(Default).ToList();
    }

    // legacy CASE 預設:CC_MARK ''/NULL→'X'、AGE_MARK ''/NULL→'N'(CandidateFilter 依此判讀)。
    private static MdcDrgXicd Default(MdcDrgXicd r) => r with
    {
        CcMark = string.IsNullOrEmpty(r.CcMark) ? "X" : r.CcMark,
        AgeMark = string.IsNullOrEmpty(r.AgeMark) ? "N" : r.AgeMark,
    };

    // 非空白碼去重;空集合時給哨兵避免 SQL IN () 失效。
    private static string[] Normalize(IReadOnlyCollection<string> codes)
    {
        var list = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToArray();
        return list.Length == 0 ? ["__none__"] : list;
    }
}
