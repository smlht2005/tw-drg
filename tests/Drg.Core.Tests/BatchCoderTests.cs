using Drg.Core;
using Drg.Core.Configuration;
using Drg.Core.Engine;
using Drg.Core.Io;
using Drg.Core.Models;
using Drg.Core.Ruleset;
using FluentAssertions;
using Xunit;

namespace Drg.Core.Tests;

public class BatchCoderTests
{
    private static GroupingRuleset Ruleset => new() { Version = "test-v1" };

    private sealed class FakeReader(IEnumerable<ClaimEncounter> items) : IClaimReader
    {
        public IEnumerable<ClaimEncounter> Read(string path) => items;
    }

    private sealed class StubGrouper(Func<ClaimEncounter, CodingResult> fn) : IDrgGrouper
    {
        public CodingResult Group(ClaimEncounter claim, GroupingRuleset ruleset) => fn(claim);
    }

    private sealed class CapturingWriter : IResultWriter
    {
        public readonly List<CodingResult> Written = [];
        public void Write(string path, IEnumerable<CodingResult> results) => Written.AddRange(results);
    }

    [Fact]
    public void Run_counts_coded_and_errored_stamps_version_and_times()
    {
        var claims = new[] { new ClaimEncounter { RowNum = 1 }, new ClaimEncounter { RowNum = 2 } };
        var grouper = new StubGrouper(c => c.RowNum == 1
            ? new CodingResult { RowNum = 1, Drg = "05010" }
            : new CodingResult { RowNum = 2, ErrDesc = "出生日格式不符" });
        var writer = new CapturingWriter();
        var coder = new BatchCoder(new FakeReader(claims), grouper, writer);

        var job = coder.Run("in.csv", "out.csv", Ruleset, new DrgOptions { BatchMax = 10 });

        job.TotalCount.Should().Be(2);
        job.CodedCount.Should().Be(1);
        job.ErrorCount.Should().Be(1);
        job.RulesetVersion.Should().Be("test-v1");          // FR-013
        job.Status.Should().Be(BatchStatus.Completed);
        job.StartedAt.Should().NotBeNull();
        job.EndedAt.Should().NotBeNull();
        writer.Written.Should().HaveCount(2);               // 零丟棄(FR-007)
    }

    [Fact]
    public void Run_throws_when_exceeding_batch_max()
    {
        var claims = Enumerable.Range(1, 3).Select(i => new ClaimEncounter { RowNum = i });
        var coder = new BatchCoder(new FakeReader(claims), new StubGrouper(_ => new CodingResult()), new CapturingWriter());

        var act = () => coder.Run("in", "out", Ruleset, new DrgOptions { BatchMax = 2 });

        act.Should().Throw<BatchTooLargeException>();        // SC-004
    }
}
