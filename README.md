# tw-drg

Tw-DRG 批次編碼服務的**現代化工作區**:以 Spec-Driven Development(SDD)規劃新世代系統,並保留對既有 legacy 編碼引擎的逆向分析文件作為移植藍圖。

## 內容結構

| 路徑 | 說明 |
|------|------|
| [`.specify/memory/constitution.md`](.specify/memory/constitution.md) | 專案憲章——四大不可妥協原則(標準合規、Legacy 行為一致性、資料隱私與安全、測試先行) |
| [`specs/001-drg-batch-coding/`](specs/001-drg-batch-coding/) | 第一個功能的完整 SDD 產物:spec / plan / research / data-model / quickstart / contracts |
| [`docs/`](docs/) | legacy DRG 分組引擎(`rddi0001`)的決策流程圖,共六張 Mermaid |
| `CLAUDE.md` | 給 AI 編碼代理的專案說明 |

## SDD 流程

本專案以 spec-kit 式流程推進,**規格即可執行的需求**:

```
constitution → specify → clarify → plan → analyze → tasks → implement
```

`specs/001-drg-batch-coding/` 已完成至 **plan** 階段:

- **spec.md** — 3 個優先序使用者故事(批次編碼 / 驗證與錯誤標註 / 無人值守)、17 條功能需求、可量測成功標準
- **plan.md** — .NET 8 技術選型、Constitution Check 閘門(四原則全通過)、`Drg.Core`+`Drg.Data`+`Drg.Api`+`Drg.Cli` 結構
- **research.md** — R1–R8 技術決策(Decision / Rationale / Alternatives)
- **data-model.md** — 4 個實體 + 驗證規則 + 案件狀態轉移
- **contracts/** — Web API 與 CLI 介面合約

## DRG 引擎流程圖(`docs/`)

由 legacy 引擎逆向重建,作為新系統移植與測試對照的藍圖:

| 文件 | 內容 |
|------|------|
| [`rddi1000_main_flow.md`](docs/rddi1000_main_flow.md) | 單筆編碼主流程(總覽) |
| [`ecc_chk_yyy_flow.md`](docs/ecc_chk_yyy_flow.md) | 併發症/合併症(CC/MCC)分級 |
| [`mdc_chk_yyy_flow.md`](docs/mdc_chk_yyy_flow.md) | MDC(主要診斷類別)指派 |
| [`combo_drg_yyy_flow.md`](docs/combo_drg_yyy_flow.md) | DRG 候選產生(多層篩選) |
| [`combo_xicd_chk_yyy_flow.md`](docs/combo_xicd_chk_yyy_flow.md) | COMBO_NO 組合規則複核 |
| [`tree_yyy_flow.md`](docs/tree_yyy_flow.md) | 決策樹權重決選 |

> 流程圖以 Mermaid 撰寫,於 GitHub、VS Code、Obsidian 可直接渲染。

## 狀態

- ✅ 憲章 v1.1.0
- ✅ `001-drg-batch-coding` 規格 → 計畫完成
- ⬜ 下一步:`tasks`(任務分解)→ `implement`

## 授權

本儲存庫之 SDD 文件與分析內容著作權歸作者所有;新世代實作之授權待定。
