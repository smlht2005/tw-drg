using FluentAssertions;
using Xunit;

namespace Drg.Parity.Tests;

public class GoldenCorpusHarnessTests
{
    private static string CorpusDir => Path.Combine(AppContext.BaseDirectory, "GoldenCorpus");

    [Fact(Skip = "T012a:官方 Tw-DRG 115/01/01 測試案例集尚未到位;到位後置入 GoldenCorpus/cases.json 並移除 Skip")]
    public void Corpus_should_be_present_and_nonempty()
    {
        var cases = GoldenCorpusLoader.Load(CorpusDir);
        cases.Should().NotBeEmpty("原則 II 需要 golden 回歸樣本作為 legacy 一致性基準");
    }

    [Fact]
    public void Loader_returns_empty_when_corpus_absent()
    {
        var cases = GoldenCorpusLoader.Load(Path.Combine(AppContext.BaseDirectory, "GoldenCorpus", "__missing__"));
        cases.Should().BeEmpty();
    }
}
