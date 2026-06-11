# Implementation Plan: DRG 批次編碼(Tw-DRG Batch Coding)

**Branch**: `001-drg-batch-coding` | **Date**: 2026-06-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-drg-batch-coding/spec.md`

## Summary

建置新世代 Tw-DRG 批次編碼服務:接收住院病歷申報檔(沿用 legacy 固定欄位輸入),逐筆驗證後依 **Tw-DRG 115/01/01 (v3.4.20)** 規則集指派 DRG/MDC/CC,輸出 **UTF-8** 結果檔。交付為 headless 的**核心函式庫 + Web API + CLI**;以 golden 回歸樣本確保與 legacy 數值一致(原則 II),全程參數化 SQL、無內嵌機敏(原則 III),測試先行(原則 IV)。

## Technical Context

**Language/Version**: C# / **.NET 8 (LTS)**
**Primary Dependencies**: ASP.NET Core(Web API)、System.CommandLine(CLI)、Dapper(參數化資料存取)、CsvHelper(固定欄位輸入解析)、xUnit + FluentAssertions(測試)
**Storage**: 關聯式資料庫存放 Tw-DRG 參考規則集(`RDDT_*`)與批次工作資料;**provider-neutral**(SQL Server / SQLite / Oracle 可替換)。參考資料由 legacy `icd10.sdf` 遷移而來,具版次。
**Testing**: xUnit 單元/合約測試 + **parity 回歸樣本庫(golden corpus)** 對照 legacy 輸出
**Target Platform**: 跨平台(Linux/Windows 容器或服務)
**Project Type**: 共用核心 lib + Web API + CLI(單一 repo,多專案)
**Performance Goals**: 現階段單批上限預設 10 筆(環境變數可調),非吞吐關鍵;設計以正確性與可稽核性優先
**Constraints**: 僅參數化 SQL、無內嵌機敏、輸出 UTF-8、DRG/MDC/CC 與 legacy 逐筆一致
**Scale/Scope**: 單檔輸入、每筆約 20 診斷 + 20 手術碼;預設 10 筆/批(`DRG_BATCH_MAX` 可調)

## Constitution Check

*GATE: 進入 Phase 0 前須通過;Phase 1 設計後再檢核一次。*

| 原則 | 符合方式 | 結果 |
|------|----------|------|
| **I. 標準合規** | 目標版次明確(Tw-DRG 115/01/01, v3.4.20);規則集版次化並標註於結果(FR-013);以官方測試案例驗收(SC-002) | ✅ PASS |
| **II. Legacy 行為一致性** | 建 golden 回歸樣本庫,比對 `DRG_NO/MDC_CODE/CC_MARK/ERR_NO`;**BIG5→UTF-8 僅編碼差異、數值不變**,列為 documented divergence | ✅ PASS(已記錄偏移) |
| **III. 資料隱私與安全** | Dapper 全參數化(取代 legacy 字串拼接);連線字串/機敏經環境變數或 secret store(取代 legacy `DBModels.DB01/DB02` 明文);日誌不含 PHI | ✅ PASS(主動矯正 legacy 缺失) |
| **IV. 測試先行(TDD)** | 規則邏輯先寫失敗測試;回歸樣本即可執行規格;Red-Green-Refactor | ✅ PASS |

**結論**:無違反,Complexity Tracking 留空。唯一偏移(輸出編碼 BIG5→UTF-8)已於 spec FR-014 與本表記錄,符合原則 II 治理。

## Project Structure

### Documentation (this feature)

```text
specs/001-drg-batch-coding/
├── plan.md              # 本檔
├── research.md          # Phase 0:技術決策
├── data-model.md        # Phase 1:實體與驗證
├── quickstart.md        # Phase 1:建置/執行/驗證
├── contracts/           # Phase 1:介面合約
│   ├── batch-coding-api.md
│   └── cli-contract.md
└── tasks.md             # Phase 2(由 /sdd tasks 產生,非本階段)
```

### Source Code (repository root)

```text
src/
├── Drg.Core/        # 領域:驗證、年齡/CC 計算、MDC 指派、combo/tree 分組、模型
├── Drg.Data/        # 參數化、provider-neutral 資料存取;RDDT_* 規則集載入
├── Drg.Api/         # ASP.NET Core Web API(提交批次、查詢狀態/結果)
└── Drg.Cli/         # System.CommandLine CLI(無人值守批次)

tests/
├── Drg.Core.Tests/        # 單元:規則、驗證、邊界
├── Drg.Parity.Tests/      # golden 回歸樣本對照 legacy(原則 II)
└── Drg.Integration.Tests/ # API/CLI 端到端、DB provider 切換
```

**Structure Decision**: 採「共用 `Drg.Core` + `Drg.Data`,由 `Drg.Api` 與 `Drg.Cli` 兩個前端共用」之結構,直接對應 FR-017(服務/API/CLI 共享同一編碼核心),避免邏輯重複,利於 parity 測試集中於 `Drg.Core`。

## Phase 0: Research → `research.md`

解析所有技術未知(平台、資料存取、規則集來源、輸入解析、批次規模、編碼引擎移植策略),逐項 Decision / Rationale / Alternatives。

## Phase 1: Design → `data-model.md`, `contracts/`, `quickstart.md`

- **data-model.md**:ClaimEncounter / CodingResult / GroupingRuleset / BatchJob 之欄位、驗證規則(PreCheck bitmask 對應)、案件狀態轉移。
- **contracts/**:Web API(提交/查詢)與 CLI 指令合約,含請求/回應 schema 與錯誤情形。
- **quickstart.md**:建置、跑一次 CLI 批次、呼叫 API、執行 parity 測試。

## Complexity Tracking

> 無違反憲章,無需填寫。

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| —         | —          | —                                    |
