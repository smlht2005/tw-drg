// Legacy-oracle 語料產生器:直接引用 rddt_lib.dll(.NET Framework 4.0 / x86),
// 自 icd10.sdf 抽「每個 MDC 一個真實主診斷」與少數外科案,跑 rddi1000_main,
// 擷取真實 DRG/MDC/CC/ERR 輸出 → tests/Drg.Parity.Tests/GoldenCorpus/legacy_oracle.json。
//
// 編譯/執行見 tools/LegacyOracle/build_oracle.ps1(須以 framework csc + /platform:x86)。
// 必須與 rddt_lib.dll / IISILib.dll / System.Data.SqlServerCe.dll / sqlce*40.dll 同目錄執行,
// 且旁附 DRGOracle.exe.config(DB00 指向 icd10.sdf、dbtype=sqlce)。

using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Globalization;
using System.IO;
using System.Text;
using rddi_lib;

internal static class Oracle
{
    private const string Root = @"C:\med\S_DRGService_3420";

    private static DateTime D(string yyyyMMdd)
    {
        return DateTime.ParseExact(yyyyMMdd, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static int Main()
    {
        var cs = "Data Source=" + Root + @"\icd10.sdf;Max Database Size=2048;";

        // 1) 每個 MDC 取一個不含 '+' 的真實主診斷
        var cases = new List<Case>();
        using (var conn = new SqlCeConnection(cs))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT MDC_CODE, MIN(ICD_NO) FROM RDDT_PDX_MDC_V " +
                "WHERE ICD_NO NOT LIKE '%+%' GROUP BY MDC_CODE ORDER BY MDC_CODE";
            using (var rd = cmd.ExecuteReader())
                while (rd.Read())
                {
                    var mdc = rd.GetString(0).Trim();
                    var icd = rd.GetString(1).Trim();
                    cases.Add(new Case { Name = "MDC" + mdc + "-MED", Cm = new List<string> { icd } });
                }
        }

        // 2) 初始化 grouper(載入參考表)
        var rddi = new rddi0001();
        rddi.rddi1000_reload_db();

        // 3) 逐案跑 legacy,擷取輸出
        foreach (var c in cases) Run(rddi, c);

        // 4) 輸出 JSON
        var outPath = Path.Combine(Root, @"tests\Drg.Parity.Tests\GoldenCorpus\legacy_oracle.json");
        File.WriteAllText(outPath, ToJson(cases), new UTF8Encoding(false));
        Console.WriteLine("cases: " + cases.Count + " -> " + outPath);
        return 0;
    }

    private static void Run(rddi0001 rddi, Case c)
    {
        var cm = new string[20];
        var op = new string[20];
        for (var i = 0; i < 20; i++) { cm[i] = ""; op[i] = ""; }
        for (var i = 0; i < c.Cm.Count && i < 20; i++) cm[i] = c.Cm[i];
        for (var i = 0; i < c.Op.Count && i < 20; i++) op[i] = c.Op[i];

        // 固定人口學:46 歲男性、2026 住院、正常出院(轉歸 1)
        DateTime feeYm = D("20260101"), birthday = D("19800101"), applS = D("20260105"),
                 inDate = D("20260105"), outDate = D("20260110"), childB = D("19800101");
        string sex = "M", part = "001", tran = "1";
        string err = "", ccMark = "", mdc = "", ccCode = new string('0', 20), drg = "";
        long exp = 100000;
        int age = 46;

        rddi.rddi1000_main(ref feeYm, ref birthday, ref sex, ref applS, ref inDate, ref outDate,
            ref part, ref childB, ref cm, ref op, exp, ref tran, ref err, age,
            ref ccMark, ref mdc, ref ccCode, ref drg);

        c.Drg = drg; c.Mdc = mdc; c.CcMark = ccMark; c.Err = err;
    }

    private static string ToJson(List<Case> cases)
    {
        var sb = new StringBuilder();
        sb.Append("[\n");
        for (var i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            sb.Append("  {");
            sb.Append("\"Name\":").Append(Q(c.Name)).Append(",");
            sb.Append("\"Cm\":").Append(Arr(c.Cm)).Append(",");
            sb.Append("\"Op\":").Append(Arr(c.Op)).Append(",");
            sb.Append("\"Sex\":\"M\",\"Birthday\":\"19800101\",\"InDate\":\"20260105\",\"OutDate\":\"20260110\",");
            sb.Append("\"PartCode\":\"001\",\"TranCode\":\"1\",\"MedAmt\":100000,\"Age\":46,");
            sb.Append("\"Drg\":").Append(Q(c.Drg)).Append(",");
            sb.Append("\"Mdc\":").Append(Q(c.Mdc)).Append(",");
            sb.Append("\"CcMark\":").Append(Q(c.CcMark)).Append(",");
            sb.Append("\"Err\":").Append(Q(c.Err));
            sb.Append(i < cases.Count - 1 ? "},\n" : "}\n");
        }
        sb.Append("]\n");
        return sb.ToString();
    }

    private static string Q(string s)
    {
        if (s == null) s = "";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string Arr(List<string> xs)
    {
        var sb = new StringBuilder("[");
        for (var i = 0; i < xs.Count; i++) { if (i > 0) sb.Append(","); sb.Append(Q(xs[i])); }
        sb.Append("]");
        return sb.ToString();
    }

    private sealed class Case
    {
        public string Name;
        public List<string> Cm = new List<string>();
        public List<string> Op = new List<string>();
        public string Drg = "", Mdc = "", CcMark = "", Err = "";
    }
}
