# `mdc_chk_yyy` 決策流程

MDC(Major Diagnostic Category,主要診斷類別)指派,對應 `_decompiled_rddt_lib\rddi_lib\rddi0001.cs` 第 1610–1746 行。
由 `rddi1000_main` 在 `ecc_chk_yyy` 之後呼叫(見 [`rddi1000_main_flow.md`](rddi1000_main_flow.md) 的 `ECC` 節點),結果寫入 `H_MDC_1`,決定後續走哪條分群分支。

`mdc_chk_yyy` 是**優先序串接**:依序試 MDC 24 → 25 → 1–23,第一個認領的就短路;全部失敗則 `H_MDC_1 = ""`。
回傳碼慣例:`0` = 已指派(成功、短路);非 0(`-1` / `1403`)= 非此類,往下試。

```mermaid
flowchart TD
    Start(["mdc_chk_yyy()"]) --> C24["呼叫 mdc24_chk_yyy()<br/>(重大創傷 MST)"]

    C24 --> Q24a{"mdc_exists_yyy(24)=0<br/>主診斷 CM[0] 在 PDX_MDC 對應 MDC=24<br/>且 icdop_chk(ICD10CM,1,2)=2 ?"}
    Q24a -- 否 --> C25
    Q24a -- 是 --> Q24b{"CM 碼對應 MDC=24 的<br/>相異 CC 群數 ≥ 2 ?"}
    Q24b -- 否 (<2) --> C25
    Q24b -- 是 --> Set24["H_MDC_1 = 24<br/>return 0"]
    Set24 --> Done(["H_MDC_1 已指派 → 結束"])

    C25["呼叫 mdc25_chk_yyy()<br/>(HIV)"]
    C25 --> Q25a{"mdc_exists_yyy(25)=0 ?<br/>主診斷對應 MDC=25"}
    Q25a -- 否 --> C123
    Q25a -- 是 --> Q25b{"掃描 CM[0..19]:<br/>XICD 中 ICD_OP_TYPE=1 且<br/>PRM_ICD_CHK=3 命中 ?"}
    Q25b -- 否 --> C123
    Q25b -- 是 --> Set25["H_MDC_1 = 25<br/>return 0"]
    Set25 --> Done

    C123["呼叫 mdc1to23_chk_yyy()<br/>(一般主類 MDC 01–23)"]
    C123 --> QSexB{"sex_arr = B ?<br/>(性別專屬診斷)"}
    QSexB -- 是 --> SexB{"性別 = F ?"}
    SexB -- 否 (男) --> Set12["H_MDC_1 = 12 (男性生殖)"]
    SexB -- 是 (女) --> Set13a["H_MDC_1 = 13 (女性生殖)"]
    Set12 --> Done
    Set13a --> Done

    QSexB -- 否 --> QSexT{"sex_arr = T ?"}
    QSexT -- 是 --> SexT{"性別 = F ?"}
    SexT -- 否 (男) --> Set11["H_MDC_1 = 11 (腎臟泌尿)"]
    SexT -- 是 (女) --> Set13b["H_MDC_1 = 13 (女性生殖)"]
    Set11 --> Done
    Set13b --> Done

    QSexT -- 否 --> Combo{"PDX_MDC 查 CM[0]+CM[i] 組合鍵<br/>MDC ∈ 1..23 命中 ?"}
    Combo -- 否 --> Single{"PDX_MDC 查 CM[0] 單一主診斷<br/>MDC ∈ 1..23 命中 ?"}
    Combo -- 是 --> SetCombo["H_MDC_1 = 命中列 MDC_CODE<br/>return 0"]
    Single -- 是 --> SetSingle["H_MDC_1 = 命中列 MDC_CODE<br/>return 0"]
    SetCombo --> Done
    SetSingle --> Done

    Single -- 否 --> Fail["三類皆未認領<br/>H_MDC_1 = \"\" (空)<br/>return 1403"]
    Fail --> DoneEmpty(["H_MDC_1 為空 → 結束<br/>(回 rddi1000_main 後續走 UN / 99 處理)"])

    classDef assigned fill:#e0ffe0,stroke:#0a0;
    classDef fail fill:#fff0d0,stroke:#d80;
    class Set24,Set25,Set12,Set13a,Set11,Set13b,SetCombo,SetSingle assigned;
    class Done assigned;
    class Fail,DoneEmpty fail;
```

## 重點

### 優先序與短路
`mdc_chk_yyy` 用 `&&` 串接三個子檢查:`mdc24 != 0 && mdc25 != 0 && mdc1to23 != 0`。任一回 `0`(已指派)即短路,後面不再跑;三者皆非 0 才把 `H_MDC_1` 清空。**順序固定:重大創傷(24)優先於 HIV(25),最後才落到一般主類(1–23)。**

### `mdc_exists_yyy(mdc)` — 共用查表
以主診斷 `CM[0]` + 指定 `MDC_CODE` 查 `RDDT_PDX_MDC`(主診斷→MDC 對照,`rddi1000_reload_db` 載入)。命中回 `0`,否則回 `1403`。24/25 都先靠它確認主診斷有對應該類。

### 各子檢查的額外條件
| 子檢查 | 額外門檻 | 命中結果 |
|--------|----------|----------|
| **MDC 24**(重大創傷) | 須有手術(`icdop_chk=2`)且對應 MDC=24 的**相異 CC 群 ≥ 2**(多部位顯著創傷) | `H_MDC_1 = 24` |
| **MDC 25**(HIV) | CM 碼在 `RDDT_XICD` 中 `ICD_OP_TYPE=1` 且 `PRM_ICD_CHK=3` | `H_MDC_1 = 25` |
| **MDC 1–23** | 見下方性別前置與組合鍵查詢 | `H_MDC_1 = 命中 MDC` |

### MDC 1–23 內部順序
1. **性別專屬旗標優先**(`sex_arr`,由 `icd10cm_chk_yyy` 設定):
   - `B`:男 → **12**(男性生殖)、女 → **13**(女性生殖)
   - `T`:男 → **11**(腎臟泌尿)、女 → **13**
2. **組合主診斷**:`CM[0]+CM[i]` 在 `PDX_MDC` 找 MDC 1–23(優先,處理「主診斷+次診斷」決定類別的情形)。
3. **單一主診斷**:組合找不到才退回 `CM[0]` 單碼查詢。
4. 全失敗 → `H_MDC_1 = ""`,回 `1403`,由 `rddi1000_main` 後續走 `UN` / 99 未分類處理。
