# `combo_xicd_chk_yyy` 決策流程

交叉 ICD 規則複核(判定某候選 DRG 的「組合條件 `COMBO_NO`」是否被本筆病歷滿足),對應 `_decompiled_rddt_lib\rddi_lib\rddi0001.cs` 第 2042–4244 行(全引擎最大宗,約 1,800 行)。
由 [`combo_drg_yyy`](combo_drg_yyy_flow.md) 對每個候選列呼叫;回 `0` = 該候選成立(收進候選 `array[]`),非 `0` = 淘汰。

結構上是一個 **以 `H_COMBO_NO` 為鍵的巨型 `switch`(~75 種組合規則)**,每個 case 是一份「配方」,把三個基本檢查 `combo_AX` / `combo_BX` / `combo_CX` 用串接邏輯組起來。

```mermaid
flowchart TD
    Start(["combo_xicd_chk_yyy()"]) --> Sw{"switch H_COMBO_NO<br/>(候選列的組合規則編號)"}

    Sw --> Recipe["對應 case = 一份配方<br/>依序呼叫 combo_AX / combo_BX / combo_CX<br/>(帶不同 ITEM_TYPE 參數)"]

    Recipe --> Pattern{"三種串接樣式"}
    Pattern --> P1["① AND-精煉<br/>num=chkX; if(num==0) num=chkY<br/>(前項命中才續做,兩項皆過才算成立)"]
    Pattern --> P2["② OR-退路<br/>num=chkX; if(num!=0) num=chkY<br/>(前項失敗才試替代)"]
    Pattern --> P3["③ CNT 門檻<br/>num = (CNT<=n) ? -1 : chkY<br/>(命中數需達門檻,如 case28/33/35)"]

    P1 --> Ret
    P2 --> Ret
    P3 --> Ret
    Ret{"最終 num == 0 ?"}
    Ret -- 是 --> Ok(["return 0<br/>候選成立 → combo_drg_yyy 收錄"])
    Ret -- 否 --> No(["return 非0<br/>淘汰此候選"])

    subgraph PRIM["三個基本檢查 (primitives) — 皆對參考視圖計數 CNT 後依 ITEM_TYPE 判定"]
        direction TB
        AX["combo_AX(A*) — 比對「診斷碼」<br/>對 RDDT_MDC_DRGWGT_DRG_XICD<br/>(TREE_DRG + COMBO_NO + ITEM_TYPE)<br/>A/A1/A2/A5/A6 → CNT>0 命中即成立<br/>A3/A4 → CNT==0 不存在才成立<br/>A2/A4 看次診斷(逗號後),A 看主診斷 CM[0]<br/>TREE_DRG=mdc02 走 RDDT_DRG_MDC02"]
        BX["combo_BX(B*) — 比對「手術碼」in_ICDOP_TAB<br/>B5 → RDDT_XICD 開刀房手術(OP_TYPE=2,OR_NOR=Y)<br/>B6/B8/B13 → 逐手術碼 + prepareDRG_XICD_B8_* 組合<br/>B/B1/B2/B4/B5 → CNT>0 成立;B3/B7 → CNT==0 成立<br/>B8 → CNT>0 且 num==0;B13 → 0<num<CNT<br/>單碼 ICD_CODE vs 組合碼 ICD_CODE_PLUS(含 +)分流"]
        CX["combo_CX(C*) — 比對「診斷碼」in_H_CM_CODE<br/>C/C1/C4 → CNT>0 成立<br/>C2 → CNT>1(需 ≥2 筆)<br/>C3 → CNT==0 不存在才成立"]
    end

    P1 -.呼叫.-> PRIM
    P2 -.呼叫.-> PRIM
    P3 -.呼叫.-> PRIM

    classDef ok fill:#e0ffe0,stroke:#0a0;
    classDef no fill:#fff0d0,stroke:#d80;
    classDef prim fill:#eef3ff,stroke:#36c;
    class Ok ok;
    class No no;
    class AX,BX,CX prim;
```

## 重點

### 這是規則「配方表」,不是線性流程
`H_COMBO_NO` 是候選 DRG 列上的欄位(來自 `RDDT_DRG_XICD`),代表「要成為這個 DRG,需滿足哪種診斷/手術組合」。`switch` 的每個 case 把基本檢查依該組合規則串起來。例如:

| COMBO_NO | 配方(摘錄) | 樣式 |
|----------|-------------|------|
| 1 | `combo_AX("A")` | 單一 |
| 16 | `AX("A")` 過 → `AX("A2")` | ① AND-精煉 |
| 17 | `AX("A")` 過 → `BX("B")` | ① 診斷+手術 |
| 20 | `BX("B")` 敗 → `BX("B1")` 過 → `BX("B7")` | ② OR-退路 |
| 28 | `BX("B")`;`CNT≤1 ? 淘汰 : BX("B3")` | ③ CNT 門檻 |
| 5 | `BX("B")`;特定 DRG(0370x)再走 MDC02 的 `AX("A2")` 並改碼 | 特例改碼 |

> 完整 75+ 種對應請直接讀原始碼 `combo_xicd_chk_yyy()` 的 `switch`(第 2045 行起);上表僅示意三種骨架樣式。

### 三個基本檢查的共同模式
皆是:組 `RowFilter`(限定 `TREE_DRG` + `COMBO_NO` + `ITEM_TYPE` + 碼集合)→ 套在參考視圖上算命中數 `CNT` → 依 ITEM_TYPE 決定「**存在**(`CNT>0`)」或「**不存在**(`CNT==0`)」或「**達門檻**(如 C2 需 `CNT>1`)」才回 `0`。

- **`combo_AX` / `combo_CX`** 看**診斷碼**(`in_H_CM_CODE` / `CM[0]`);差別在 C2 要求 ≥2 筆、A2/A4 看次診斷。
- **`combo_BX`** 看**手術碼**(`in_ICDOP_TAB`),且區分單碼(`ICD_CODE`)與含 `+` 的組合碼(`ICD_CODE_PLUS`);B5/B6/B8/B13 另查 `RDDT_XICD` 的開刀房屬性與 `prepareDRG_XICD_B8_*` 組合展開。

### ITEM_TYPE 的「肯定 / 否定」語意(關鍵)
同一檢查的字尾決定是「**要有**」還是「**要沒有**」:
- **肯定型**(命中才成立):`A A1 A2 A5 A6`、`B B1 B2 B4 B5`、`C C1 C4`
- **否定型**(不存在才成立):`A3 A4`、`B3 B7`、`C3`
- **特殊計數**:`B6`(num==CNT 全數命中)、`B8`(CNT>0 但 num==0)、`B13`(0<num<CNT 部分命中)、`C2`(CNT>1)

### 輸出
回 `0` → `combo_drg_yyy` 將該 `TREE_DRG` 收進候選;非 `0` → 淘汰。最終候選集再由 [`tree_yyy`](tree_yyy_flow.md) 決選。
