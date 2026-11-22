using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CutterPieceTests
{
    [Fact]
    public void Every_shape_has_four_cells_and_returns_after_four_rotations()
    {
        foreach (CutterShape shape in Enum.GetValues<CutterShape>())
        {
            var start = CutterShapes.Cells(shape, 0);
            Assert.Equal(4, start.Distinct().Count());

            var piece = new CutterPiece(shape, 4, 8);
            for (var turn = 0; turn < 4; turn++)
            {
                Assert.True(piece.TryRotateClockwise());
            }

            Assert.Equal(0, piece.Rotation);
            Assert.Equal(start.OrderBy(static cell => cell.X).ThenBy(static cell => cell.Y),
                piece.Cells.OrderBy(static cell => cell.X).ThenBy(static cell => cell.Y));
        }
    }

    [Fact]
    public void Square_keeps_its_cells_when_rotated_in_the_corner()
    {
        var piece = new CutterPiece(CutterShape.O, 0, 0);
        var before = piece.Cells;

        Assert.True(piece.TryRotateClockwise());
        Assert.Equal(before, piece.Cells);
    }

    [Fact]
    public void Rotation_kicks_back_inside_so_each_click_keeps_turning()
    {
        var onTheFloor = new CutterPiece(CutterShape.I, 0, 0);
        Assert.True(onTheFloor.TryRotateClockwise());
        Assert.Equal(1, onTheFloor.Rotation);
        Assert.True(onTheFloor.OriginY > 0);
        AssertCellsInside(onTheFloor);

        var atRightEdge = new CutterPiece(CutterShape.I, PilotRules.Width - 1, 0, rotation: 3);
        Assert.True(atRightEdge.TryRotateClockwise());
        Assert.Equal(0, atRightEdge.Rotation);
        Assert.True(atRightEdge.OriginX < PilotRules.Width - 1);
        AssertCellsInside(atRightEdge);

        foreach (CutterShape shape in Enum.GetValues<CutterShape>())
        {
            var spawned = CutterSpawns.AtTop(shape);
            for (var turn = 0; turn < 8; turn++)
            {
                Assert.True(spawned.TryRotateClockwise());
                Assert.Equal((turn + 1) % 4, spawned.Rotation);
                AssertCellsInside(spawned);
            }
        }
    }

    [Fact]
    public void Horizontal_move_stays_inside_the_board_and_ignores_rocks()
    {
        var onRocks = new CutterPiece(CutterShape.O, 0, 0);
        Assert.False(onRocks.TryMove(-1));
        Assert.Equal(0, onRocks.OriginX);
        Assert.True(onRocks.TryMove(1));
        Assert.Equal(1, onRocks.OriginX);

        var atRight = new CutterPiece(CutterShape.I, PilotRules.Width - 4, 3);
        Assert.False(atRight.TryMove(1));
        Assert.Equal(PilotRules.Width - 4, atRight.OriginX);
        Assert.True(atRight.TryMove(-1));
    }

    [Fact]
    public void Horizontal_i_drops_onto_the_top_rock_row_and_leaves_the_board_unchanged()
    {
        var board = new Board();
        var before = Snapshot(board);
        var piece = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        var originX = piece.OriginX;
        var minY = MinCellY(piece.Cells);

        piece.DropToFirstContact(board);

        Assert.Equal(PilotRules.InitialRockRows - 1 - minY, piece.OriginY);
        Assert.Equal(originX, piece.OriginX);
        Assert.Equal(0, piece.Rotation);
        foreach (var cell in piece.Cells)
        {
            Assert.Equal(PilotRules.InitialRockRows - 1, piece.OriginY + cell.Y);
        }

        AssertSame(board, before);
    }

    [Fact]
    public void Vertical_i_contacts_rock_with_only_its_lowest_cell()
    {
        var board = new Board();
        var before = Snapshot(board);
        var piece = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        var originX = piece.OriginX;
        Assert.True(piece.TryRotateClockwise());

        var minY = MinCellY(piece.Cells);
        Assert.True(minY < 0);
        Assert.Equal(1, CountCellsAtY(piece.Cells, minY));

        piece.DropToFirstContact(board);

        Assert.Equal(originX, piece.OriginX);
        Assert.Equal(1, piece.Rotation);
        var rockOverlaps = 0;
        var above = 0;
        foreach (var cell in piece.Cells)
        {
            var worldX = piece.OriginX + cell.X;
            var worldY = piece.OriginY + cell.Y;
            if (cell.Y == minY)
            {
                Assert.Equal(PilotRules.InitialRockRows - 1, worldY);
                Assert.Equal(BlockType.SolidRock, board.Get(worldX, worldY));
                rockOverlaps++;
                continue;
            }

            Assert.True(worldY > PilotRules.InitialRockRows - 1);
            Assert.NotEqual(BlockType.SolidRock, board.Get(worldX, worldY));
            Assert.NotEqual(BlockType.CoreGem, board.Get(worldX, worldY));
            above++;
        }

        Assert.Equal(1, rockOverlaps);
        Assert.Equal(piece.Cells.Length - 1, above);
        AssertSame(board, before);
    }

    [Fact]
    public void Horizontal_i_stops_at_the_higher_rock_when_one_column_is_lower()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        var originX = piece.OriginX;
        var minY = MinCellY(piece.Cells);
        var loweredX = originX;
        var highRock = PilotRules.InitialRockRows - 1;
        Assert.True(board.TryClear(loweredX, highRock));
        Assert.Equal(BlockType.Empty, board.Get(loweredX, highRock));
        Assert.Equal(BlockType.SolidRock, board.Get(loweredX, highRock - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(loweredX + 1, highRock));
        var before = Snapshot(board);

        piece.DropToFirstContact(board);

        Assert.Equal(highRock - minY, piece.OriginY);
        Assert.Equal(originX, piece.OriginX);
        Assert.Equal(0, piece.Rotation);
        foreach (var cell in piece.Cells)
        {
            Assert.Equal(highRock, piece.OriginY + cell.Y);
        }

        AssertSame(board, before);
    }

    [Fact]
    public void S_stops_on_the_rock_under_a_column_that_is_not_the_global_bottom()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.S, 0, PilotRules.CutterSpawnY);
        var originX = piece.OriginX;
        var minY = MinCellY(piece.Cells);
        var bottomColumn = new bool[board.Width];
        foreach (var cell in piece.Cells)
        {
            if (cell.Y == minY)
            {
                bottomColumn[piece.OriginX + cell.X] = true;
            }
        }

        var keptRockColumn = -1;
        foreach (var cell in piece.Cells)
        {
            var x = piece.OriginX + cell.X;
            if (!bottomColumn[x])
            {
                keptRockColumn = x;
            }
        }

        Assert.True(keptRockColumn >= 0);
        Assert.False(bottomColumn[keptRockColumn]);

        for (var x = 0; x < board.Width; x++)
        {
            if (!bottomColumn[x])
            {
                continue;
            }

            for (var y = 0; y < board.Height; y++)
            {
                board.TryClear(x, y);
            }
        }

        Assert.Equal(BlockType.SolidRock, board.Get(keptRockColumn, PilotRules.InitialRockRows - 1));
        var before = Snapshot(board);

        piece.DropToFirstContact(board);

        var rockY = PilotRules.InitialRockRows - 1;
        var raisedCellY = 0;
        foreach (var cell in piece.Cells)
        {
            if (piece.OriginX + cell.X == keptRockColumn)
            {
                raisedCellY = cell.Y;
            }
        }

        Assert.Equal(rockY - raisedCellY, piece.OriginY);
        Assert.Equal(originX, piece.OriginX);
        Assert.Equal(0, piece.Rotation);
        Assert.True(raisedCellY > minY);
        foreach (var cell in piece.Cells)
        {
            var worldY = piece.OriginY + cell.Y;
            if (piece.OriginX + cell.X == keptRockColumn)
            {
                Assert.Equal(rockY, worldY);
                continue;
            }

            Assert.Equal(BlockType.Empty, board.Get(piece.OriginX + cell.X, worldY));
        }

        Assert.Equal(BlockType.SolidRock, board.Get(keptRockColumn, rockY));
        AssertSame(board, before);
    }

    [Fact]
    public void Rotated_s_stops_when_the_higher_column_meets_rock_and_carves_only_that_cell()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.S, 3, PilotRules.CutterSpawnY, 1);
        var leftBottomY = 0;
        var rightBottomY = 0;
        var leftX = 0;
        var rightX = 0;
        foreach (var cell in piece.Cells)
        {
            var columnBottom = true;
            foreach (var other in piece.Cells)
            {
                if (other.X == cell.X && other.Y < cell.Y)
                {
                    columnBottom = false;
                }
            }

            if (!columnBottom)
            {
                continue;
            }

            if (leftX == 0 && rightX == 0)
            {
                leftX = piece.OriginX + cell.X;
                leftBottomY = cell.Y;
            }
            else
            {
                rightX = piece.OriginX + cell.X;
                rightBottomY = cell.Y;
            }
        }

        if (leftBottomY < rightBottomY)
        {
            var swapX = leftX;
            var swapY = leftBottomY;
            leftX = rightX;
            leftBottomY = rightBottomY;
            rightX = swapX;
            rightBottomY = swapY;
        }

        Assert.True(leftBottomY > rightBottomY);
        var redRow = PilotRules.InitialRockRows - 1;
        Assert.Equal(BlockType.SolidRock, board.Get(leftX, redRow));
        Assert.Equal(BlockType.SolidRock, board.Get(leftX, redRow - 1));
        Assert.True(board.TryClear(rightX, redRow));
        Assert.True(board.TryClear(rightX, redRow - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(rightX, redRow - 2));

        piece.DropToFirstContact(board);

        Assert.Equal(redRow - leftBottomY, piece.OriginY);
        var overlaps = 0;
        foreach (var cell in piece.Cells)
        {
            var x = piece.OriginX + cell.X;
            var y = piece.OriginY + cell.Y;
            if (board.Get(x, y) == BlockType.SolidRock)
            {
                Assert.Equal(leftX, x);
                Assert.Equal(redRow, y);
                overlaps++;
            }
        }

        Assert.Equal(1, overlaps);
        var carved = board.CarveSolids(piece.OriginX, piece.OriginY, piece.Cells);
        Assert.Equal(new[] { new CellOffset(leftX, redRow) }, carved);
        Assert.Equal(BlockType.SolidRock, board.Get(leftX, redRow - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(rightX, redRow - 2));
    }

    [Fact]
    public void Vertical_i_stops_on_a_core_gem_above_the_rock_rows()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        var originX = piece.OriginX;
        Assert.True(piece.TryRotateClockwise());
        var minY = MinCellY(piece.Cells);
        var column = piece.OriginX + ColumnOfLowest(piece.Cells, minY);
        var gemY = PilotRules.InitialRockRows;

        for (var y = 0; y < board.Height; y++)
        {
            board.TryClear(column, y);
            Assert.Equal(BlockType.Empty, board.Get(column, y));
        }

        Assert.False(board.TrySet(-1, gemY, BlockType.CoreGem));
        Assert.False(board.TrySet(column, board.Height, BlockType.CoreGem));
        Assert.True(board.TrySet(column, gemY, BlockType.CoreGem));
        Assert.True(gemY > PilotRules.InitialRockRows - 1);
        Assert.Equal(BlockType.Empty, board.Get(column, gemY - 1));
        Assert.Equal(BlockType.SolidRock, board.Get(column + 1, PilotRules.InitialRockRows - 1));
        var before = Snapshot(board);

        piece.DropToFirstContact(board);

        var bottomY = piece.OriginY + minY;
        Assert.Equal(gemY, bottomY);
        Assert.True(bottomY > PilotRules.InitialRockRows - 1);
        Assert.Equal(BlockType.CoreGem, board.Get(column, bottomY));
        Assert.Equal(originX, piece.OriginX);
        Assert.Equal(1, piece.Rotation);
        foreach (var cell in piece.Cells)
        {
            if (cell.Y == minY)
            {
                continue;
            }

            Assert.True(piece.OriginY + cell.Y > gemY);
        }

        AssertSame(board, before);
    }

    [Fact]
    public void Horizontal_i_already_inside_rock_does_not_move_up()
    {
        var board = new Board();
        var buriedY = PilotRules.CoreY;
        Assert.True(buriedY < PilotRules.InitialRockRows);
        Assert.Equal(BlockType.SolidRock, board.Get(0, buriedY));
        var before = Snapshot(board);
        var piece = new CutterPiece(CutterShape.I, 0, buriedY);

        piece.DropToFirstContact(board);

        Assert.Equal(buriedY, piece.OriginY);
        Assert.Equal(0, piece.OriginX);
        Assert.Equal(0, piece.Rotation);
        AssertSame(board, before);
    }

    private static int MinCellY(CellOffset[] cells)
    {
        var minY = cells[0].Y;
        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i].Y < minY)
            {
                minY = cells[i].Y;
            }
        }

        return minY;
    }

    private static int CountCellsAtY(CellOffset[] cells, int y)
    {
        var count = 0;
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i].Y == y)
            {
                count++;
            }
        }

        return count;
    }

    private static int ColumnOfLowest(CellOffset[] cells, int minY)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i].Y == minY)
            {
                return cells[i].X;
            }
        }

        return cells[0].X;
    }

    private static void AssertCellsInside(CutterPiece piece)
    {
        foreach (var cell in piece.Cells)
        {
            var x = piece.OriginX + cell.X;
            var y = piece.OriginY + cell.Y;
            Assert.InRange(x, 0, PilotRules.Width - 1);
            Assert.InRange(y, 0, PilotRules.Height - 1);
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
