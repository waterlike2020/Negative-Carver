using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class MatchStateTests
{
    [Fact]
    public void Chain_count_starts_at_zero_and_keeps_the_latest_collapse()
    {
        var match = new MatchState();

        Assert.Equal(0, match.ChainCount);

        match.RecordChain(2);
        Assert.Equal(2, match.ChainCount);

        match.RecordChain(0);
        Assert.Equal(0, match.ChainCount);
    }

    [Fact]
    public void Chain_count_can_be_read_and_not_written_from_outside()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var source = File.ReadAllText(Path.Combine(dir.FullName, "src", "NegativeCarver.Core", "MatchState.cs"));
        Assert.Contains("public int ChainCount { get; private set; }", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UnityEngine", source, StringComparison.Ordinal);
    }
}
