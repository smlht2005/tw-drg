using Drg.Core.Models;
using Drg.Core.Ruleset;

namespace Drg.Core.Engine;

/// <summary>單筆 DRG 分組主編排,移植自 rddi0001 rddi1000_main(604–979)。串接已建模組:
/// AgeCalculator/SexResolver → Icd10CmCheck(-1) → EccCheck → MdcCheck → SentinelCheck(哨兵早退)
/// → OP 掃描 + OpCodeExpander → 00 組 / 外科 / 內科(DEPP)/ UN 退路(各以 ComboDrg.Generate
/// + TreeSelector 收斂)→ 心臟 SP_OP 特例 → 最終 tree。候選列經 <see cref="ICandidateSource"/>
/// 逐筆載入(Core 不相依 Drg.Data)。combo_drg_yyy 的 v_DRG_1 收斂 = 候選清單交 TreeSelector。</summary>
public sealed class DrgGrouper(ICandidateSource candidates) : IDrgGrouper
{
    // prepareDRG_XICD:這些 MDC 不載候選表(switch 早退,4564–4570)。
    private static readonly string[] NoCandidateMdc = ["19", "20", "25", "26", "27", "28"];
    private static readonly string[] NothingInArray = ["NothingInArray"];

    public CodingResult Group(ClaimEncounter claim, GroupingRuleset rs)
    {
        var ctx = BuildContext(claim);
        var result = new CodingResult { RowNum = claim.RowNum };

        if (Icd10CmCheck.Run(ctx, rs) != 0)   // -1:硬性 ICD-10-CM 檢核失敗
        {
            // legacy -1 路徑 ecc_chk 未跑,H_CC_MARK 仍為 ""(非 GroupingContext 預設 "N")。
            result.CcMark = "";
            result.CcCode = new string(ctx.CcCode);
            result.ErrNo = new string(ctx.ErrCode).Trim();
            return result;
        }

        EccCheck.Run(ctx, rs);
        MdcCheck.Run(ctx, rs);
        result.CcMark = ctx.CcMark;
        result.CcCode = new string(ctx.CcCode);
        result.ErrNo = new string(ctx.ErrCode).Trim();

        var sentinel = SentinelCheck.Run(ctx, rs);
        if (sentinel is not null) { result.Mdc = ctx.Mdc; result.Drg = sentinel; return result; }

        // ---- OP 掃描(726–778):建 ICDOP_TAB、偵測 OR 手術、多手術展開 ----
        var icdop = new string[40];
        var vIcdNoOp = "";
        var opCount = 0;
        for (var i = 0; i < 20; i++)
        {
            if (string.IsNullOrWhiteSpace(ctx.OpCodes[i])) continue;
            icdop[i] = ctx.OpCodes[i];
            opCount++;
            if (IsType2ChkX(ctx.OpCodes[i].Trim(), rs)) vIcdNoOp = "OR";
        }
        var vOpWk = false;
        if (opCount > 1)
        {
            vOpWk = OpCodeExpander.Expand(icdop, rs);
            for (var i = 20; i < 40; i++)
                if (!string.IsNullOrWhiteSpace(icdop[i]) && IsType2ChkX(icdop[i].Trim(), rs)) vIcdNoOp = "OR";
        }

        // ---- 候選載入(switchViewByMDC→prepareDRG_XICD + Merge 00)----
        var codes = Codes(ctx.CmCodes, ctx.OpCodes);
        var pool = new List<MdcDrgXicd>();
        if (!NoCandidateMdc.Contains(ctx.Mdc)) pool.AddRange(candidates.LoadForMdc(ctx.Mdc, codes));
        var cand00 = candidates.Load00(codes);

        var tempDrg = new[] { "", "", "", "", "" };
        var vMdc2 = ""; var vDrg2 = ""; var vDrg3 = "";
        ctx.DepFlag = "P";

        // ---- 00 組(779–792):命中且非 482xx/483xx → 直接回傳 ----
        var vDrg1 = ComboThenTree(cand00, icdop, ctx, rs, "00", 1);
        if (vDrg1 != "")
        {
            if (vDrg1 is not ("48201" or "48202" or "48301" or "48302"))
            { result.Mdc = "00"; result.Drg = vDrg1; return result; }
            vMdc2 = "00"; vDrg2 = vDrg1;
        }

        // ---- 外科(793–799):有 OR 手術才試 ----
        if (vIcdNoOp == "OR")
            tempDrg[0] = ComboThenTree(pool, icdop, ctx, rs, ctx.Mdc, 1);

        // ---- MDC14 產科(800–803):mdc_icd9cm_yyy(opflag=2)----
        if (ctx.Mdc == "14" && tempDrg[0] == "")
            tempDrg[1] = ComboThenTree(pool, icdop, ctx, rs, ctx.Mdc, 2);

        // ---- drg_chk + UN 退路(807–819)----
        if (DrgCheck.Check(tempDrg, "", 0, 2, 1) != 2)
        {
            if (ctx.Mdc is not ("15" or "17" or "18" or "22" or "23"))
            {
                // UNF_MDC_99_CHK_yyy:重掃 ORNORY 手術 → combo_drg("UN")
                Array.Clear(icdop, 0, 20);
                var or = "";
                for (var i = 0; i < 20; i++)
                {
                    if (string.IsNullOrWhiteSpace(ctx.OpCodes[i])) continue;
                    if (IsOrnorY(ctx.OpCodes[i].Trim(), rs)) { or = "OR"; icdop[i] = ctx.OpCodes[i]; }
                }
                if (or == "OR" || vOpWk)
                    tempDrg[0] = ComboThenTree(pool, icdop, ctx, rs, "UN", 1);
            }
        }
        else if (ctx.Mdc == "15") vDrg3 = vDrg1;

        // ---- DEPP 過濾(824–863):外科分群存在? 否則退內科 ----
        var tset = new HashSet<string> { tempDrg[0].Trim(), tempDrg[1].Trim(), tempDrg[2].Trim() };
        var orpCnt = rs.MdcDrgWgt.Count(w =>
            (w.Dep ?? "").Trim().ToUpperInvariant() == "P"
            && (w.TreeMdcNo ?? "").Trim() != "15"
            && tset.Contains((w.TreeDrg ?? "").Trim()));

        if (orpCnt == 0)
        {
            Array.Clear(icdop, 0, icdop.Length);
            ctx.DepFlag = "M";
            var nor = "";
            for (var i = 0; i < 20; i++)
            {
                if (ctx.OpCodes[i] == "") continue;
                if (IsOrnorn(ctx.OpCodes[i].Trim(), rs)) { nor = "NOR"; icdop[i] = ctx.OpCodes[i]; }
            }
            if (nor == "NOR")
                tempDrg[0] = ComboThenTree(pool, icdop, ctx, rs, ctx.Mdc, 1);
            tempDrg[1] = ComboThenTree(pool, icdop, ctx, rs, ctx.Mdc, 2);   // mdc_icd9cm_yyy
        }

        // ---- 組裝 + 最終 tree(864–875)----
        if (ctx.Mdc != "15") tempDrg[2] = vDrg2;
        tempDrg[3] = vDrg3;
        vDrg1 = TreeSelector.Select(tempDrg, ctx, rs);

        var vMdc1 = ctx.Mdc;
        if (vDrg2 == vDrg1) vMdc1 = vMdc2;

        // ---- 心臟 SP_OP 特例(876–964)----
        if (AgeCalculator.Days(ctx.OutDate, "2021/01/01") >= 0 && vDrg1 is "11201" or "11602")
            vDrg1 = CardiacRefine(vDrg1, ctx.OpCodes);

        result.Mdc = vMdc1;
        result.Drg = vDrg1;   // 空 = legacy -2(無法分群)
        return result;
    }

