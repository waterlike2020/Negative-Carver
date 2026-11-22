using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class ChainTests
{
    [Fact]
    public void Fresh_board_collapses_nothing()
    {
        var board = new Board();
        var before = Snapshot(board);

        var rows = board.CollapseChains();

        Assert.Empty(rows);
        Assert.Equal(-1, board.NextChainRow());
        AssertSame(board, before);
    }

    [Fact]
    public void One_hole_is_one_chain_and_drops_the_core_once()
    {
        var hole = PilotRules.CoreY + 1;
        var board = HoledBoard(hole, hole + 1);
        var expected = HoledBoard(hole, hole + 1);
        expected.ApplyGravity();

        var rows = board.CollapseChains();

        Assert.Equal(new[] { hole }, rows);
        Assert.Equal(-1, board.NextChainRow());
        AssertSame(board, Snapshot(expected));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, hole));
    }

    [Fact]
    public void Two_holes_fall_again_without_input_and_count_two()
    {
        var lower = PilotRules.CoreY;
        var upper = lower + 3;
        var board = TwoHoles(lower, upper);
        var expected = TwoHoles(lower, upper);
        expected.ApplyGravity();

        var rows = board.CollapseChains();

        Assert.Equal(new[] { upper, lower }, rows);
        Assert.Equal(-1, board.NextChainRow());
        AssertSame(board, Snapshot(expected));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, upper - 1));
        Assert.Equal(1, CountGems(board));
    }

    [Fact]
    public void A_preview_above_a_hole_does_not_fall_or_count_as_a_chain()
    {
        var board = new Board();
        var hole = PilotRules.InitialRockRows - 1;
        ClearRow(board, hole);
        Assert.True(board.TrySet(0, hole + 1, BlockType.CutterPreview));

        var rows = board.CollapseChains();

        Assert.Empty(rows);
        Assert.Equal(BlockType.CutterPreview, board.Get(0, hole + 1));
        Assert.Equal(BlockType.Empty, board.Get(0, hole));
    }

    [Fact]
    public void Collapse_stops_when_nothing_remains_above_a_hole()
    {
        var board = new Board();
        var before = Snapshot(board);

        var rows = board.CollapseChains(board.Height);

        Assert.Empty(rows);
        AssertSame(board, before);
    }

    [Fact]
    public void Collapse_stops_at_the_board_height_even_if_blocks_could_fall_farther()
    {
        var board = FloatingCap();
        var oneStep = FloatingCap();
        var stepped = oneStep.CollapseChains(1);
        Assert.Single(stepped);
        Assert.True(oneStep.NextChainRow() >= 0);

        var capped = board.CollapseChains(2);

        Assert.Equal(2, capped.Length);
        Assert.Equal(BlockType.Empty, board.Get(0, board.Height - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(0, board.Height - 3));
        Assert.Equal(BlockType.Empty, board.Get(0, 0));

        var rest = new Board();
        var unlimited = FloatingCap();
        var fallen = unlimited.CollapseChains();

        Assert.Equal(rest.Height - 1, fallen.Length);
        Assert.True(fallen.Length <= rest.Height);
        Assert.Equal(BlockType.SolidRock, unlimited.Get(0, 0));
        Assert.Equal(BlockType.Empty, unlimited.Get(0, 1));
        Assert.Empty(unlimited.CollapseChains());
    }

    [Fact]
    public void Default_collapse_limit_is_the_board_height()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "NegativeCarver.Core", "Board.cs"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var open = source.IndexOf("public int[] CollapseChains()", StringComparison.Ordinal);
        var limited = source.IndexOf("public int[] CollapseChains(int limit)", StringComparison.Ordinal);
        Assert.True(open >= 0);
        Assert.True(limited > open);
        var parameterless = source.Substring(open, limited - open);
        var body = source.Substring(limited);
        var nextMethod = body.IndexOf("private bool HasSolidAbove", StringComparison.Ordinal);
        Assert.True(nextMethod > 0);
        body = body.Substring(0, nextMethod);

        Assert.Contains("return CollapseChains(Height);", parameterless, StringComparison.Ordinal);
        Assert.Contains("for (var step = 0; step < limit; step++)", body, StringComparison.Ordinal);
        Assert.Contains("if (row < 0)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("while (true)", parameterless, StringComparison.Ordinal);
        Assert.DoesNotContain("while (true)", body, StringComparison.Ordinal);
        Assert.DoesNotContain("18", parameterless, StringComparison.Ordinal);
        Assert.DoesNotContain("18", body, StringComparison.Ordinal);
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

    private static Board FloatingCap()
    {
        var board = new Board();
        for (var y = 0; y < board.Height; y++)
        {
            ClearRow(board, y);
        }

        for (var x = 0; x < board.Width; x++)
        {
            Assert.True(board.TrySet(x, board.Height - 1, BlockType.SolidRock));
        }

        return board;
    }

    private static Board HoledBoard(int hole, int gemY)
    {
        var board = new Board();
        ClearRow(board, hole);
        Assert.True(board.TrySet(PilotRules.CoreX, gemY, BlockType.CoreGem));
        return board;
    }

    private static Board TwoHoles(int lower, int upper)
    {
        var board = new Board();
        ClearRow(board, lower);
        ClearRow(board, upper);
        Assert.True(board.TrySet(PilotRules.CoreX, upper + 1, BlockType.CoreGem));
        return board;
    }

    private static int CountGems(Board board)
    {
        var gems = 0;
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                if (board.Get(x, y) == BlockType.CoreGem)
                {
                    gems++;
                }
            }
        }

        return gems;
    }

    private static void ClearRow(Board board, int y)
    {
        for (var x = 0; x < board.Width; x++)
        {
            board.TryClear(x, y);
        }
    }

    private static BlockType[,] Snapshot(Board board)
    {
        var copy = new BlockType[board.Width, board.Height];
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                copy[x, y] = board.Get(x, y);
            }
        }

        return copy;
    }

    private static void AssertSame(Board board, BlockType[,] before)
    {
        for (var y = 0; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                Assert.Equal(before[x, y], board.Get(x, y));
            }
        }
    }
}
