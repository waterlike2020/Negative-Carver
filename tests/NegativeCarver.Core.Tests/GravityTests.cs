using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class GravityTests
{
    [Fact]
    public void Gravity_on_a_fresh_board_changes_nothing()
    {
        var board = new Board();
        var before = Snapshot(board);
        var rows = board.PenetratedRowsHighToLow();

        Assert.Equal(board.Height - PilotRules.InitialRockRows, rows.Length);
        Assert.Equal(board.Height - 1, rows[0]);
        Assert.Equal(PilotRules.InitialRockRows, rows[rows.Length - 1]);
        for (var i = 1; i < rows.Length; i++)
        {
            Assert.True(rows[i] < rows[i - 1]);
        }

        board.ApplyGravity();
        AssertSame(board, before);
        board.ShiftRowDown(-1);
        board.ShiftRowDown(board.Height);
        AssertSame(board, before);
    }

    [Fact]
    public void One_empty_row_drops_rocks_and_the_core_by_one_and_leaves_the_rest()
    {
        var board = new Board();
        var hole = PilotRules.CoreY + 1;
        Assert.True(hole < PilotRules.InitialRockRows - 1);
        ClearRow(board, hole);
        Assert.True(board.TrySet(PilotRules.CoreX, PilotRules.CoreY, BlockType.SolidRock));
        Assert.True(board.TrySet(PilotRules.CoreX, hole + 1, BlockType.CoreGem));
        var before = Snapshot(board);

        board.ApplyGravity();

        for (var y = 0; y < hole; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                Assert.Equal(before[x, y], board.Get(x, y));
            }
        }

        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, hole));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, hole + 1));
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX, PilotRules.InitialRockRows - 1));
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

        Assert.Equal(1, gems);
        for (var x = 0; x < board.Width; x++)
        {
            if (x == PilotRules.CoreX)
            {
                continue;
            }

            Assert.Equal(BlockType.SolidRock, board.Get(x, hole));
            Assert.Equal(BlockType.SolidRock, board.Get(x, PilotRules.InitialRockRows - 2));
            Assert.Equal(BlockType.Empty, board.Get(x, PilotRules.InitialRockRows - 1));
        }
    }

    [Fact]
    public void Two_empty_rows_drop_a_block_once_for_each_hole_below_it()
    {
        var board = new Board();
        var lower = PilotRules.CoreY;
        var upper = lower + 3;
        Assert.True(upper + 1 < PilotRules.InitialRockRows);
        ClearRow(board, lower);
        ClearRow(board, upper);
        Assert.True(board.TrySet(PilotRules.CoreX, upper + 1, BlockType.CoreGem));

        board.ApplyGravity();

        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, upper - 1));
        Assert.NotEqual(BlockType.CoreGem, board.Get(PilotRules.CoreX, upper + 1));
        for (var y = 0; y < lower; y++)
        {
            Assert.Equal(BlockType.SolidRock, board.Get(0, y));
        }

        for (var x = 0; x < board.Width; x++)
        {
            Assert.Equal(BlockType.SolidRock, board.Get(x, lower));
            Assert.Equal(BlockType.SolidRock, board.Get(x, lower + 1));
        }

        Assert.Equal(BlockType.SolidRock, board.Get(0, upper));
        Assert.Equal(BlockType.SolidRock, board.Get(0, PilotRules.InitialRockRows - 3));
        Assert.Equal(BlockType.Empty, board.Get(0, PilotRules.InitialRockRows - 2));
        Assert.Equal(BlockType.Empty, board.Get(0, PilotRules.InitialRockRows - 1));
    }

    [Fact]
    public void A_clear_hole_lets_the_core_fall_without_overlap()
    {
        var board = new Board();
        var hole = PilotRules.CoreY + 1;
        ClearRow(board, hole);
        Assert.True(board.TrySet(PilotRules.CoreX, hole + 1, BlockType.CoreGem));

        Assert.False(board.ShiftRowDown(hole));
        Assert.False(board.CoreOverlapDetected);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, hole));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, hole + 1));
    }

    [Fact]
    public void Core_falling_into_a_rock_is_an_overlap()
    {
        var board = new Board();
        var y = PilotRules.CoreY;
        Assert.True(board.TrySet(PilotRules.CoreX, y, BlockType.SolidRock));
        Assert.True(board.TrySet(PilotRules.CoreX, y + 1, BlockType.CoreGem));

        Assert.True(board.ShiftRowDown(y));
        Assert.True(board.CoreOverlapDetected);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, y));
    }

    [Fact]
    public void Core_falling_into_another_core_is_an_overlap()
    {
        var board = new Board();
        var y = PilotRules.CoreY;
        Assert.True(board.TrySet(PilotRules.CoreX, y, BlockType.CoreGem));
        Assert.True(board.TrySet(PilotRules.CoreX, y + 1, BlockType.CoreGem));

        Assert.True(board.ShiftRowDown(y));
        Assert.True(board.CoreOverlapDetected);
    }

    [Fact]
    public void A_rock_falling_onto_a_rock_is_not_a_core_overlap()
    {
        var board = new Board();

        Assert.False(board.ShiftRowDown(PilotRules.CoreY));
        Assert.False(board.CoreOverlapDetected);
    }

    [Fact]
    public void Collapse_of_a_real_hole_does_not_mark_overlap()
    {
        var board = new Board();
        ClearRow(board, PilotRules.CoreY);
        Assert.True(board.TrySet(PilotRules.CoreX, PilotRules.CoreY + 1, BlockType.CoreGem));

        board.CollapseChains();

        Assert.False(board.CoreOverlapDetected);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
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
