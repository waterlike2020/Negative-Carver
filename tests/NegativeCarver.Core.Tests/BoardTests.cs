using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class BoardTests
{
    [Fact]
    public void New_board_fills_only_the_bottom_rock_rows()
    {
        var board = new Board();

        Assert.Equal(PilotRules.Width, board.Width);
        Assert.Equal(PilotRules.Height, board.Height);
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "NegativeCarver.Core", "Board.cs"));
        Assert.Contains("cells[PilotRules.CoreX, PilotRules.CoreY] = BlockType.CoreGem;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("cells[4, 2]", source, StringComparison.Ordinal);

        for (var y = 0; y < board.Height; y++)
        {
            var expected = y < PilotRules.InitialRockRows ? BlockType.SolidRock : BlockType.Empty;
            for (var x = 0; x < board.Width; x++)
            {
                if (x == PilotRules.CoreX && y == PilotRules.CoreY)
                {
                    Assert.Equal(BlockType.CoreGem, board.Get(x, y));
                    continue;
                }

                Assert.Equal(expected, board.Get(x, y));
            }
        }
    }

    [Fact]
    public void Cell_center_is_the_middle_of_a_unit_cell()
    {
        var origin = BoardSpace.CellCenter(0, 0);
        var topRock = BoardSpace.CellCenter(PilotRules.Width - 1, PilotRules.InitialRockRows - 1);

        Assert.Equal(0.5f, origin.X);
        Assert.Equal(0.5f, origin.Y);
        Assert.Equal(9.5f, topRock.X);
        Assert.Equal(9.5f, topRock.Y);
    }

    [Fact]
    public void TryClear_empties_one_rock_and_leaves_its_neighbor()
    {
        var board = new Board();

        Assert.True(board.TryClear(0, 0));
        Assert.Equal(BlockType.Empty, board.Get(0, 0));
        Assert.Equal(BlockType.SolidRock, board.Get(1, 0));
        Assert.False(board.TryClear(0, 0));
        Assert.False(board.TryClear(-1, 0));
        Assert.False(board.TryClear(0, board.Height));
    }

    [Fact]
    public void TryClear_also_empties_a_core_gem()
    {
        var board = new Board();
        Assert.True(board.TrySet(PilotRules.CoreX, PilotRules.CoreY, BlockType.CoreGem));

        Assert.True(board.TryClear(PilotRules.CoreX, PilotRules.CoreY));

        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX + 1, PilotRules.CoreY));
    }

    [Fact]
    public void Horizontal_i_hard_drop_clears_only_its_four_top_rocks()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        piece.DropToFirstContact(board);
        var row = PilotRules.InitialRockRows - 1;

        var carved = board.CarveSolids(piece.OriginX, piece.OriginY, piece.Cells);

        Assert.Equal(piece.Cells.Length, carved.Count);
        for (var i = 0; i < piece.Cells.Length; i++)
        {
            var x = piece.OriginX + piece.Cells[i].X;
            var y = piece.OriginY + piece.Cells[i].Y;
            Assert.Equal(row, y);
            Assert.Equal(BlockType.Empty, board.Get(x, y));
            Assert.Contains(new CellOffset(x, y), carved);
        }

        for (var x = piece.Cells.Length; x < board.Width; x++)
        {
            Assert.Equal(BlockType.SolidRock, board.Get(x, row));
        }

        for (var y = 0; y < row; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                var expected = x == PilotRules.CoreX && y == PilotRules.CoreY
                    ? BlockType.CoreGem
                    : BlockType.SolidRock;
                Assert.Equal(expected, board.Get(x, y));
            }
        }
    }

    [Fact]
    public void Square_overlapping_a_core_gem_leaves_the_gem_and_clears_only_rocks()
    {
        var board = new Board();
        Assert.True(board.TrySet(PilotRules.CoreX, PilotRules.CoreY, BlockType.CoreGem));
        var piece = new CutterPiece(CutterShape.O, PilotRules.CoreX, PilotRules.CoreY);

        var carved = board.CarveSolids(piece.OriginX, piece.OriginY, piece.Cells);

        Assert.Equal(piece.Cells.Length - 1, carved.Count);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.DoesNotContain(new CellOffset(PilotRules.CoreX, PilotRules.CoreY), carved);
        foreach (var cell in piece.Cells)
        {
            var x = piece.OriginX + cell.X;
            var y = piece.OriginY + cell.Y;
            if (x == PilotRules.CoreX && y == PilotRules.CoreY)
            {
                continue;
            }

            Assert.Equal(BlockType.Empty, board.Get(x, y));
        }

        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX - 1, PilotRules.CoreY));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, PilotRules.CoreY - 1));
    }

    [Fact]
    public void Carve_skips_cells_outside_the_board_and_non_rock()
    {
        var board = new Board();
        Assert.True(board.TrySet(0, 0, BlockType.CutterPreview));
        Assert.True(board.TrySet(1, 0, BlockType.CoreGem));
        var pattern = new[]
        {
            new CellOffset(-1, 0),
            new CellOffset(0, 0),
            new CellOffset(1, 0),
            new CellOffset(2, 0),
            new CellOffset(0, board.Height),
        };

        var carved = board.CarveSolids(0, 0, pattern);

        Assert.Equal(new[] { new CellOffset(2, 0) }, carved);
        Assert.Equal(BlockType.CutterPreview, board.Get(0, 0));
        Assert.Equal(BlockType.CoreGem, board.Get(1, 0));
        Assert.Equal(BlockType.Empty, board.Get(2, 0));
        Assert.Equal(BlockType.SolidRock, board.Get(3, 0));
        Assert.Empty(board.CarveSolids(0, 0, null));
        Assert.Empty(board.CarveSolids(2, 0, new[] { new CellOffset(0, 0) }));
    }

    [Fact]
    public void CheckPenetration_is_true_only_when_every_cell_in_the_row_is_empty()
    {
        var board = new Board();
        var row = PilotRules.InitialRockRows - 1;
        var before = Snapshot(board);

        for (var y = 0; y < board.Height; y++)
        {
            Assert.Equal(y >= PilotRules.InitialRockRows, board.CheckPenetration(y));
        }

        Assert.False(board.CheckPenetration(-1));
        Assert.False(board.CheckPenetration(board.Height));
        AssertSame(board, before);

        ClearRow(board, row);
        for (var x = 0; x < board.Width; x++)
        {
            Assert.True(board.TrySet(x, row, BlockType.SolidRock));
            Assert.False(board.CheckPenetration(row));
            Assert.True(board.TrySet(x, row, BlockType.Empty));
        }

        Assert.True(board.TrySet(0, row, BlockType.CoreGem));
        Assert.False(board.CheckPenetration(row));
        Assert.True(board.TrySet(0, row, BlockType.Empty));
        Assert.True(board.TrySet(board.Width - 1, row, BlockType.CoreGem));
        Assert.False(board.CheckPenetration(row));
        Assert.True(board.TrySet(board.Width - 1, row, BlockType.Empty));
        Assert.True(board.TrySet(PilotRules.CoreX, row, BlockType.CutterPreview));
        Assert.False(board.CheckPenetration(row));
        Assert.True(board.TrySet(PilotRules.CoreX, row, BlockType.Empty));

        Assert.True(board.CheckPenetration(row));
        Assert.False(board.CheckPenetration(row - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(0, row - 1));
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
