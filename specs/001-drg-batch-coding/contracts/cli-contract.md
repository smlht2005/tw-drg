# Contract: Command-Line Interface (CLI)

**Date**: 2026-06-11 | 對應 FR-009 無人值守、US3、FR-017。

## 指令

```
drg code --input <path> [--output <path>] [--silent] [--ruleset <version>]
```

| 選項 | 必填 | 說明 |
|------|------|------|
| `--input` | 是 | 病歷申報輸入檔(沿用 legacy 固定欄位格式) |
| `--output` | 否 | 結果輸出路徑;預設依輸入檔名衍生 |
| `--silent` | 否 | 無互動;僅以 exit code + 日誌回報(對應 legacy 自動模式) |
| `--ruleset` | 否 | 規則集版次;預設環境變數 / 115-01-01 |

## 環境變數

| 變數 | 說明 |
|------|------|
| `DRG_BATCH_MAX` | 單批最大筆數(預設 10,SC-004) |
| `DRG_DB_CONNECTION` | 資料庫連線字串(機敏,**不得內嵌**,原則 III) |

## Exit codes

| code | 意義 |
|------|------|
| 0 | 全批完成(含逐筆錯誤標註仍算完成) |
| 2 | 輸入無法讀取 / 不存在 |
| 3 | 超過 `DRG_BATCH_MAX` |
| 4 | 規則集 / 資料庫無法載入 |

## 行為不變式

- stdout 輸出進度(讀檔 → 編碼 → 產製),stderr 輸出錯誤(FR-010);`--silent` 時僅 stderr + exit code。
- 結果與 Web API 相同核心(`Drg.Core`)→ 同輸入必得同 DRG(FR-017 一致性)。
- 輸出 UTF-8(FR-014)。
- 不在 stdout/日誌輸出 PHI(原則 III)。
