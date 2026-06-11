# Contract: Batch Coding Web API

**Date**: 2026-06-11 | base path: `/api/v1`

對應 FR-009(無人值守)、FR-010(進度/狀態)、US3。所有時間為 ISO-8601;回應 UTF-8 JSON。

## POST /api/v1/batches — 提交批次

提交一份病歷申報檔進行編碼。

**Request**(multipart 或 JSON 指定來源):
```json
{ "inputRef": "path-or-upload-id", "outputRef": "path (optional)" }
```

**Responses**:
- `202 Accepted` → `{ "batchId": "<guid>", "status": "Received", "totalCount": 8 }`
- `400 Bad Request` → 輸入無法讀取 / 超過 `DRG_BATCH_MAX`:`{ "error": "BATCH_TOO_LARGE", "max": 10 }`(SC-004)
- `422 Unprocessable` → 檔案結構整體無效

## GET /api/v1/batches/{batchId} — 查詢狀態

**Responses**:
- `200 OK`:
```json
{
  "batchId": "<guid>",
  "status": "Completed",
  "rulesetVersion": "Tw-DRG 115/01/01 (v3.4.20)",
  "startedAt": "2026-06-11T08:00:00Z",
  "endedAt": "2026-06-11T08:00:01Z",
  "totalCount": 8, "codedCount": 6, "errorCount": 2
}
```
- `404 Not Found` → 未知 batchId

## GET /api/v1/batches/{batchId}/result — 取得結果

**Responses**:
- `200 OK`:每筆案件的編碼結果(CodingResult),UTF-8。
```json
{
  "batchId": "<guid>",
  "rulesetVersion": "Tw-DRG 115/01/01 (v3.4.20)",
  "results": [
    { "rowNum": 1, "drg": "07201", "mdc": "07", "ccMark": "Y", "ccCode": "01000000000000000000", "errNo": "", "errDesc": "" },
    { "rowNum": 2, "drg": "", "mdc": "", "ccMark": "", "ccCode": "", "errNo": "", "errDesc": "出生日格式不符 診斷碼長度錯誤" }
  ]
}
```
- `409 Conflict` → 批次尚未完成
- `404 Not Found`

## 不變式(契約測試對象)

- `results.length == totalCount`(FR-007 零丟棄;SC-001)。
- 每筆要嘛 `drg` 有值,要嘛 `errDesc` 有值(FR-006;SC-003)。
- 回應一律帶 `rulesetVersion`(FR-013)。
- 任一筆錯誤不影響其他筆之 `drg`(FR-016 single)。
