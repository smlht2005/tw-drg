using Microsoft.Data.Sqlite;

// Stage 2:讀 migration/*.tsv → 建立 icd10.sqlite。
// TSV 由 scripts/export_sdf.ps1 自 icd10.sdf 匯出(NULL=\N、UTF-8、首列為欄名)。
// 數值欄以 SQLite 欄位親和性(INTEGER/REAL)於 INSERT 時自動轉換,綁定字串即可。

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var migDir = Path.Combine(root, "migration");
var dbPath = args.Length > 1 ? args[1] : Path.Combine(root, "icd10.sqlite");

// table -> (column, sqliteType)
var schema = new Dictionary<string, (string Col, string Type)[]>
{
    ["RDDT_XICD_V"] =
    [
        ("ICD_OP_TYPE", "TEXT"), ("ICD_CODE", "TEXT"), ("SEX_CHK", "TEXT"), ("AGE_CHK", "TEXT"),
        ("PRM_ICD_CHK", "TEXT"), ("OR_NOR", "TEXT"), ("SEX_NO", "TEXT"),
    ],
    ["RDDT_ECC_V"] = [("ICD_NO_1", "TEXT"), ("TYPE", "TEXT"), ("ICD_NO_GROUP", "TEXT")],
    ["RDDT_ECC_GROUP_V"] = [("ICD_NO_GROUP", "TEXT"), ("ICD_NO", "TEXT")],
    ["RDDT_PDX_MDC_V"] = [("ICD_NO", "TEXT"), ("MDC_CODE", "TEXT"), ("CC", "TEXT"), ("OP", "TEXT")],
    ["RDDT_MDC_DRGWGT_V"] =
    [
        ("TREE_MDC_NO", "TEXT"), ("TREE_NO", "INTEGER"), ("TREE_DRG", "TEXT"), ("TREE_WGT", "REAL"),
        ("DEP", "TEXT"), ("AVG_EXP", "INTEGER"), ("COMBO_NO", "TEXT"), ("CC_MARK", "TEXT"),
    ],
};

if (File.Exists(dbPath)) File.Delete(dbPath);
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

using (var pragma = conn.CreateCommand())
{
    pragma.CommandText = "PRAGMA journal_mode=OFF; PRAGMA synchronous=OFF;";
    pragma.ExecuteNonQuery();
}

foreach (var (table, cols) in schema)
{
    var tsv = Path.Combine(migDir, table + ".tsv");
    if (!File.Exists(tsv))
    {
        Console.WriteLine($"SKIP {table}: {tsv} 不存在");
        continue;
    }

    using (var create = conn.CreateCommand())
    {
        var colDefs = string.Join(", ", cols.Select(c => $"[{c.Col}] {c.Type}"));
        create.CommandText = $"DROP TABLE IF EXISTS [{table}]; CREATE TABLE [{table}] ({colDefs});";
        create.ExecuteNonQuery();
    }

    using var tx = conn.BeginTransaction();
    using var insert = conn.CreateCommand();
    insert.Transaction = tx;
    var ph = string.Join(", ", cols.Select((_, i) => $"@p{i}"));
    insert.CommandText = $"INSERT INTO [{table}] VALUES ({ph})";
    var ps = cols.Select((_, i) => insert.CreateParameter()).ToArray();
    for (var i = 0; i < ps.Length; i++) { ps[i].ParameterName = $"@p{i}"; insert.Parameters.Add(ps[i]); }

    using var reader = new StreamReader(tsv);
    reader.ReadLine();   // 跳過 header
    var n = 0;
    string? line;
    while ((line = reader.ReadLine()) is not null)
    {
        var f = line.Split('\t');
        for (var i = 0; i < cols.Length; i++)
            ps[i].Value = f[i] == "\\N" ? DBNull.Value : f[i];
        insert.ExecuteNonQuery();
        n++;
    }
    tx.Commit();
    Console.WriteLine($"{table}\t{n} rows");
}

Console.WriteLine($"DONE -> {dbPath}");
