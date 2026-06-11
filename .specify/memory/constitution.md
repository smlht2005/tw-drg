# Tw-DRG 編碼服務 Constitution

<!-- 本憲章為「全新開發」的新世代 Tw-DRG 編碼服務所立,但須對齊既有 legacy 引擎(反編譯之
     rddi_lib / DRGICD10)的分組輸出。以下三條為不可妥協的核心條款,日後每份 plan 都會在
     Constitution Check 閘門逐條檢核;違反須在 plan 的 Complexity Tracking 表中提出正當理由,
     否則應簡化設計以符合原則。 -->

## Core Principles

### I. 標準合規(Standards Compliance) — NON-NEGOTIABLE

對官方規格的正確性是強制要求,不可用「看起來對」替代驗證。

- 所有 DRG/MDC 指派、ICD-10-CM/PCS 碼處理、健保署(NHI/中央健康保險署)申報檔格式,必須符合**官方公告版本**。
- 採用的標準版次必須明確標註並可追溯:Tw-DRG 版次、ICD-10 年度、健保署檔案格式版本;適用時遵循 HL7 FHIR、LOINC。
- 任何分組規則的實作都須能對照**官方測試案例 / 公告範例**驗證;無對照來源者不得宣稱合規。
- 標準改版視為受控變更:須記錄來源文件、生效日,並重跑合規驗證。

### II. Legacy 行為一致性(Legacy Behavior Parity)

新系統在改變任何分組行為之前,輸出必須與既有 legacy 引擎逐筆一致。

- 必須建立並維護**回歸樣本庫(golden / regression corpus)**:涵蓋各 MDC、哨兵碼(XXX/YYY/ZZZ/WWW/GGG/HHH)、CC/MCC 分級、特例(新生兒 903、心臟 ECMO)等代表性案件。
- 任何重構或現代化,在合併前須對回歸樣本比對 `DRG_NO / MDC_CODE / CC_MARK / ERR_NO` **逐筆相符**。
- 凡與 legacy 輸出有差異,須明確記錄、**分類為「修正既有缺陷」或「規格變更」**,並取得簽核;未簽核的差異視為回歸失敗。
- 當「標準合規(原則 I)」與「Legacy 一致性」衝突時,以**標準為準**,並將該差異列為已知偏移(documented divergence)留存。

### III. 資料隱私與安全(Data Privacy & Security) — NON-NEGOTIABLE

病患資料(PHI)之保護優先於一切便利性。對應 legacy `DBModels` 已發現的缺失,明文禁止重蹈。

- **禁止內嵌任何連線帳密 / 伺服器位址**於程式碼、設定或版控(legacy `DBModels.DB01/DB02` 之明文帳密為反面教材);機敏設定一律外部化、加密保管。
- **日誌、錯誤訊息、commit 不得含 PHI**(身分證號、病歷號、可識別個資);除錯/稽核改以去識別化或代碼處理。
- **申報輸出檔屬「受控 PHI 產物」**:得含申報必要之識別碼,但須加密儲存與傳輸、存取控管、留存可稽核軌跡,並遵循最小揭露(僅輸出申報所需欄位);非申報用途之匯出一律去識別化。
- **所有 SQL 一律參數化**,禁止字串拼接組查詢(對應 legacy 全面字串拼接之 injection 風險面)。
- PHI 遵循最小揭露;傳輸與靜態儲存須加密;存取須留可稽核軌跡。

### IV. 測試先行(Test-First / TDD) — NON-NEGOTIABLE

實作前必須先有會失敗的測試,嚴格遵循 Red-Green-Refactor。

- 流程強制:**撰寫測試 → 經核可 → 確認失敗(Red)→ 才實作至通過(Green)→ 重構(Refactor)**。
- 規則繁重的分組邏輯(MDC 指派、CC/MCC 分級、combo/tree 決選)須以測試案例釘住預期結果,測試即規格的可執行形式。
- 與原則 II 互補:回歸樣本庫即為 Legacy 一致性的測試集;新行為另立單元/合約測試。
- 未附對應測試的分組邏輯變更,視為未完成,不得合併。

## Additional Constraints(附加限制)

- **單一可信資料來源**:DRG/ICD/CC 等參考資料(對應 legacy `RDDT_*` 表)須有受控的版本與更新流程,不得散落硬編碼。
- **可稽核性**:每筆編碼結果須可回溯其輸入、採用的標準版次與規則路徑,以供健保申報核對與爭議釐清。
- **資料庫中立**:資料存取層不得綁死單一資料庫;切換後端不應影響分組結果。

## Development Workflow & Quality Gates(開發流程與品質閘門)

- 採 **Spec-Driven Development(SDD)** 流程:constitution → specify → clarify → plan → analyze → tasks → implement。
- **Constitution Check** 為 plan 階段強制閘門:逐條檢核上述三原則,未過不得進入實作。
- **回歸閘門**:涉及分組邏輯的變更,須通過回歸樣本庫逐筆比對(原則 II)方可合併。
- **測試先行為強制**(原則 IV):分組邏輯變更須先有失敗測試;Red-Green-Refactor 為實作的標準節奏。
- Code review 須確認無內嵌機敏、無 PHI 外洩、SQL 參數化(原則 III)。

## Governance

- 本憲章之效力高於其他慣例;與之衝突的做法須修正或在 plan 中提出受控豁免並記錄理由。
- **修訂程序**:任何條款變更須提出理由、影響範圍與對既有 feature 的回溯處理,經專案負責人核可後更新版本與日期。
- **版號規則(semver)**:MAJOR=移除/重定義原則或不相容治理變更;MINOR=新增原則或實質擴充;PATCH=措辭澄清、不影響語意。
- **合規驗證**:每次 plan/analyze 階段對照本憲章;發行前確認三大核心原則之閘門均通過。

**Version**: 1.2.0 | **Ratified**: 2026-06-11 | **Last Amended**: 2026-06-11