    // combo_drg_yyy 等價:設 FilterMdc/OpFlag → 候選按 TREE_MDC_NO 分流 → ComboDrg.Generate → TreeSelector 收斂。
    private static string ComboThenTree(
        IReadOnlyList<MdcDrgXicd> pool, string[] op, GroupingContext ctx, GroupingRuleset rs,
        string filterMdc, int opFlag)
    {
        ctx.FilterMdc = filterMdc;
        ctx.OpFlag = opFlag;
        var scoped = pool.Where(c => (c.TreeMdcNo ?? "").Trim() == filterMdc).ToList();
        // opflag=1 時 in_ICDOP_TAB 空會以 'NothingInArray' 哨兵入 SQL(不丟例外);對齊 ComboDrg 守門。
        var opArg = opFlag == 1 && !op.Any(o => !string.IsNullOrWhiteSpace(o)) ? NothingInArray : op;
        var list = new ComboDrg().Generate(scoped, ctx.CmCodes, opArg, ctx, rs);
        return TreeSelector.Select(list, ctx, rs);
    }

    // ---- 心臟/ECMO 細分(11201/11601/11602/11202),op_no=20 哨兵以 break 外迴圈表達 ----
    private static string CardiacRefine(string drg, string[] op)
    {
        for (var i = 0; i < 20; i++)
        {
            if (op[i].Length == 0) continue;
            if (Array.IndexOf(SpOp, op[i]) >= 0) { drg = "11602"; break; }
            var hit1 = Array.IndexOf(SpOp1, op[i]) >= 0;
            if (hit1)
            {
                if (drg != "11601") { drg = "11601"; continue; }
                drg = "11602"; break;   // drg=="11601"
            }
        }
        if (drg == "11601")
            for (var i = 0; i < 20; i++)
                if (op[i].Length > 0 && Array.IndexOf(SpOp2, op[i]) >= 0) { drg = "11602"; break; }

        if (drg == "11201")
        {
            var cnt = 0;
            for (var i = 0; i < 20; i++)
            {
                if (op[i].Length == 0) continue;
                if (Array.IndexOf(SpOp5, op[i]) >= 0) { drg = "11201"; break; }
                if (Array.IndexOf(SpOp6, op[i]) >= 0)
                {
                    cnt++;
                    if (cnt > 1) { drg = "11201"; break; }
                    drg = "11202";
                    break;
                }
            }
        }
        return drg;
    }

