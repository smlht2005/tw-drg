# 參考資料遷移:icd10.sdf(SQL CE)→ icd10.sqlite

legacy 的 `RDDT_*` 參考表存於 `icd10.sdf`(SQL Server Compact 4.0,196 MB)。`.NET 8`
無法直接讀取 SQL CE(其 native provider 為 x86 + .NET Framework),故以**兩階段**遷移到
SQLite,供 `Drg.Data` 以 `Microsoft.Data.Sqlite` 查詢。

> 衍生產物(`migration/`、`icd10.sqlite`)**不納版控**(見 `.gitignore`);需要時依下列步驟重建。

## Stage 1 — 由 .sdf 匯出 TSV(32 位元 Windows PowerShell)

SQL CE 4.0 native 為 x86,**必須**以 32 位元 PowerShell 執行:

```powershell
C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -NoProfile `
  -File C:\med\S_DRGService_3420\scripts\export_sdf.ps1
```

輸出 `migration\*.tsv`(UTF-8、首列欄名、`NULL` 以 `\N` 表示)。NULL 需保留——
`Xicd.SexNo`/`PdxMdc.Cc`/`LiveMark` 等的 null 與空字串在分組邏輯中語意不同。

## Stage 2 — 載入 TSV 建立 SQLite(.NET 8)

```powershell
dotnet run --project tools/Drg.Migrate -c Release -- C:\med\S_DRGService_3420
```

於專案根產生 `icd10.sqlite`。數值欄(`TREE_NO`/`AVG_EXP` → INTEGER、`TREE_WGT` → REAL)
以 SQLite 欄位親和性於 INSERT 時自動轉換;故 ruleset record 之對應欄用 `long`/`double`。

## 已遷移資料表

| 階段 | 表 | 列數 | 用途 |
|---|---|---|---|
| A | `RDDT_XICD_V` | 222,162 | ICD 驗證(Icd10CmCheck) |
| A | `RDDT_ECC_V` | 18,129 | CC/MCC(EccCheck) |
| A | `RDDT_ECC_GROUP_V` | 509,382 | CC/MCC 群組 |
| A | `RDDT_PDX_MDC_V` | 72,239 | 主診斷→MDC(MdcCheck) |
| A | `RDDT_MDC_DRGWGT_V` | 1,068 | DRG 權重(TreeSelector) |
| B(待做) | `RDDT_MDC_DRG_XICD_V` | 1,005,668 | combo_drg 候選 join 表 |
| B(待做) | `RDDT_MDC_DRG_XICD_00_V` / `_NOTIN_V` / `_UN_V` | 258,320 | combo_drg per-MDC 變體 |
| B(待做) | `RDDT_DRG_MDC02_V` | 3,036 | MDC02 特例 |

## 連線設定

`Drg.Data` 經 `DbConnectionFactory(DbProvider.Sqlite, "Data Source=…/icd10.sqlite")` 連線;
連線字串由環境變數提供(`DRG_DB_CONNECTION`),不內嵌(憲章原則 III)。
