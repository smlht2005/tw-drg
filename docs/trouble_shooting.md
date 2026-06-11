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

---

## TS-002 — `/clear` 後遺失 session 進度:如何在清除前保存

**日期**: 2026-06-12
**影響**: 所有跨 session 的開發工作

### 症狀

`/clear`(或 context 自動壓縮、關掉 Claude Code)之後,新 session **不記得**上一輪做到哪、下一步是什麼,只能從頭重看程式碼推敲,浪費時間且可能漏掉口頭約定的決策。

### 根因

`/clear` 只清空**對話 context**(記憶體),**不動檔案系統**。
凡是只存在於對話裡、沒落地到檔案的資訊(進度、決策、待辦、踩過的雷)清除後就消失。
反之:任何寫進**檔案**的內容都會留存,新 session 可重新讀回。

### 修法 — clear 前的「落地」清單

在 `/clear` 前(或長任務告一段落時),把對話裡的狀態寫進以下三處:

1. **`specs/001-drg-batch-coding/tasks.md` 的「目前進度快照」區塊** — 最重要。
   - 更新日期、各 Phase 完成度、**待辦關鍵路徑**(下一步第一順位)、測試總計。
   - 把完成的 task 標 `[x]`、部分完成標 `[~]` 並在子項註明缺什麼。
   - 新 session 開場第一件事就是讀這份,等於「交接書」。

2. **memory 目錄**(`C:\Users\hungtao.liu\.claude\projects\C--med-S-DRGService-3420\memory\`) — 跨 session 長期事實。
   - 適合:非程式碼可推導的決策、約定、踩雷結論(`type: project | feedback`)。
   - 每則一檔 + frontmatter,並在同目錄 `MEMORY.md` 加一行索引;新 session 會自動載入。

3. **`docs/trouble_shooting.md`(本檔)** — 把這輪解掉的具體問題+根因+修法寫成 TS-NNN。

### 預防 / 操作慣例

- **養成 clear 前先說「更新進度快照」**:請 Claude 把 `tasks.md` 快照與必要 memory 寫好,確認落地後再 `/clear`。
- 快速自檢清單(clear 前):
  - [ ] `tasks.md` 快照日期=今天?關鍵路徑指向正確的下一步?
  - [ ] 這輪有無「非程式碼可推導」的決策需進 memory?
  - [ ] 這輪有無新踩雷需補 TS-NNN?
- 反模式:把進度只留在對話裡、或寫在臨時暫存檔(如 `task.md`、桌面筆記)而不在上述三處 — 新 session 不會去找。
- 提醒:memory 內容反映「寫入當下」的事實;若提到某檔/函式/旗標,新 session 用前要先確認仍存在。

### 附:跨 `/clear` 的記憶機制總表

原則一句話:**寫到磁碟 = 存活;只在對話裡 = clear 後消失。**

**✅ 能跨 clear 存活**

| 機制 | 怎麼用 | 特性 |
|---|---|---|
| memory 檔 | Write 進 `…\memory\*.md` + `MEMORY.md` 索引 | 新 session **自動載入**,最省事的長期記憶 |
| `#` 快捷鍵 | 輸入框打 `# 某件事` | 一行加進 memory / CLAUDE.md |
| `/memory` 指令 | 開 memory / CLAUDE.md 編輯 | 手動檢視、整理 |
| CLAUDE.md(專案+全域) | 寫入專案規則 / 約定 | 每 session 開場必載入,優先級高於 memory |
| `/job-handover` skill | 產生結構化交接文件 | 大里程碑 / 交接他人時用 |
| `/daily-log` skill | 彙整當日 git/WIP/session | append 到單一日期檔,可日後搜尋 |
| git commit / push | 程式碼落地 | 待 push 的 commit 不算落地,記得推 |
| docs / specs 檔 | `tasks.md`、本檔 | 一般文件,永久存活 |

**❌ 反而會 loss(別依賴做交接)**

| 機制 | 為何不可靠 |
|---|---|
| TaskCreate / TaskUpdate 待辦清單 | in-session todo 是**對話範圍**,clear 後清空 → 進度要落到 `tasks.md` |
| `/compact` | 只**壓縮**對話、非存檔,且可能丟細節 |
| 對話裡的口頭結論 | 沒寫檔就沒了 |

**本專案最小固定組合**:① `tasks.md` 快照(結構化進度)② memory 檔(非程式碼可推導決策)③ git push(程式碼)。三者到位即可安全 `/clear`。
