using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class StageTests
{
    [Fact]
    public void A_finished_stage_advances_and_the_hud_names_it_above_the_drops()
    {
        var match = new MatchState();
        Assert.Equal(PilotRules.FirstStage, match.Stage);
        match.RecordDrop(false);
        match.Reset();
        Assert.Equal(PilotRules.FirstStage, match.Stage);
        Assert.Equal(0, match.DropCount);

        match.RecordChain(2);
        match.RecordDrop(true);
        Assert.Equal(2, match.ChainCount);
        Assert.True(match.Defeated);

        match.AdvanceStage();
        Assert.Equal(PilotRules.FirstStage + 1, match.Stage);
        Assert.Equal(0, match.DropCount);
        Assert.Equal(0, match.ChainCount);
        Assert.False(match.Defeated);

        match.AdvanceStage();
        Assert.Equal(PilotRules.FirstStage + 2, match.Stage);

        var root = FindRepositoryRoot();
        var hud = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PilotHud.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var scoreLine = hud.IndexOf("scoreText = CreateLine(canvasObject.transform, -16f);", StringComparison.Ordinal);
        var stageLine = hud.IndexOf("stageText = CreateLine(canvasObject.transform, -56f);", StringComparison.Ordinal);
        var dropLine = hud.IndexOf("dropText = CreateLine(canvasObject.transform, -96f);", StringComparison.Ordinal);
        Assert.True(scoreLine >= 0);
        Assert.True(stageLine > scoreLine);
        Assert.True(dropLine > stageLine);

        var update = Body(hud, "private void Update()");
        var score = update.IndexOf("scoreText.text = \"Score \" + grid.Match.ShownScore(grid.CheckCoreGemFreed());", StringComparison.Ordinal);
        var stage = update.IndexOf("stageText.text = \"Stage \" + grid.Match.Stage;", StringComparison.Ordinal);
        var drops = update.IndexOf("dropText.text = \"Drops \" + grid.Match.DropCount;", StringComparison.Ordinal);
        Assert.True(score >= 0);
        Assert.True(stage > score);
        Assert.True(drops > stage);

        var advance = Body(grid, "public void AdvanceStage()");
        var step = advance.IndexOf("Match.AdvanceStage();", StringComparison.Ordinal);
        var rebuild = advance.IndexOf("Restart();", StringComparison.Ordinal);
        Assert.True(step >= 0);
        Assert.True(rebuild > step);
        Assert.Contains("grid.AdvanceStage();", Body(hud, "private void RestartMatch()"), StringComparison.Ordinal);
    }

    private static string Body(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
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

        throw new InvalidOperationException(signature);
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

        throw new InvalidOperationException("NegativeCarver.sln was not found above the test output directory.");
    }
}
