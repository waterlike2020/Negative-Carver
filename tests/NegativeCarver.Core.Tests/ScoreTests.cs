using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class ScoreTests
{
    [Fact]
    public void A_clear_pays_stage_base_plus_unused_drops_and_later_stages_add_up()
    {
        var match = new MatchState();
        Assert.Equal(0, match.Score);
        Assert.Equal(0, match.ShownScore(false));

        for (var i = 0; i < 10; i++)
        {
            match.RecordDrop(false);
        }

        var first = MatchState.StageClearScore(PilotRules.FirstStage, 10);
        Assert.Equal((PilotRules.ClearScorePerStage * PilotRules.FirstStage) + ((PilotRules.MaxCutterDrops - 10) * PilotRules.SpareDropScore), first);
        Assert.Equal(first, match.ShownScore(true));
        Assert.Equal(0, match.Score);

        match.BankStageClear();
        Assert.Equal(first, match.Score);
        Assert.Equal(first, match.ShownScore(true));
        match.BankStageClear();
        Assert.Equal(first, match.Score);

        match.AdvanceStage();
        Assert.Equal(first, match.Score);
        Assert.Equal(PilotRules.FirstStage + 1, match.Stage);
        Assert.Equal(0, match.DropCount);
        Assert.Equal(first, match.ShownScore(false));

        for (var i = 0; i < PilotRules.MaxCutterDrops; i++)
        {
            match.RecordDrop(false);
        }

        var second = MatchState.StageClearScore(PilotRules.FirstStage + 1, PilotRules.MaxCutterDrops);
        Assert.Equal(PilotRules.ClearScorePerStage * (PilotRules.FirstStage + 1), second);
        match.BankStageClear();
        match.AdvanceStage();
        Assert.Equal(first + second, match.Score);
        Assert.Equal(first + second, match.ShownScore(false));
    }

    [Fact]
    public void A_failed_stage_adds_nothing_and_reset_keeps_the_bank()
    {
        var match = new MatchState();
        match.RecordDrop(false);
        match.BankStageClear();
        var banked = match.Score;
        match.AdvanceStage();

        match.RecordDrop(true);
        Assert.True(match.Defeated);
        Assert.Equal(banked, match.ShownScore(false));
        match.AdvanceStage();
        Assert.Equal(banked, match.Score);

        match.Reset();
        Assert.Equal(banked, match.Score);
        Assert.Equal(0, match.DropCount);
        Assert.Equal(PilotRules.ClearScorePerStage, MatchState.StageClearScore(PilotRules.FirstStage, PilotRules.MaxCutterDrops + 3));
    }

    [Fact]
    public void The_next_button_banks_only_a_freed_core_before_the_stage_advances()
    {
        var root = FindRepositoryRoot();
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var advance = MethodBody(grid, "public void AdvanceStage()");
        var freed = advance.IndexOf("if (CheckCoreGemFreed())", StringComparison.Ordinal);
        var bank = advance.IndexOf("Match.BankStageClear();", StringComparison.Ordinal);
        var step = advance.IndexOf("Match.AdvanceStage();", StringComparison.Ordinal);
        var rebuild = advance.IndexOf("Restart();", StringComparison.Ordinal);
        Assert.True(freed >= 0);
        Assert.True(bank > freed);
        Assert.True(step > bank);
        Assert.True(rebuild > step);
        Assert.Contains("Match.BankStageClear();", BodyOf(advance, "if (CheckCoreGemFreed())"), StringComparison.Ordinal);
    }

    private static string BodyOf(string source, string header)
    {
        var start = source.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(open, i - open + 1);
                }
            }
        }

        return string.Empty;
    }

    private static string MethodBody(string source, string signature)
    {
        return BodyOf(source, signature);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("NegativeCarver.sln");
    }
}
