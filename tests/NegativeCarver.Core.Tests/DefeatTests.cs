using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class DefeatTests
{
    [Fact]
    public void A_fresh_board_has_no_rock_on_the_defeat_line()
    {
        var board = new Board();

        Assert.False(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
        Assert.False(board.HasSolidRockAtOrAbove(PilotRules.InitialRockRows));
        Assert.True(board.HasSolidRockAtOrAbove(-1));
    }

    [Fact]
    public void A_rock_on_or_above_the_defeat_line_is_defeat_and_a_core_is_not()
    {
        var board = new Board();
        Assert.True(board.TrySet(0, PilotRules.DefeatHeight - 1, BlockType.SolidRock));
        Assert.False(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));

        Assert.True(board.TrySet(0, PilotRules.DefeatHeight, BlockType.CoreGem));
        Assert.False(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));

        Assert.True(board.TrySet(1, PilotRules.DefeatHeight, BlockType.SolidRock));
        Assert.True(board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));

        var above = new Board();
        Assert.True(above.TrySet(0, above.Height - 1, BlockType.SolidRock));
        Assert.True(above.HasSolidRockAtOrAbove(PilotRules.DefeatHeight));
    }

    [Fact]
    public void The_drop_limit_defeats_on_the_last_allowed_drop_and_then_stops_counting()
    {
        var match = new MatchState();

        for (var i = 0; i < PilotRules.MaxCutterDrops - 1; i++)
        {
            match.RecordDrop(false);
        }

        Assert.False(match.Defeated);
        Assert.Equal(PilotRules.MaxCutterDrops - 1, match.DropCount);

        match.RecordDrop(false);
        Assert.True(match.Defeated);
        Assert.Equal(PilotRules.MaxCutterDrops, match.DropCount);

        match.RecordDrop(true);
        Assert.Equal(PilotRules.MaxCutterDrops, match.DropCount);
        Assert.True(match.Defeated);
    }

    [Fact]
    public void A_rock_on_the_defeat_line_defeats_before_the_drop_limit()
    {
        var match = new MatchState();

        match.RecordDrop(true);

        Assert.True(match.Defeated);
        Assert.Equal(1, match.DropCount);
    }

    [Fact]
    public void Defeat_blocks_later_input_and_uses_the_pilot_limits()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NegativeCarver.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var cutter = File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Scripts", "CutterController.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var grid = File.ReadAllText(Path.Combine(dir.FullName, "Assets", "Scripts", "GridManager.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var match = File.ReadAllText(Path.Combine(dir.FullName, "src", "NegativeCarver.Core", "MatchState.cs"));
        var update = Body(cutter, "private void Update()");
        var hardDrop = Body(cutter, "public void HardDrop()");
        var left = Body(cutter, "public void MoveLeft()");
        var right = Body(cutter, "public void MoveRight()");
        var rotate = Body(cutter, "public void Rotate()");

        Assert.True(update.IndexOf("if (IsDefeated())", StringComparison.Ordinal) < update.IndexOf("Keyboard.current", StringComparison.Ordinal));
        Assert.Contains("if (IsDefeated() || IsCleared() || grid == null || piece == null)", left, StringComparison.Ordinal);
        Assert.Contains("if (IsDefeated() || IsCleared() || grid == null || piece == null)", right, StringComparison.Ordinal);
        Assert.Contains("if (IsDefeated() || IsCleared() || grid == null || piece == null)", rotate, StringComparison.Ordinal);
        Assert.Contains("if (IsDefeated() || IsCleared() || grid == null || piece == null)", hardDrop, StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", left, StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", right, StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", rotate, StringComparison.Ordinal);
        Assert.DoesNotContain("carveFx", hardDrop, StringComparison.Ordinal);
        Assert.True(left.IndexOf("IsDefeated()", StringComparison.Ordinal) < left.IndexOf("piece.TryMove(-1);", StringComparison.Ordinal));
        Assert.True(right.IndexOf("IsDefeated()", StringComparison.Ordinal) < right.IndexOf("piece.TryMove(1);", StringComparison.Ordinal));
        Assert.True(rotate.IndexOf("IsDefeated()", StringComparison.Ordinal) < rotate.IndexOf("piece.TryRotateClockwise();", StringComparison.Ordinal));
        Assert.True(hardDrop.IndexOf("IsDefeated()", StringComparison.Ordinal) < hardDrop.IndexOf("BeginFall();", StringComparison.Ordinal));
        Assert.DoesNotContain("DropToFirstContact", hardDrop, StringComparison.Ordinal);
        var finish = Body(cutter, "private void FinishDrop(CutterTouch touch)");
        var collapse = finish.IndexOf("LastChainCount = grid.CheckAndCollapseLines();", StringComparison.Ordinal);
        var record = finish.IndexOf("grid.Match.RecordDrop(grid.HasSolidRockOnDefeatLine());", StringComparison.Ordinal);
        var spawn = finish.IndexOf("SpawnNext();", StringComparison.Ordinal);
        Assert.True(collapse >= 0);
        Assert.True(record > collapse);
        Assert.True(spawn > record);
        Assert.Contains("return board.HasSolidRockAtOrAbove(PilotRules.DefeatHeight);", grid, StringComparison.Ordinal);
        Assert.Contains("DropCount >= PilotRules.MaxCutterDrops", match, StringComparison.Ordinal);
        Assert.DoesNotContain("15", match, StringComparison.Ordinal);
        Assert.DoesNotContain("30", match, StringComparison.Ordinal);
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
