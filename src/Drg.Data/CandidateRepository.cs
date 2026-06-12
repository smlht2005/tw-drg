using Dapper;
using Drg.Core.Engine;
using Drg.Core.Ruleset;

namespace Drg.Data;

/// <summary>逐筆載入 combo_drg 候選 join 列(對應 legacy prepareDRG_XICD / RDDT_MDC_DRG_XICD_00)。
/// 參數化查詢、無使用者輸入串接(原則 III)。批次上限小(SC-004),per-record 查詢成本可接受。
/// 接縫 <see cref="ICandidateSource"/> 定義於 Core,供主編排 DrgGrouper 依賴(不反向相依 Drg.Data)。</summary>
public sealed class CandidateRepository(IDbConnectionFactory factory) : ICandidateSource
{
    // 欄位別名對齊 MdcDrgXicd;CC_MARK ''/NULL→'X'、AGE_MARK ''/NULL→'N'(legacy CASE 預設)。
    private const string Cols =
        "TREE_DRG AS TreeDrg, TREE_MDC_NO AS TreeMdcNo, TREE_NO AS TreeNo, TREE_WGT AS TreeWgt, " +
        "DEP AS Dep, COMBO_NO AS ComboNo, LIVE_MARK AS LiveMark, ITEM_TYPE AS ItemType, " +
        "CASE WHEN CC_MARK IS NULL OR CC_MARK = '' THEN 'X' ELSE CC_MARK END AS CcMark, " +
        "AGE_18Y AS Age18Y, AGE_36Y AS Age36Y, AGE_41Y AS Age41Y, AGE_5Y_65Y AS Age5Y65Y, " +
        "AGE_2Y AS Age2Y, AGE_28D AS Age28D, AGE_2D AS Age2D, " +
        "CASE WHEN AGE_MARK IS NULL OR AGE_MARK = '' THEN 'N' ELSE AGE_MARK END AS AgeMark, " +
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

        return rows;
    }

    public IReadOnlyList<MdcDrgXicd> Load00(IReadOnlyCollection<string> codes)
    {
        var codeList = Normalize(codes);
        using var conn = factory.Create();
        conn.Open();
        return conn.Query<MdcDrgXicd>(
            $"SELECT {Cols} FROM RDDT_MDC_DRG_XICD_00_V " +
            "WHERE ICD_CODE = '+' OR ICD_CODE = '*' OR ICD_CODE IN @codes",
            new { codes = codeList }).AsList();
    }

    // 非空白碼去重;空集合時給哨兵避免 SQL IN () 失效。
    private static string[] Normalize(IReadOnlyCollection<string> codes)
    {
        var list = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToArray();
        return list.Length == 0 ? ["__none__"] : list;
    }
}
