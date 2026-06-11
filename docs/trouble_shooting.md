# Troubleshooting

本檔記錄已發生過的問題、根因與修法,供日後快速排查。

---

## TS-001 — Mermaid 圖在 GitHub 無法渲染:`Parse error ... got 'STR'`

**日期**: 2026-06-11
**影響檔案**: `docs/rddi1000_main_flow.md`、`docs/mdc_chk_yyy_flow.md`

### 症狀

GitHub 預覽 Mermaid 圖時顯示紅框錯誤:

```
Unable to render rich display
Parse error on line 41:
...類<br/>H_MDC_1 = ( \ (空)<br/>return 1403
----------------------^
Expecting 'SQE', 'DOUBLECIRCLEEND', 'PE', '-)', 'STADIUMEND', 'SUBROUTINEEND',
'PIPE', 'CYLINDEREND', 'DIAMOND_STOP', 'TAGEND', 'TRAPEND', 'INVTRAPEND',
'UNICODE_TEXT', 'TEXT', 'TAGSTART', got 'STR'
```

(另一檔同類錯誤在 `rddi1000_main_flow.md` line 34:`...combo_drg_yyy(\"00\")"] C00 --> Q00{`)

### 根因

Mermaid 的 flowchart 解析器**不支援在節點標籤 `"..."` 內使用反斜線跳脫的雙引號 `\"`**。
解析器讀到內層的 `"` 就誤判字串提前結束,接著遇到剩餘文字 → 報 `got 'STR'`。

問題寫法:

```
C00["... combo_drg_yyy(\"00\")"]
Fail["... H_MDC_1 = \"\" (空) ..."]
```

### 修法

把 `\"` 改成 HTML 實體 **`&quot;`**(Mermaid 會解碼,渲染後仍顯示為 `"`,外觀與語意不變):

```
C00["... combo_drg_yyy(&quot;00&quot;)"]
Fail["... H_MDC_1 = &quot;&quot; (空) ..."]
```

修復 commit:`e7c0d01`。

### 預防

- Mermaid 標籤需顯示雙引號時用 `&quot;`(或 `#quot;`),**絕不要用 `\"`**。
- 括號 `()`、`+`、`/` 等特殊字元只要包在 `"..."` 標籤內即安全,無需跳脫。
- 解析器只回報**第一個**錯誤;修掉後若仍紅框,代表後面還有第二處,需再查。
- 快速自檢:`grep -rn '\\"' docs/` 應為 0 筆。