    private static GroupingContext BuildContext(ClaimEncounter c)
    {
        var inDate = Slash(c.InDate);
        var is903 = c.PartCode == "903" && !string.IsNullOrWhiteSpace(c.ChildBirthday)
                    && Slash(c.ChildBirthday!) != "0000/00/00";
        var baseDate = is903 ? Slash(c.ChildBirthday!) : Slash(c.Birthday);
        // appl_s_date 未在輸入模型;沿用 OracleParityTests 慣例:入院日缺時以出生日當 now(年齡 0)。
        var now = inDate != "0000/00/00" ? inDate : baseDate;

        return new GroupingContext
        {
            CmCodes = Pad(c.CmCodes),
            OpCodes = Pad(c.OpCodes),
            Sex = SexResolver.Resolve(c.Sex, c.Pid),
            PartMark = c.PartCode,
            OutDate = Slash(c.OutDate),
            TranCode = c.TranCode,
            MedAmt = (int)c.MedAmt,
            Birthday = baseDate,
            ChildBirthday = is903 ? Slash(c.ChildBirthday!) : "",
            Ages = AgeCalculator.Years(now, baseDate),
            Months = AgeCalculator.Months(now, baseDate),
            Days = AgeCalculator.Days(now, baseDate),
        };
    }

    private static string Slash(string ymd) =>
        string.IsNullOrWhiteSpace(ymd) || ymd.Length != 8
            ? "0000/00/00"
            : $"{ymd[..4]}/{ymd.Substring(4, 2)}/{ymd.Substring(6, 2)}";

    private static string[] Pad(string[] codes)
    {
        var a = new string[20];
        for (var i = 0; i < 20; i++) a[i] = i < codes.Length ? (codes[i] ?? "").Trim() : "";
        return a;
    }

    private static string[] Codes(string[] cm, string[] op) =>
        cm.Concat(op).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct().ToArray();

    private static bool Type2(Xicd x) =>
        (x.IcdOpType ?? "").Trim() == "2" && (x.PrmIcdChk is null || x.PrmIcdChk.Trim().ToUpperInvariant() != "X");

    private static string OrNor(Xicd x) => (x.OrNor ?? "").Trim().ToUpperInvariant();

    private static bool IsType2ChkX(string code, GroupingRuleset rs) =>
        rs.Xicd.Any(x => Type2(x) && OrNor(x) is "Y" or "N1" && (x.IcdCode ?? "").Trim() == code);

    private static bool IsOrnorY(string code, GroupingRuleset rs) =>
        rs.Xicd.Any(x => Type2(x) && OrNor(x) == "Y" && (x.IcdCode ?? "").Trim() == code);

    private static bool IsOrnorn(string code, GroupingRuleset rs) =>
        rs.Xicd.Any(x => Type2(x) && OrNor(x) is "N" or "N1" && (x.IcdCode ?? "").Trim() == code);

