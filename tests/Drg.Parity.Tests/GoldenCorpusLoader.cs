using System.Text.Json;

namespace Drg.Parity.Tests;

/// <summary>一筆 golden 對照案件:輸入欄位 + 期望 DRG/MDC/CC(來自 legacy 或官方參考結果)。</summary>
public sealed record GoldenCase(
    string Name,
    string[] Input,            // 固定欄位(同 ClaimCsvReader 之欄位位置)
    string ExpectedDrg,
    string ExpectedMdc,
    string ExpectedCcMark);

/// <summary>載入 golden corpus(原則 II 回歸樣本)。實際樣本由 T012a 取得後置入 GoldenCorpus/cases.json。</summary>
public static class GoldenCorpusLoader
{
    public static IReadOnlyList<GoldenCase> Load(string dir)
    {
        var file = Path.Combine(dir, "cases.json");
        if (!File.Exists(file)) return [];
        return JsonSerializer.Deserialize<List<GoldenCase>>(File.ReadAllText(file)) ?? [];
    }
}
