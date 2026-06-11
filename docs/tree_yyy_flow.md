# `tree_yyy` 決策流程

DRG 候選決選步驟,對應 `_decompiled_rddt_lib\rddi_lib\rddi0001.cs` 第 1921–2040 行。
由 `rddi1000_main` 在分群尾段呼叫(見 [`rddi1000_main_flow.md`](rddi1000_main_flow.md) 的 `tree_yyy` 節點),輸入候選 DRG 陣列 `H_TEMP_DRG[]`,輸出最終 `v_DRG_1`。

```mermaid
flowchart TD
    Start(["tree_yyy(h_drg[])<br/>輸入候選 DRG 陣列 (temp_drg_cnt 筆)"]) --> Norm["階段1：候選 DRG 正規化合併<br/>逐筆掃描 h_drg[]"]

    Norm --> N1{"h_drg[i] ∈ {10402,10403} ?"}
    N1 -- 是 --> N1a["將 {10409,10410,10404} 取代為 h_drg[i]"]
    N1 -- 否 --> N2{"h_drg[i] ∈ {10409,10410} ?"}
    N2 -- 是 --> N2a["將 {10404} 取代為 h_drg[i]"]
    N1a --> N3
    N2a --> N3
    N2 -- 否 --> N3

    N3{"h_drg[i] ∈ {10502,10503} ?"}
    N3 -- 是 --> N3a["將 {10509,10510,10504} 取代為 h_drg[i]"]
    N3 -- 否 --> N4{"h_drg[i] ∈ {10509,10510} ?"}
    N4 -- 是 --> N4a["將 {10504} 取代為 h_drg[i]"]
    N3a --> N5
    N4a --> N5
    N4 -- 否 --> N5

    N5{"h_drg[i] ∈ {47701..47704} ?"}
    N5 -- 是 --> N5a["將 {46801..46804} 取代為 h_drg[i]"]
    N5 -- 否 --> N6{"h_drg[i] ∈ {28901,28902} ?"}
    N5a --> N6
    N6 -- 是 --> N6a["將 {290} 取代為 h_drg[i]"]
    N6 -- 否 --> NLoop{"還有下一筆 h_drg[i] ?"}
    N6a --> NLoop
    NLoop -- 是 --> N1
    NLoop -- 否 --> Date

    Date["階段2：TEMP_OUT_DATE 若空<br/>→ 預設為今天"]
    Date --> Query["階段3：查 dt_RDDT_MDC_DRGWGT<br/>WHERE TREE_DRG ∈ 候選集合 (sts)<br/>取得 TREE_MDC_NO / TREE_DRG / TREE_NO / TREE_WGT / AVG_EXP"]

    Query --> Loop{"逐筆掃描符合的 DRGWGT 列"}
    Loop -- "下一列" --> M15{"H_MDC_1 = 15 ?<br/>(新生兒)"}

    M15 -- 是 --> Pick15{"TREE_NO < 目前最小值 ?"}
    Pick15 -- 是 --> Set15["記錄此列:最小 TREE_NO<br/>v_DRG_1 = TREE_DRG"]
    Pick15 -- 否 --> Loop
    Set15 --> Loop

    M15 -- 否 --> WgtZero{"TREE_WGT = 0 ?"}
    WgtZero -- 是 --> Mean["WGT_MEAN = H_MED_AMT / AVG_EXP<br/>以點數估算權重代入 TREE_WGT"]
    WgtZero -- 否 --> PickW
    Mean --> PickW

    PickW{"TREE_WGT > 目前最大權重<br/>或 (權重相等 且 TREE_NO 較小) ?"}
    PickW -- 是 --> SetW["更新最佳:最大 TREE_WGT<br/>tie-break 最小 TREE_NO<br/>v_DRG_1 = TREE_DRG"]
    PickW -- 否 --> Loop
    SetW --> Loop

    Loop -- "掃描完畢" --> Done(["輸出 v_DRG_1 (決選 DRG)"])

    classDef phase fill:#e8f0ff,stroke:#36c;
    classDef win fill:#e0ffe0,stroke:#0a0;
    class Set15,SetW win;
    class Done win;
```

## 重點

### 階段1：候選 DRG 正規化合併
把同群中「較細分 / 較高階」的 DRG 覆蓋掉「概括 / 較低階」碼,確保後續決選不會選到被取代的版本。固定合併規則(hard-coded):

| 觸發碼 | 取代目標 | 對應分群 |
|--------|----------|----------|
| `10402` / `10403` | `10409` `10410` `10404` | MDC 01 神經 |
| `10409` / `10410` | `10404` | MDC 01 神經 |
| `10502` / `10503` | `10509` `10510` `10504` | MDC 01 |
| `10509` / `10510` | `10504` | MDC 01 |
| `47701`–`47704` | `46801`–`46804` | MDC 08 肌肉骨骼 |
| `28901` / `28902` | `290` | MDC 05 循環 |

### 階段3–4:依權重決選
- 候選集合(正規化後的 `H_TEMP_DRG[]`)比對參考表 `RDDT_MDC_DRGWGT`(由 `rddi1000_reload_db` 載入)。
- **MDC 15(新生兒)**:不看權重,選 **`TREE_NO` 最小**(決策樹順位最前)的 DRG。
- **其他 MDC**:選 **`TREE_WGT` 最大**;若該列權重為 0,則以 `醫療費用 / AVG_EXP`(點數 ÷ 平均費用)推算權重代入;權重相同時以 **`TREE_NO` 最小** tie-break。

### 輸出
`v_DRG_1` = 決選出的 DRG,回到 `rddi1000_main` 續做 `v_MDC_1` 對應與心臟特例細分。
