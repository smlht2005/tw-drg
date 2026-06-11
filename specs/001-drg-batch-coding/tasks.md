---
description: "Task list for DRG 批次編碼 implementation"
---

# Tasks: DRG 批次編碼(Tw-DRG Batch Coding)

**Input**: Design documents from `specs/001-drg-batch-coding/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: **強制納入** — 憲章原則 IV(測試先行)要求 Red-Green-Refactor;各故事測試先寫且須先失敗。
**Organization**: 依使用者故事分組,可獨立實作與測試。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行(不同檔、無相依)
- **[Story]**: US1 / US2 / US3
- 路徑依 plan.md:`src/Drg.Core`、`src/Drg.Data`、`src/Drg.Api`、`src/Drg.Cli`;測試於 `tests/`
- 標記:`[x]` 完成 / `[~]` 部分完成 / `[ ]` 未開始

---

## 目前進度快照(2026-06-11)

- **Setup + Foundational**:完成(T001–T013、含 T012b 遷移批次 1)。
- **US1 引擎純邏輯**:`AgeCalculator`/`SexResolver`(T019/T019a)、`Icd10CmCheck`(T020)、`EccCheck`(T021)、`MdcCheck`(T022)、`TreeSelector`(T025)、`CandidateFilter`(T023 marks 段)— 全數完成並測試。
- **真實 parity**:`tools/LegacyOracle` 自真實 `rddi1000_main` 產生 25 案語料;`OracleParityTests` 驗證 `Icd10CmCheck→EccCheck→MdcCheck` 對 25 案 **MDC/CC 完全一致**。
- **測試總計**:73 passed / 1 skip(Core 67、Parity 3、Data 2、Integration 1)。
- **資料**:遷移批次 1+2 完成(icd10.sqlite 共 10 表、1.59M 列);combo 叢集資料前置就緒。
- **combo 查詢層(C1)**:`ComboMatchRule`(combo_AX/BX/CX 回傳決策規則,C1a)+ `CandidateRepository`(逐筆參數化 SQL 載入候選 join 列:主視圖∪NotIn∪UN、含 _00;對真實 icd10.sqlite 整合測試,C1b)皆完成。
- **待辦關鍵路徑**:combo_AX/BX/CX 計數(CandidateRepository + ComboMatchRule 串接)→ T024 `ComboXicd`(72-case 分派)→ combo_drg 串接 + 主編排 → T026 `DrgGrouper` → 擴充 oracle 比對完整 DRG。
- **未推前置**:無(已推至 `origin/001-drg-batch-coding`)。

---

## Phase 1: Setup(共用基礎)

- [x] T001 建立 .NET 8 solution 與專案:`src/Drg.Core`、`src/Drg.Data`、`src/Drg.Api`、`src/Drg.Cli`、`tests/Drg.Core.Tests`、`tests/Drg.Parity.Tests`、`tests/Drg.Integration.Tests`
- [x] T002 [P] 加入相依套件:Dapper、CsvHelper(`Drg.Core/Drg.Data`)、System.CommandLine(`Drg.Cli`)、xUnit + FluentAssertions(tests)
- [x] T003 [P] `.editorconfig` + 格式化/Lint 設定;CI build 工作流
- [x] T004 [P] 環境變數設定繫結(`DRG_DB_CONNECTION`、`DRG_BATCH_MAX`)於 `src/Drg.Core/Configuration/DrgOptions.cs`(機敏不內嵌,原則 III)

## Phase 2: Foundational(阻斷性前置 — 所有故事之前須完成)

**⚠️ CRITICAL**: 完成前任何使用者故事不得開工。

- [x] T005 [P] `ClaimEncounter` 模型於 `src/Drg.Core/Models/ClaimEncounter.cs`(data-model.md)
- [x] T006 [P] `CodingResult` 模型於 `src/Drg.Core/Models/CodingResult.cs`
- [x] T007 [P] `BatchJob` + `BatchStatus` enum 於 `src/Drg.Core/Models/BatchJob.cs`
- [x] T008 [P] 規則集參考模型(PdxMdc / MdcDrgWgt / Xicd / Ecc / EccGroup)於 `src/Drg.Core/Ruleset/`
- [x] T009 參數化、provider-neutral 連線工廠於 `src/Drg.Data/DbConnectionFactory.cs`(原則 III:全參數化、無內嵌機敏)
- [x] T010 規則集載入(`RDDT_*` → 記憶體、版次化)於 `src/Drg.Data/RulesetRepository.cs`
- [x] T011 固定欄位 CSV 讀取 → `ClaimEncounter` 於 `src/Drg.Core/Io/ClaimCsvReader.cs`(FR-014 輸入相容)
- [x] T012 Golden 回歸樣本測試骨架 + fixtures 於 `tests/Drg.Parity.Tests/GoldenCorpus/`(原則 II)
- [x] T012b 參考資料遷移 **批次 1**:icd10.sdf(SQL CE)→ icd10.sqlite 五張 RDDT 核心表(`scripts/export_sdf.ps1` + `tools/Drg.Migrate`;文件 `docs/data_migration.md`);整合測試 `tests/Drg.Data.Tests/RealRulesetIntegrationTests.cs` 對真實資料驗證 MdcCheck
- [x] T012c 參考資料遷移 **批次 2**:combo join 表(`RDDT_MDC_DRG_XICD_V` 1M 列 + `_00_V`/`_NOTIN_V`/`_UN_V` + `RDDT_DRG_MDC02_V`)→ icd10.sqlite(共 10 表、1.59M 列、89MB)— combo_drg/combo_xicd 資料前置就緒
- [~] T012a [H1] golden corpus 來源(SC-002 / 原則 I·II)
  - [x] **legacy-oracle 語料**:`tools/LegacyOracle`(netfx x86 harness 直呼 rddi1000_main)自真實引擎產生 25 案(每 MDC 一案)→ `GoldenCorpus/legacy_oracle.json`
  - [ ] 官方 Tw-DRG 115/01/01 測試案例集(取得後併入,作為獨立來源交叉驗證)
- [x] T013 批次協調骨架(`BatchId` 資料隔離、起訖時間)於 `src/Drg.Core/BatchCoder.cs`

**Checkpoint**: 基礎就緒 — 使用者故事可開工。

---

## Phase 3: User Story 1 - 將合法病歷檔批次編出 DRG 結果檔 (P1) 🎯 MVP

**Goal**: 對全合法輸入,逐筆產出與官方一致的 DRG/MDC/CC 結果檔。
**Independent Test**: 餵全合法範例檔 → 每筆有 DRG/MDC/CC,且對官方參考結果逐筆相符。

### Tests(先寫、先失敗 — 原則 IV)⚠️

- [~] T014 [P] [US1] Parity 測試:對 legacy-oracle 語料比對 DRG/MDC/CC
  - [x] `tests/Drg.Parity.Tests/OracleParityTests.cs`:`Icd10CmCheck→EccCheck→MdcCheck` 管線對 25 案比對 **MDC/CC**(全數一致)
  - [ ] 完整 **DRG** 比對:待 T024/T026 後啟用(並擴充含 OP 的外科案)
- [x] T015 [P] [US1] 單元:年齡計算(含月/日與量化怪癖)於 `tests/Drg.Core.Tests/AgeCalculatorTests.cs`;性別推導於 `tests/Drg.Core.Tests/SexResolverTests.cs`
- [x] T016 [P] [US1] 單元:CC/MCC 分級(MCC→T→CC 優先序、同群排除)於 `tests/Drg.Core.Tests/EccCheckTests.cs`
- [x] T017 [P] [US1] 單元:MDC 指派(24/25/1–23 優先序、性別分流 B/T)於 `tests/Drg.Core.Tests/MdcCheckTests.cs`
- [x] T018 [P] [US1] 單元:tree 權重決選(MDC15 順位、權重 tie-break、等價重映射)於 `tests/Drg.Core.Tests/TreeSelectorTests.cs`

### Implementation

- [x] T019 [US1] 年齡/月/日計算器於 `src/Drg.Core/Engine/AgeCalculator.cs`
- [x] T019a [US1] [M2] 性別推導(Sex 空白 → 由 Pid,legacy `convertSex`)於 `src/Drg.Core/Engine/SexResolver.cs`(FR-004 邊界;影響 MDC 11/12/13 分流)
- [x] T020 [US1] ICD-10-CM 驗證關卡於 `src/Drg.Core/Engine/Icd10CmCheck.cs`(含 `GroupingContext` 狀態物件;errcode_chk_yyy + icdop_chk_yyy + chk5 組合);修正 Foundational 的 `Xicd`/`RDDT_XICD_V` schema 對齊
- [x] T021 [US1] ECC(CC/MCC)分級於 `src/Drg.Core/Engine/EccCheck.cs`(藍圖 `docs/ecc_chk_yyy_flow.md`)
- [x] T022 [US1] MDC 指派於 `src/Drg.Core/Engine/MdcCheck.cs`(藍圖 `docs/mdc_chk_yyy_flow.md`)
- [~] T023 [US1] combo_drg 候選產生(藍圖 `docs/combo_drg_yyy_flow.md`)
  - [x] marks 篩選(CC/AGE/LIVE/DEP)純函式於 `src/Drg.Core/Engine/CandidateFilter.cs` + 合成測試
  - [ ] 查詢層:逐筆參數化 SQL 取候選列(`RDDT_MDC_DRG_XICD_*_V`)— **阻塞**:icd10.sdf(SQL CE)需先遷移 SQLite
  - [ ] opflag 之 ICD/ITEM_TYPE 比對 + per-MDC 視圖選擇(main 740–880)+ combo_xicd 串接
- [ ] T024 [US1] combo_xicd(COMBO_NO 配方)於 `src/Drg.Core/Engine/ComboXicd.cs`(藍圖 `docs/combo_xicd_chk_yyy_flow.md`)
- [x] T025 [US1] tree 權重決選於 `src/Drg.Core/Engine/TreeSelector.cs`(藍圖 `docs/tree_yyy_flow.md`)
- [ ] T026 [US1] 分組主協調(rddi1000_main 等價)於 `src/Drg.Core/Engine/DrgGrouper.cs`(藍圖 `docs/rddi1000_main_flow.md`)
- [ ] T027 [US1] UTF-8 結果輸出於 `src/Drg.Core/Io/ResultWriter.cs`(FR-014)
- [ ] T028 [US1] 串接 `BatchCoder` happy path:讀 → 分組 → 寫 於 `src/Drg.Core/BatchCoder.cs`
- [ ] T028a [US1] [M1] 將 `rulesetVersion`(115/01/01)標註於 `BatchJob` 與結果輸出(FR-013 可追溯)於 `src/Drg.Core/BatchCoder.cs`、`src/Drg.Core/Io/ResultWriter.cs`

**Checkpoint**: US1 可獨立運行——全合法檔產出正確 DRG 結果。

---

## Phase 4: User Story 2 - 輸入驗證與錯誤標註(不中斷整批) (P2)

**Goal**: 髒資料逐筆標註中文錯誤原因,整批續跑、零丟棄。
**Independent Test**: 餵混合髒資料 → 合法照編、錯誤帶原因、整批跑完且輸出筆數=輸入筆數。

### Tests ⚠️

- [ ] T029 [P] [US2] 單元:PreCheck 驗證 bitmask 全規則於 `tests/Drg.Core.Tests/ClaimValidatorTests.cs`
- [ ] T030 [P] [US2] 整合:混合檔 → 全筆出現、錯誤標註於 `tests/Drg.Integration.Tests/MixedBatchTests.cs`

### Implementation

- [ ] T031 [US2] 驗證規則(日期、碼長度、主診斷、版本生效日)於 `src/Drg.Core/Validation/ClaimValidator.cs`(data-model.md 對照表)
- [ ] T032 [US2] 中文錯誤原因對應於 `src/Drg.Core/Validation/ErrorDescriptions.cs`
- [ ] T033 [US2] 毀損 CSV 列容錯(略過+記錄)於 `src/Drg.Core/Io/ClaimCsvReader.cs`
- [ ] T034 [US2] 將驗證併入 `BatchCoder`:逐筆獨立、零丟棄(FR-006/007/016)於 `src/Drg.Core/BatchCoder.cs`

**Checkpoint**: US1 + US2 皆可獨立運行。

---

## Phase 5: User Story 3 - 無人值守 / 自動化執行 (P3)

**Goal**: 經 API / CLI 非互動啟動,完成後自結束;結果與核心一致。
**Independent Test**: `--silent` 跑完自結束、exit code 正確;API 提交→查詢→取結果。

### Tests ⚠️

- [ ] T035 [P] [US3] 合約:Web API 端點與不變式於 `tests/Drg.Integration.Tests/ApiContractTests.cs`(contracts/batch-coding-api.md)
- [ ] T036 [P] [US3] 合約:CLI exit codes + silent 於 `tests/Drg.Integration.Tests/CliContractTests.cs`(contracts/cli-contract.md)

### Implementation

- [ ] T037 [US3] Web API 端點 POST/GET batches 於 `src/Drg.Api/Controllers/BatchesController.cs`
- [ ] T038 [US3] CLI `drg code` 指令於 `src/Drg.Cli/Commands/CodeCommand.cs`
- [ ] T039 [US3] `DRG_BATCH_MAX` 上限檢查 + exit codes(2/3/4)於 `src/Drg.Cli` 與 `src/Drg.Api`
- [ ] T040 [US3] 進度/狀態回報(API status、CLI stdout 三階段)— FR-010

**Checkpoint**: 三個故事皆可獨立運行。

---

## Phase 6: Polish & Cross-Cutting

- [ ] T041 [P] 無 PHI 日誌稽核(原則 III)於 `src/Drg.Core/Logging/`
- [ ] T042 [P] DB provider 切換整合測試(SQL Server / SQLite / Oracle)於 `tests/Drg.Integration.Tests/ProviderSwitchTests.cs`
- [ ] T043 [P] 依 `quickstart.md` 跑驗收驗證
- [ ] T044 [P] 文件更新(README、CLAUDE.md 同步實際結構)

---

## Dependencies & Execution Order

- **Setup(P1)**:無相依,先行。
- **Foundational(P2)**:依 Setup;**阻斷所有故事**。其中 T009/T010(資料層)→ T011(讀檔)→ T013(協調)為鏈式;T005–T008 模型可平行。
- **US1(P3 階段)**:依 Foundational;為 MVP,優先完成。引擎內部相依:T019/T020 → T021/T022 → T023 → T024 → T025 → T026 → T027/T028。
- **US2**:依 Foundational;可與 US1 平行開工(不同檔),但 T034 串接須在 US1 的 T028 之後。
- **US3**:依 US1 核心(T026/T028)可用;API/CLI 共用 `Drg.Core`。
- **Polish**:依所需故事完成。

### Parallel Opportunities

- T002/T003/T004 平行;T005–T008 模型平行。
- 各故事 `[P]` 測試可同時撰寫。
- Foundational 完成後,US1 與 US2 可由不同人平行推進。

## Implementation Strategy

**MVP First**:Setup → Foundational → **US1** → 以 golden corpus 驗證 parity(原則 II)→ 即可展示。
**Incremental**:US1(可編碼)→ US2(可容錯)→ US3(可自動化),每階段獨立可測、不破壞前階段。
**TDD(原則 IV)**:每項實作前先寫對應失敗測試;分組邏輯以 golden corpus 釘住 legacy 等價。

## Notes

- 引擎六模組(T021–T026)逐一對應先前 `docs/*_flow.md` 流程圖,移植時先寫測試、對齊 legacy 輸出。
- BIG5→UTF-8 為已記錄偏移:parity 測試比對**數值**(DRG/MDC/CC),非位元組編碼。
- 所有資料存取走 Dapper 參數化(原則 III);連線字串經環境變數。
