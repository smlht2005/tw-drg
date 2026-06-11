# Phase 0 Research: DRG 批次編碼

**Date**: 2026-06-11 | **Plan**: [plan.md](./plan.md)

逐項解析技術未知。每項格式:Decision / Rationale / Alternatives considered。

## R1. 執行平台與語言

- **Decision**: C# / .NET 8 (LTS)。
- **Rationale**: legacy 引擎本即 .NET;留在同平台可逐方法移植 `rddi0001` 分組邏輯,**大幅降低 DRG 數值偏移風險(原則 II)**;團隊以 C# 為主力,維護成本低;.NET 8 跨平台、可容器化,滿足 headless 服務(FR-017)。
- **Alternatives**: Python(生態佳但需重寫全部規則,parity 風險高、團隊非主力)、Java(同 parity 風險)。皆否決。

## R2. 資料存取方式

- **Decision**: Dapper + ADO.NET,**全程參數化查詢**;以 `IDbConnection` 抽象支援多 provider。
- **Rationale**: 直接落實原則 III(取代 legacy `DBModels` 全面字串拼接的 injection 風險面);Dapper 輕量、效能近原生、易對應現有 SQL;`IDbConnection` 抽象維持 DB 中立(附加限制)。
- **Alternatives**: EF Core(對既有 `RDDT_*` 表與大量 RowFilter 式查詢偏重、ORM 阻抗)、沿用 legacy 字串拼接(**違反原則 III,直接否決**)。

## R3. Tw-DRG 參考規則集來源

- **Decision**: 將 legacy `icd10.sdf` 內 `RDDT_MDC_DRGWGT_V / RDDT_PDX_MDC_V / RDDT_XICD_V / RDDT_ECC / RDDT_ECC_GROUP / RDDT_DRG_XICD` 等遷移為受控、版次化的參考資料,啟動時載入記憶體;標註版次 115/01/01。
- **Rationale**: 規則資料即標準的可執行形式(原則 I);版次化使結果可追溯(FR-013);記憶體載入對應 legacy `rddi1000_reload_db` 的做法,效能可接受。
- **Alternatives**: 硬編碼規則於程式(違反單一可信來源、難維護)、每筆即時查 DB(I/O 過重)。否決。

## R4. 輸入解析與容錯

- **Decision**: CsvHelper 以固定欄位對應解析(沿用 legacy 欄位位置,FR-014);遇結構毀損列(欄位數不符)略過並記錄,不中止整批(FR-006、FR-016 single)。
- **Rationale**: 維持上游相容;容錯逐筆獨立符合 FR-016「single & both」。
- **Alternatives**: 嚴格解析整檔失敗即中止(違反 FR-006/FR-007)。否決。

## R5. 輸出編碼

- **Decision**: 輸出 **UTF-8**(取代 legacy BIG5),欄位 schema 初期沿用 legacy 中文欄位、後續可調(FR-014)。
- **Rationale**: 使用者指定;UTF-8 為現代互通標準。**屬對 legacy 的 documented divergence**——僅編碼層,DRG/MDC/CC 數值不變,符合原則 II 治理。
- **Alternatives**: 維持 BIG5(使用者已否決)。

## R6. 批次規模與設定

- **Decision**: 單批最大筆數預設 **10**,由環境變數 `DRG_BATCH_MAX` 覆寫(SC-004)。其他可調項(DB 連線、規則集版次路徑)亦走環境變數 / secret store。
- **Rationale**: 符合使用者決議;機敏外部化落實原則 III。
- **Alternatives**: 寫死上限(不具彈性)、無上限(資源風險)。否決。
- **Note**: 預設 10 偏小,疑為測試預設;正式環境經 `DRG_BATCH_MAX` 調高即可,無需改碼。

## R7. 交付形態(服務/API/CLI)

- **Decision**: `Drg.Core`/`Drg.Data` 為共用核心;`Drg.Api`(ASP.NET Core)與 `Drg.Cli`(System.CommandLine)兩前端共用之。
- **Rationale**: 對應 FR-017;單一編碼核心避免邏輯分歧,parity 測試集中(原則 II/IV)。
- **Alternatives**: API 與 CLI 各自實作(邏輯重複、易偏移)。否決。

## R8. 編碼引擎移植策略

- **Decision**: 將 legacy `rddi0001` 之決策流程(已產出六張流程圖:rddi1000_main / ecc / mdc / combo_drg / combo_xicd / tree)逐段移植進 `Drg.Core`,**每段先以 legacy 輸出為預期值寫失敗測試**再實作(原則 IV),並納入 golden 回歸樣本(原則 II)。
- **Rationale**: 流程圖已文件化(`docs/*_flow.md`),可作為移植藍圖;測試先行確保等價。
- **Alternatives**: 從官方規格書重寫(風險高、無法保證與 legacy 既有申報一致);直接呼叫 legacy DLL(無法跨平台、延續舊技術債)。否決。

## 未解決 / 待後續

- 官方 Tw-DRG 115/01/01 測試案例集之取得來源(健保署)— 影響 SC-002 驗收,需於實作前備妥。
- legacy 輸出樣本是否可大量取得作為 golden corpus 基準(若不足,改以官方測試集為準,原則 I 優先)。
