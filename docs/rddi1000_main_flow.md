# `rddi1000_main` 決策流程

Tw-DRG 單筆分組主流程,對應 `_decompiled_rddt_lib\rddi_lib\rddi0001.cs` 第 604–979 行。
此圖由反編譯原始碼重建,僅描述控制流;實際分群規則資料在 `icd10.sdf` 的 `RDDT_*` 參考表。

```mermaid
flowchart TD
    Start(["rddi1000_main(單筆)"]) --> Init["init_vars + 載入 host variables<br/>H_FEE_YM / H_*_DATE / H_CM_CODE / H_OP_CODE"]
    Init --> Age{"H_IN_DATE == 0000/00/00 ?"}
    Age -- "是 + part_code=903 且有 child_birthday" --> AgeChild["以 child_birthday 算<br/>ages/months/days"]
    Age -- "是, 其他" --> AgeAppl["以 APPL_BEG_DATE + birthday 算年齡"]
    Age -- "否 + part_code=903 且有 child_birthday" --> AgeInChild["以 IN_DATE + child_birthday 算年齡"]
    Age -- "否, 其他" --> AgeIn["以 IN_DATE + birthday 算年齡"]

    AgeChild --> CM
    AgeAppl --> CM
    AgeInChild --> CM
    AgeIn --> CM

    CM{"icd10cm_chk_yyy() == 0 ?<br/>(ICD-10-CM 驗證)"}
    CM -- "否 (驗證失敗)" --> RetM1(["回傳 -1<br/>err_code / cc / mdc 帶出"])

    CM -- "是" --> ECC["ecc_chk_yyy() → CC_MARK / CC_CODE<br/>(細節見 ecc_chk_yyy_flow.md)<br/>mdc_chk_yyy() → 指派 MDC<br/>(細節見 mdc_chk_yyy_flow.md)<br/>帶出 err/age/cc/mdc"]

    ECC --> S1{"MDC = 19 或 20 ?<br/>(精神科)"}
    S1 -- 是 --> DXXX(["DRG = XXX → 回傳 0"])
    S1 -- 否 --> S2{"XICD Type1_Chk1<br/>命中主診斷 CM[0] ?"}
    S2 -- 是 --> DYYY(["DRG = YYY → 回傳 0"])
    S2 -- 否 --> S3{"XICD Type1 且<br/>PRM_ICD_CHK ∈ {3,4} ?"}
    S3 -- 是 --> DZZZ(["DRG = ZZZ → 回傳 0"])
    S3 -- 否 --> S4{"XICD Type1_Chk6 命中 ?"}
    S4 -- 是 --> DWWW(["DRG = WWW → 回傳 0"])
    S4 -- 否 --> S5{"XICD Type1_Chk8 命中 ?"}
    S5 -- 是 --> DGGG(["DRG = GGG → 回傳 0"])
    S5 -- 否 --> S6{"ophhh_chk_yyy() == 0 ?"}
    S6 -- 是 --> DHHH(["DRG = HHH → 回傳 0"])

    S6 -- 否 --> OP["掃描 H_OP_CODE[0..19] 建 ICDOP_TAB<br/>Type2_ChkX 命中 → v_icd_no_op = OR (開刀房手術)<br/>op_count > 1 → Op_Code_Rtn_yyy() 多手術展開"]

    OP --> C00["v_MDC_1 = 00<br/>Merge RDDT_MDC_DRG_XICD_00<br/>combo_drg_yyy(\"00\")"]
    C00 --> Q00{"v_DRG_1 已產生 ?"}
    Q00 -- "是, 且不在 {48201,48202,48301,48302}" --> Ret00(["mdc/drg = 00組結果 → 回傳 0"])
    Q00 -- "是, 屬 482xx/483xx" --> Stash["暫存 v_MDC_2 / v_DRG_2"]
    Q00 -- 否 --> Surg

    Stash --> Surg
    Surg["v_MDC_1 = H_MDC_1<br/>若 v_icd_no_op = OR → combo_drg_yyy() (外科)<br/>(細節見 combo_drg_yyy_flow.md)<br/>H_TEMP_DRG[0] = v_DRG_1"]
    Surg --> M14{"MDC=14 且 TEMP_DRG[0] 空 ?<br/>(產科)"}
    M14 -- 是 --> ICD9["mdc_icd9cm_yyy()"]
    M14 -- 否 --> DrgChk
    ICD9 --> DrgChk

    DrgChk{"drg_chk_yyy(H_TEMP_DRG) == 2 ?"}
    DrgChk -- "否 (未匹配)" --> UNF{"MDC ∉ {15,17,18,22,23} ?"}
    UNF -- 是 --> DoUNF["v_MDC_1 = UN<br/>UNF_MDC_99_CHK_yyy()"]
    UNF -- 否 --> Depp
    DoUNF --> Depp
    DrgChk -- "是" --> M15a{"MDC = 15 ?"}
    M15a -- 是 --> SetD3["v_DRG_3 = v_DRG_1"]
    M15a -- 否 --> Depp
    SetD3 --> Depp

    Depp["DEPP 過濾: TREE_DRG IN (TEMP_DRG[0..2])<br/>orp_cnt = 符合筆數"]
    Depp --> Orp{"orp_cnt == 0 ?<br/>(找不到外科分群)"}
    Orp -- 是 --> Med["v_depflag = M (內科)<br/>重掃 NOR 手術 → combo_drg_yyy()<br/>mdc_icd9cm_yyy()"]
    Orp -- 否 --> Tree
    Med --> Tree

    Tree["若 MDC≠15: TEMP_DRG[2]=v_DRG_2, TEMP_DRG[3]=v_DRG_3<br/>tree_yyy(H_TEMP_DRG) — DRG 決策樹 + 權重<br/>(細節見 tree_yyy_flow.md)"]
    Tree --> MdcPick["v_MDC_1 = H_MDC_1<br/>若 v_DRG_2 = v_DRG_1 → v_MDC_1 = v_MDC_2"]
    MdcPick --> Cardiac{"out_date ≥ 2021/01/01<br/>且 v_DRG_1 ∈ {11201, 11602} ?"}
    Cardiac -- 是 --> SpOp["比對 SP_OP / SP_OP1 / SP_OP2 / SP_OP5 / SP_OP6<br/>心臟/ECMO 手術碼 → 細分 11601/11602/11201/11202"]
    Cardiac -- 否 --> Final
    SpOp --> Final

    Final["mdc_code = v_MDC_1<br/>drg = v_DRG_1"]
    Final --> Empty{"drg 為空 ?"}
    Empty -- 是 --> RetM2(["回傳 -2 (無法分群)"])
    Empty -- 否 --> Ret0(["回傳 0 (成功)"])

    classDef sentinel fill:#ffe0e0,stroke:#c00;
    classDef ok fill:#e0ffe0,stroke:#0a0;
    classDef err fill:#fff0d0,stroke:#d80;
    class DXXX,DYYY,DZZZ,DWWW,DGGG,DHHH sentinel;
    class Ret00,Ret0 ok;
    class RetM1,RetM2 err;
```

