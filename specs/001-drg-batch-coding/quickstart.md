# Quickstart: DRG 批次編碼

**Date**: 2026-06-11 | **Plan**: [plan.md](./plan.md)

新世代服務尚未實作(本文為 Phase 1 設計產物,描述實作後的驗證方式)。

## 先決條件

- .NET 8 SDK
- 可存取的資料庫(SQL Server / SQLite / Oracle),已載入 Tw-DRG 115/01/01 參考規則集
- 環境變數:
  ```bash
  export DRG_DB_CONNECTION="<參數化連線字串,勿內嵌於碼>"
  export DRG_BATCH_MAX=10
  ```

## 建置與測試

```bash
dotnet build
dotnet test                              # 全部測試
dotnet test tests/Drg.Parity.Tests       # 僅 legacy 一致性回歸(原則 II)
```

## 跑一次批次(CLI)

```bash
dotnet run --project src/Drg.Cli -- code --input ./samples/claims.csv --output ./out/result.csv
# 無人值守
dotnet run --project src/Drg.Cli -- code --input ./samples/claims.csv --silent ; echo "exit=$?"
```

## 跑一次批次(API)

```bash
dotnet run --project src/Drg.Api &
curl -X POST localhost:5000/api/v1/batches -H 'Content-Type: application/json' \
     -d '{"inputRef":"./samples/claims.csv"}'        # → {batchId,...}
curl localhost:5000/api/v1/batches/<batchId>           # 狀態
curl localhost:5000/api/v1/batches/<batchId>/result    # 結果(UTF-8)
```

## 驗收對應(spec)

| 驗證 | 對應 |
|------|------|
| 結果筆數 = 輸入筆數 | SC-001、FR-007 |
| 對官方測試集逐筆相符 | SC-002、原則 I/II |
| 錯誤筆帶中文原因 | SC-003、FR-006、US2 |
| `--silent` 跑完自結束 | US3、FR-009 |
| 輸出為 UTF-8 | FR-014 |
| 結果含 rulesetVersion | FR-013 |
