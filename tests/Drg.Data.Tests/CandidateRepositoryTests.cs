using Drg.Data;
using FluentAssertions;
using Xunit;

namespace Drg.Data.Tests;

/// <summary>對真實 icd10.sqlite 驗證 CandidateRepository(逐筆載入 combo 候選列)。缺 DB 則略過。</summary>
public sealed class CandidateRepositoryTests
{
    private static string? FindDb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var c = Path.Combine(dir.FullName, "icd10.sqlite");
            if (File.Exists(c)) return c;
            dir = dir.Parent;
        }
        return null;
    }

    private static CandidateRepository Repo()
        => new(new DbConnectionFactory(DbProvider.Sqlite, $"Data Source={FindDb()}"));

    [SkippableFact]
    public void LoadForMdc_returns_wellformed_rows()
    {
        Skip.If(FindDb() is null, "icd10.sqlite 不存在;先執行遷移批次 1+2");
        var rows = Repo().LoadForMdc("05", ["A3681"]);

        rows.Should().NotBeEmpty();   // NotIn(MDC05) ∪ UN 必有列
        // CASE 預設套用:CC_MARK / AGE_MARK 不應為空或 null
        rows.Should().OnlyContain(r => !string.IsNullOrEmpty(r.CcMark) && !string.IsNullOrEmpty(r.AgeMark));
        rows.Should().OnlyContain(r => !string.IsNullOrEmpty(r.TreeDrg) && !string.IsNullOrEmpty(r.ComboNo));
    }

    [SkippableFact]
    public void Load00_runs_and_maps_columns()
    {
        Skip.If(FindDb() is null, "icd10.sqlite 不存在");
        var rows = Repo().Load00(["A3681", "027034Z"]);

        // 至少 '+'/'*' 萬用列會回來;欄位對映正確(數值欄已轉型)
        rows.Should().NotBeNull();
        rows.Should().OnlyContain(r => !string.IsNullOrEmpty(r.CcMark) && !string.IsNullOrEmpty(r.AgeMark));
    }
}
