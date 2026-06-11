# Phase 1 Data Model: DRG 批次編碼

**Date**: 2026-06-11 | **Plan**: [plan.md](./plan.md)

## 實體

### ClaimEncounter(病歷申報案件)— 輸入

| 欄位 | 型別 | 說明 |
|------|------|------|
| RowNum | int | 來源列號(用於錯誤回報、結果排序) |
| HospId | string | 醫事機構代號 |
| FeeYm | string(6) | 費用年月 YYYYMM |
| Pid | string | 身分識別(PHI) |
| SeqNo | string | 流水號 |
| Sex | string(1) | 性別 F/M/X;空白時由 Pid 推導 |
| InDate | date | 入院日 YYYYMMDD |
| Birthday | date | 出生日 YYYYMMDD |
| OutDate | date | 出院日 YYYYMMDD |
| ChildBirthday | date? | 子女出生日(新生兒就附母親案件用) |
| PartCode | string | 案件類別(如 903 新生兒) |
| TranCode | string | 轉歸代碼(4=死亡 等) |
| MedAmt | long | 醫療費用 |
| CmCodes | string[≤20] | 診斷碼(index 0 = 主診斷) |
| OpCodes | string[≤20] | 手術碼(index 0 = 主手術) |

### CodingResult(編碼結果)— 輸出

| 欄位 | 型別 | 說明 |
|------|------|------|
| RowNum | int | 對應來源案件 |
| Drg | string | DRG 碼;含哨兵碼 XXX/YYY/ZZZ/WWW/GGG/HHH |
| Mdc | string | MDC(主要診斷類別) |
| CcMark | string(1) | CC 分級:M / T / Y / N |
| CcCode | string(20) | 位元圖:貢獻併發症之診斷位置標 1 |
| ErrNo | string | 分組錯誤碼 |
| ErrDesc | string | 中文錯誤原因(驗證未過時) |

### GroupingRuleset(分組規則集)— 參考資料(版次化)

抽象表述官方 Tw-DRG 標準,對應 legacy `RDDT_*`:

- **PdxMdc**:主診斷 → MDC 對照(`RDDT_PDX_MDC`)
- **MdcDrgWgt**:DRG 權重/決策樹/順位(`RDDT_MDC_DRGWGT`)
- **Xicd**:交叉/排除 ICD 規則、ITEM_TYPE、COMBO_NO(`RDDT_XICD` / `RDDT_DRG_XICD`)
- **Ecc / EccGroup**:併發症規則與分組歸屬(`RDDT_ECC` / `RDDT_ECC_GROUP`)
- **Version**:版次(115/01/01),供 FR-013 標註

### BatchJob(批次作業)

| 欄位 | 型別 | 說明 |
|------|------|------|
| BatchId | guid | 執行識別(資料隔離,對應 legacy USER_GUID) |
| InputRef | string | 輸入位置 |
| OutputRef | string | 輸出位置 |
| RulesetVersion | string | 採用規則集版次 |
| StartedAt / EndedAt | datetime | 起訖時間(FR-012) |
| Status | enum | Received → Running → Completed / Failed |
| TotalCount / CodedCount / ErrorCount | int | 統計 |

## 驗證規則(對應 legacy PreCheck bitmask → FR-002)

| 規則 | 錯誤原因(中文) |
|------|----------------|
| 入院日空值 | 入院日空值 |
| 出生日空值 | 出生日空值 |
| 入院日長度≠8 / 格式不符 | 入院日格式不符 |
| 出生日長度≠8 / 格式不符 | 出生日格式不符 |
| 出院日空白或格式不符 | 出院日不可空白或格式不符 |
| 主診斷碼缺漏 | 第8欄(主診斷碼)格式錯誤 |
| 任一診斷碼長度 > 7 | 診斷碼長度錯誤 |
| 任一手術碼長度 > 7 | 手術碼長度錯誤 |
| 出院日 < 版本生效日(20260101) | 出院日-本版限制日期:115/01/01 |

> 多個錯誤可同時成立(bitmask 累加);錯誤原因串接呈現。長度類錯誤(診斷/手術)致該筆不送分組。

## 案件狀態轉移

```
Received ──validate──▶ Validated ──group──▶ Coded
   │                       │
   └──(驗證未過)──────────┴──────────────▶ Errored(帶 ErrDesc,仍輸出)
```

- 逐筆獨立(FR-016 single):一筆 Errored 不影響其他筆。
- 輸出同時含 Coded 與 Errored(FR-016 both、FR-007 零丟棄)。
- 無法分群者 → Coded 但 Drg 為哨兵碼 / ErrNo 標示。