## 重點

- **🔴 哨兵碼(早退)**:`XXX`(MDC 19/20 精神科)、`YYY/ZZZ/WWW/GGG`(XICD 規則命中)、`HHH`(`ophhh_chk`)。命中即 `return 0`,代表特殊或不可分群案件,不再進入分群樹。
- **🟢 正常出口**:`return 0`(00 組直接命中,或最終 `tree_yyy` 完成)。
- **🟡 錯誤出口**:`-1`(ICD-10-CM 驗證失敗)、`-2`(跑完仍無 DRG)。
- **三條年齡計算路徑**:`part_code=903`(新生兒就附母親案件)改用 `child_birthday`。
- **外科 vs 內科分流**:先試外科(`v_depflag=P` + `combo_drg_yyy`),DEPP 過濾 `orp_cnt=0` 找不到才退回內科(`v_depflag=M`)。
- **心臟特例**:`11201/11601/11602` 在 2021/01/01 後用硬編碼手術清單(ECMO 等 `02xxxxx` 碼)細分,為版本演進補丁。

## 回傳碼

| 回傳值 | 意義 | 出口 |
|--------|------|------|
| `0` | 分群成功(含哨兵碼) | `mdc_code` / `drg` 已填 |
| `-1` | ICD-10-CM 驗證失敗 | 帶出 err/cc/mdc |
| `-2` | 流程跑完仍無 DRG | `drg` 為空 |
