using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Reset_clears_the_chain_the_drops_and_defeat()
    {
        var match = new MatchState();
        match.RecordChain(3);
        match.RecordDrop(true);

        match.Reset();

        Assert.Equal(0, match.ChainCount);
        Assert.Equal(0, match.DropCount);
        Assert.False(match.Defeated);

        match.RecordDrop(false);
        Assert.Equal(1, match.DropCount);
        Assert.False(match.Defeated);
    }

    [Fact]
    public void Restarting_the_queue_deals_a_new_order()
    {
        var queue = new CutterQueue(1);
        var first = queue.Next().Shape;
        queue.Next();
        queue.Reset();
        var shapes = new System.Collections.Generic.List<CutterShape>();
        for (var i = 0; i < 7; i++)
        {
            shapes.Add(queue.Next().Shape);
        }

        Assert.Equal(7, shapes.Distinct().Count());
        Assert.Contains(first, shapes);
        Assert.Equal(shapes[0], queue.Next().Shape);
    }

    [Fact]
    public void A_finished_match_shows_the_panel_and_restart_builds_a_new_board()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var root = dir.FullName;
        var hud = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "PilotHud.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var cutter = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var update = Body(hud, "private void Update()");
        var status = update.IndexOf("statusText.text = MatchStatus.Describe(grid.CheckCoreGemFreed(), grid.Match.Defeated);", StringComparison.Ordinal);
        var show = update.IndexOf("resultPanel.SetActive(statusText.text != MatchStatus.Playing);", StringComparison.Ordinal);
        var title = update.IndexOf("titleText.text = statusText.text;", StringComparison.Ordinal);
        Assert.True(status >= 0);
        Assert.True(show > status);
        Assert.True(title > show);
        Assert.Contains("label.text = \"Next\";", hud, StringComparison.Ordinal);
        Assert.Contains("button.onClick.AddListener(RestartMatch);", hud, StringComparison.Ordinal);
        Assert.Contains("grid.AdvanceStage();", Body(hud, "private void RestartMatch()"), StringComparison.Ordinal);
        Assert.Contains("panel.AddComponent<Image>();", hud, StringComparison.Ordinal);
        Assert.Contains("if (EventSystem.current != null)", hud, StringComparison.Ordinal);
        Assert.Contains("eventSystem.AddComponent<EventSystem>();", hud, StringComparison.Ordinal);
        Assert.Contains("eventSystem.AddComponent<InputSystemUIInputModule>();", hud, StringComparison.Ordinal);

        var play = Body(cutter, "private void Update()");
        var guard = play.IndexOf("if (IsDefeated() || IsCleared())", StringComparison.Ordinal);
        var keyboard = play.IndexOf("Keyboard.current", StringComparison.Ordinal);
        Assert.True(guard >= 0);
        Assert.True(keyboard > guard);

        var restart = Body(grid, "public void Restart()");
        var clear = restart.IndexOf("ClearTiles();", StringComparison.Ordinal);
        var board = restart.IndexOf("board = new Board(new System.Random());", StringComparison.Ordinal);
        var match = restart.IndexOf("Match.Reset();", StringComparison.Ordinal);
        var spawn = restart.IndexOf("SpawnInitialRocks();", StringComparison.Ordinal);
        var cutterRestart = restart.IndexOf("cutter.Restart();", StringComparison.Ordinal);
        Assert.True(clear >= 0);
        Assert.True(board > clear);
        Assert.True(match > board);
        Assert.True(spawn > match);
        Assert.True(cutterRestart > spawn);
        Assert.DoesNotContain("Defeated", restart, StringComparison.Ordinal);
        Assert.DoesNotContain("return", restart, StringComparison.Ordinal);
        Assert.Contains("Destroy(tile.gameObject);", Body(grid, "private void ClearTiles()"), StringComparison.Ordinal);

        var cutterRestartBody = Body(cutter, "public void Restart()");
        var shuffle = cutterRestartBody.IndexOf("queue.Reset();", StringComparison.Ordinal);
        var deal = cutterRestartBody.IndexOf("SpawnNext();", StringComparison.Ordinal);
        Assert.True(shuffle >= 0);
        Assert.True(deal > shuffle);
        Assert.DoesNotContain("IsDefeated", cutterRestartBody, StringComparison.Ordinal);
        Assert.DoesNotContain("IsCleared", cutterRestartBody, StringComparison.Ordinal);

        var hardDrop = Body(cutter, "private void FinishDrop(CutterTouch touch)");
        var record = hardDrop.IndexOf("grid.Match.RecordDrop(grid.HasSolidRockOnDefeatLine());", StringComparison.Ordinal);
        var stop = hardDrop.IndexOf("if (IsDefeated() || IsCleared())", record, StringComparison.Ordinal);
        var hide = hardDrop.IndexOf("ClearPreview();", StringComparison.Ordinal);
        var leave = hardDrop.IndexOf("return;", hide, StringComparison.Ordinal);
        var next = hardDrop.IndexOf("SpawnNext();", StringComparison.Ordinal);
        Assert.True(record >= 0);
        Assert.True(stop > record);
        Assert.True(hide > stop);
        Assert.True(leave > hide);
        Assert.True(next > leave);
        Assert.Contains("return grid != null && grid.CheckCoreGemFreed();", Body(cutter, "private bool IsCleared()"), StringComparison.Ordinal);
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

        throw new InvalidOperationException("Block was not closed: " + signature);
    }
}
