# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Taiwan NHI (中央健康保險署) **DRG 編碼服務** — a batch tool that reads a hospital claims CSV, assigns each record a DRG / MDC code, and writes a result CSV. Despite the `S_DRGService` namespace and "Service" naming, it is a **WinForms desktop app** (`DRGICD10.exe`, originally VB.NET, assembly v3.4.20.0, x86, .NET Framework 4.0 Client Profile, by **IISI**). Entry point: `S_DRGService.DRGForm.Main`.

The folder root is a **binary deployment** — the shipped `.exe`, native DLLs, and the `icd10.sdf` reference database. There is no source project at the root and no git repo.

## Decompiled source — where the real code lives

The original source is not shipped. **C# decompilations** (via `ilspycmd`, ILSpy 8.x) are the canonical place to read logic. They are auto-generated and **not built** by anything here:
- `_decompiled\` — the app (`DRGICD10.exe`): UI + batch orchestration.
- `_decompiled_rddt_lib\` — `rddt_lib.dll`: the DRG grouping engine + typed reference DataSet.
- `_decompiled_IISILib\` — `IISILib.dll`: the `DBModels` data-access layer.

Regenerate with (`-p` = project, `-o` = output dir):
```powershell
ilspycmd "C:\med\S_DRGService_3420\DRGICD10.exe" -p -o "C:\med\S_DRGService_3420\_decompiled"
ilspycmd "C:\med\S_DRGService_3420\rddt_lib.dll" -p -o "C:\med\S_DRGService_3420\_decompiled_rddt_lib"
ilspycmd "C:\med\S_DRGService_3420\IISILib.dll" -p -o "C:\med\S_DRGService_3420\_decompiled_IISILib"
```
Notes: `ilspycmd` is installed as a global dotnet tool (`C:\Users\<user>\.dotnet\tools\ilspycmd.exe`). ILSpy only emits **C#**, not the original VB.NET — logic is faithful but syntax/idioms (VB runtime calls like `Operators.CompareString`, `Conversions.ToString`, `ProjectData.SetProjectError`, on-error-goto state machines) appear as `Microsoft.VisualBasic.*` calls. The decompiled `DRGForm.cs`/`DRGService.cs` `InitializeComponent` is reconstructed, not the original designer file. Antivirus may flag reflection/decompilation of the exe; this is a false positive on a self-owned binary.

## Architecture (read `_decompiled\S_DRGService\`)

Three source files matter:
- **`DRGForm.cs`** — entry point + launcher UI. Parses command-line args (`CmdArgOrder`, and a `chkCmd` mode enum — `chkCmd.A` runs **silent/auto**, hiding the progress form and auto-closing on completion). Lets the user pick input/output drive+dir, then opens the `DRGService` form.
- **`DRGService.cs`** — the batch pipeline form. All work runs on a `BackgroundWorker` (`Worker_DoWork`) reporting progress through three UI steps (檔案讀取 → 編碼 → 結果產製).
- **`DRGCommLib.cs`** — VB6-style static helper module: byte-length string ops (BIG5/DBCS aware: `BLeft`/`BRight`/`BSubStr`), `CountTimeInterval` profiling stamps, path/dir helpers, version string (`retAppVersion`).

### Batch pipeline (`Worker_DoWork`)
1. Read input + output file paths from the **`DRG_TEMP_FILES`** table (`FILE_TYPE = 'INPUT' | 'OUTPUT'`); writes `STRTIME`/`ENDTIME` markers.
2. **`BatchIns_DRGTMP`** — parse the input CSV with `TextFieldParser` (comma-delimited, **fixed column positions 0–54**), normalize sex (`convertSex`, derived from PID 2nd char if blank), validate each row via **`PreCheck`** (a bitmask: missing/format-wrong 入院日/出生日/出院日, 主診斷碼 format, diagnosis/op code length > 7, and an out-date floor of `20260101`). Valid rows → `DRG_TEMP` + `DRG_TEMP_SORT`; invalid rows get a Chinese `ErrDesc` and are skipped from coding.
3. **`ProcICD10`** — for each sorted row, call the native grouping engine `rddi_lib.rddi0001`: `rddi1000_reload_db()` once, then `rddi1000_main(...)` per record to produce DRG_NO / CC_MARK / MDC_CODE / ERR_NO, written back to `DRG_TEMP`.
4. **`DRG_TMP_2_FILE`** — `SELECT` `DRG_TEMP` with Chinese column aliases, write a **BIG5-encoded** CSV (quoted values, `USER_GUID` column stripped from output), then delete this run's rows.

Rows are scoped per run by a `USER_GUID` (a `Guid` generated per process), so concurrent/residual data is isolated.

### The DRG engine lives in `rddt_lib.dll` (also .NET, decompiled in `_decompiled_rddt_lib\`)
`DRGService.ProcICD10` only orchestrates; the actual grouping is `rddi_lib.rddi0001` in **`rddt_lib.dll`** (a managed .NET assembly, not native). The C# in `_decompiled\` does I/O, validation, and orchestration; the rules live here.

- **`rddi0001.cs`** (~4,700 lines) — the Tw-DRG grouper, a line-by-line port of legacy COBOL (methods suffixed `_yyy`, COBOL-style `H_*` host variables). Two public entry points used by the app:
  - `rddi1000_reload_db()` — loads reference tables into in-memory `DataTable`s once: `RDDT_MDC_DRGWGT_V` (MDC/DRG weights), `RDDT_PDX_MDC_V` (principal-diagnosis → MDC), `RDDT_XICD_V` (cross/exclusion ICD rules). Plus `RDDT_ECC`, `RDDT_DRG_XICD` etc. via the typed `icd10DataSet` TableAdapters.
  - `rddi1000_main(...)` — groups one record. Flow: compute age/months/days (`part_code == "903"` newborns use `child_birthday`) → `icd10cm_chk_yyy` (validate ICD-10-CM) → `ecc_chk_yyy` (complication/comorbidity → `cc_mark`/`cc_code`) → `mdc_chk_yyy` (assign MDC) → OP-code partition into surgical vs medical (`v_depflag` P/M) via `combo_drg_yyy` → `tree_yyy` (DRG decision-tree + weight). Returns `0` ok, `-1` CM-check fail, `-2` no DRG. **Flow diagrams: [`docs/rddi1000_main_flow.md`](docs/rddi1000_main_flow.md) (overall), [`docs/ecc_chk_yyy_flow.md`](docs/ecc_chk_yyy_flow.md) (`ecc_chk_yyy` CC/MCC severity), [`docs/mdc_chk_yyy_flow.md`](docs/mdc_chk_yyy_flow.md) (`mdc_chk_yyy` MDC assignment), [`docs/combo_drg_yyy_flow.md`](docs/combo_drg_yyy_flow.md) (`combo_drg_yyy` candidate generation), [`docs/combo_xicd_chk_yyy_flow.md`](docs/combo_xicd_chk_yyy_flow.md) (`combo_xicd_chk_yyy` COMBO_NO rule dispatcher), [`docs/tree_yyy_flow.md`](docs/tree_yyy_flow.md) (`tree_yyy` final DRG selection).**
  - Sentinel DRG codes mean "ungroupable / special": `XXX` (MDC 19/20 mental), `YYY`/`ZZZ`/`WWW`/`GGG` (XICD rule hits), `HHH` (`ophhh_chk`). Hard-coded `SP_OP*_CODE` arrays are cardiac/ECMO procedure lists driving DRGs `11201`/`11601`/`11602`.
- **`icd10DataSet.cs`** + `*TableAdapters\` — strongly-typed `DataSet` over the `RDDT_*` reference tables; this is the schema of the DRG ruleset inside `icd10.sdf`.
- **`rddt0001.cs`** — a second, older variant of the grouper class (same shape, smaller). The app calls `rddi0001`, not this.

To change *grouping rules* you edit `rddi0001.cs` logic and/or the `RDDT_*` data in `icd10.sdf` — but remember `_decompiled_*` is not built by anything (see below).

### Data layer internals (`IISILib.dll`, decompiled in `_decompiled_IISILib\`)
`DBModels` (`IISILib.IISI`) wraps an `IDbConnection` via a `ConnectionSingleton`. `Init(DB00)` reads `appSettings["DB00"]` + `appSettings["dbtype"]` and picks the provider (`SqlCe`/`Oracle_DataAccess`/`SqlClient`/`OleDb`). Notable:
- **Nested-transaction counting**: `BeginTransaction`/`CommitTransaction` ref-count via `_MultiTransaction`; only the outermost actually commits, and any inner rollback sets a flag that forces the outer to roll back. The pipeline's many `BeginTransaction`/`CommitTransaction` pairs rely on this.
- `GetDataTable`/`ExecuteNonQuery`/`ExecuteScalar` support parameters, but the app **doesn't use them** — it passes raw concatenated SQL. ⚠️ The hardcoded `DBNameEnum.DB01`/`DB02` branches contain **embedded server IPs and credentials** (test/IDC databases); treat as sensitive and do not commit/publish.

## Data layer & DB switching

DB access goes through `DBModels` (in `IISILib.dll`, namespace `IISILib.IISI`) — `BeginTransaction` / `CommitTransaction` / `ExecuteNonQuery(ref sql)` / `GetDataTable(ref sql)`. **Queries are built by string concatenation** (no parameterization); preserve that style when editing, and be aware values are interpolated directly into SQL.

`DRGICD10.exe.config` selects the backend at runtime — this is the only thing configurable from the deployment folder:
- `appSettings/dbtype`: `sqlce` (default) | `oracle` | `sqlserver`
- `appSettings/DB00` + `connectionStrings/DB00`: the connection string for that provider
- Default is SQL CE against `icd10.sdf` (~196 MB, `System.Data.SqlServerCe` 4.0, resolved via `|DataDirectory|`). Oracle/SQL Server variants are present as commented blocks — uncomment the matching `dbtype` + `DB00` pair. Keep the two consistent.

## Running

```powershell
.\DRGICD10.exe
```
Requires .NET Framework 4.0 (Client Profile), 32-bit. Bundled native stacks are x86 (SQL CE `sqlce*40*.dll`; Oracle ODP.NET 11 `Oracle.DataAccess.dll` + `oci.dll`/`oraociei11.dll`/…), so the process must stay x86. **Do not delete** these DLLs or `IISILib.dll` / `rddt_lib.dll` — there is no source for the latter two here.

## Version / date constraints

Build is stamped **115/01/01** (ROC year 115 = 2026); `PreCheck` rejects out-dates earlier than `20260101`. When bumping the version, the human-readable string is in `DRGCommLib.retAppVersion`.

## Logging

`System.Diagnostics` is configured in `DRGICD10.exe.config` with a `FileLogTraceListener` (`My.Application.Log`) at `Information`. The pipeline also collects in-memory timing stamps (`DRGCommLib.CountTimeInterval`) and per-row error logs surfaced at the end of a run.
