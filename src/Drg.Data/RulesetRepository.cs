using Dapper;
using Drg.Core.Ruleset;

namespace Drg.Data;

public interface IRulesetRepository
{
    GroupingRuleset Load(string version);
}

/// <summary>
/// 載入版次化 Tw-DRG 規則集(對應 legacy RDDT_* 視圖/表),全表載入記憶體(對應 legacy rddi1000_reload_db)。
/// 查詢為常數 SQL、無使用者輸入串接(原則 III)。
/// </summary>
public sealed class RulesetRepository(IDbConnectionFactory factory) : IRulesetRepository
{
    public GroupingRuleset Load(string version)
    {
        using var conn = factory.Create();
        conn.Open();
        return new GroupingRuleset
        {
            Version = version,
            PdxMdc = conn.Query<PdxMdc>(
                "SELECT ICD_NO AS IcdNo, MDC_CODE AS MdcCode, CC AS Cc, OP AS Op FROM RDDT_PDX_MDC_V").AsList(),
            MdcDrgWgt = conn.Query<MdcDrgWgt>(
                "SELECT TREE_MDC_NO AS TreeMdcNo, TREE_DRG AS TreeDrg, TREE_NO AS TreeNo, TREE_WGT AS TreeWgt, " +
                "AVG_EXP AS AvgExp, DEP AS Dep, COMBO_NO AS ComboNo FROM RDDT_MDC_DRGWGT_V").AsList(),
            Xicd = conn.Query<Xicd>(
                "SELECT ICD_OP_TYPE AS IcdOpType, ICD_CODE AS IcdCode, SEX_CHK AS SexChk, " +
                "AGE_CHK AS AgeChk, PRM_ICD_CHK AS PrmIcdChk, OR_NOR AS OrNor, SEX_NO AS SexNo " +
                "FROM RDDT_XICD_V").AsList(),
            Ecc = conn.Query<Ecc>(
                "SELECT TYPE AS Type, ICD_NO_1 AS IcdNo1, ICD_NO_GROUP AS IcdNoGroup FROM RDDT_ECC_V").AsList(),
            EccGroup = conn.Query<EccGroup>(
                "SELECT ICD_NO AS IcdNo, ICD_NO_GROUP AS IcdNoGroup FROM RDDT_ECC_GROUP_V").AsList(),
        };
    }
}