    private static readonly string[] SpOp =
    [
        "0271046", "027104Z", "02710D6", "02710DZ", "02710T6", "02710TZ", "0271346", "027134Z", "0271356", "027135Z",
        "0271366", "027136Z", "0271376", "027137Z", "02713D6", "02713DZ", "02713E6", "02713EZ", "02713F6", "02713FZ",
        "02713G6", "02713GZ", "02713T6", "02713TZ", "0271446", "027144Z", "0271456", "027145Z", "0271466", "027146Z",
        "0271476", "027147Z", "02714D6", "02714DZ", "02714E6", "02714EZ", "02714F6", "02714FZ", "02714G6", "02714GZ",
        "02714T6", "02714TZ", "0272046", "027204Z", "02720D6", "02720DZ", "02720T6", "02720TZ", "0272346", "027234Z",
        "0272356", "027235Z", "0272366", "027236Z", "0272376", "027237Z", "02723D6", "02723DZ", "02723E6", "02723EZ",
        "02723F6", "02723FZ", "02723G6", "02723GZ", "02723T6", "02723TZ", "0272446", "027244Z", "0272456", "027245Z",
        "0272466", "027246Z", "0272476", "027247Z", "02724D6", "02724DZ", "02724E6", "02724EZ", "02724F6", "02724FZ",
        "02724G6", "02724GZ", "02724T6", "02724TZ", "0273046", "027304Z", "02730D6", "02730DZ", "02730T6", "02730TZ",
        "0273346", "027334Z", "0273356", "027335Z", "0273366", "027336Z", "0273376", "027337Z", "02733D6", "02733DZ",
        "02733E6", "02733EZ", "02733F6", "02733FZ", "02733G6", "02733GZ", "02733T6", "02733TZ", "0273446", "027344Z",
        "0273456", "027345Z", "0273466", "027346Z", "0273476", "027347Z", "02734D6", "02734DZ", "02734E6", "02734EZ",
        "02734F6", "02734FZ", "02734G6", "02734GZ", "02734T6", "02734TZ", "02H13DZ", "02H13YZ", "02H23DZ", "02H23YZ",
        "02H33DZ", "02H33YZ",
    ];

    private static readonly string[] SpOp1 =
    [
        "0270046", "027004Z", "02700D6", "02700DZ", "02700T6", "02700TZ", "0270346", "027034Z", "02703D6", "02703DZ",
        "02703T6", "02703TZ", "0270446", "027044Z", "02704D6", "02704DZ", "02704T6", "02704TZ", "0270356", "027035Z",
        "0270366", "027036Z", "0270376", "027037Z", "02703E6", "02703EZ", "02703F6", "02703FZ", "02703G6", "02703GZ",
        "0270456", "027045Z", "0270466", "027046Z", "0270476", "027047Z", "02704E6", "02704EZ", "02704F6", "02704FZ",
        "02704G6", "02704GZ", "02H03DZ", "02H03YZ",
    ];

    private static readonly string[] SpOp2 =
    [
        "0200001", "02703Z6", "02703ZZ", "02704Z6", "02704ZZ", "02713Z6", "02713ZZ", "02714Z6", "02714ZZ", "02723Z6",
        "02723ZZ", "02724Z6", "02724ZZ", "02733Z6", "02733ZZ", "02734Z6", "02734ZZ", "02C03Z6", "02C03Z7", "02C03ZZ",
        "02C04Z6", "02C04ZZ", "02C13Z6", "02C13Z7", "02C13ZZ", "02C14Z6", "02C14ZZ", "02C23Z6", "02C23Z7", "02C23ZZ",
        "02C24Z6", "02C24ZZ", "02C33Z6", "02C33Z7", "02C33ZZ", "02C34Z6", "02C34ZZ",
    ];

    private static readonly string[] SpOp5 =
    [
        "02713Z6", "02713ZZ", "02714Z6", "02714ZZ", "02723Z6", "02723ZZ", "02724Z6", "02724ZZ", "02733Z6", "02733ZZ",
        "02734Z6", "02734ZZ", "027F34Z", "027F3DZ", "027F3ZZ", "027F44Z", "027F4DZ", "027F4ZZ", "027G34Z", "027G3DZ",
        "027G3ZZ", "027G44Z", "027G4DZ", "027G4ZZ", "027H34Z", "027H3DZ", "027H3ZZ", "027H44Z", "027H4DZ", "027H4ZZ",
        "027J34Z", "027J3DZ", "027J3ZZ", "027J44Z", "027J4DZ", "027J4ZZ", "02C13Z6", "02C13Z7", "02C13ZZ", "02C14Z6",
        "02C14ZZ", "02C23Z6", "02C23Z7", "02C23ZZ", "02C24Z6", "02C24ZZ", "02C33Z6", "02C33Z7", "02C33ZZ", "02C34Z6",
        "02C34ZZ",
    ];

    private static readonly string[] SpOp6 =
        ["02C03ZZ", "02C04ZZ", "02703ZZ", "02703Z6", "02704ZZ", "02704Z6", "0200001", "02C03Z6", "02C03Z7", "02C04Z6"];
}
