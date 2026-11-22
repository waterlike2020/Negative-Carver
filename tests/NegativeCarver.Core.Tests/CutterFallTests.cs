using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CutterFallTests
{
    [Fact]
    public void Descend_moves_one_empty_cell_and_does_not_carve()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.O, 0, PilotRules.CutterSpawnY);

        var touch = piece.TryDescend(board);

        Assert.True(touch.Moved);
        Assert.False(touch.Locked);
        Assert.Empty(touch.Carved);
        Assert.Equal(PilotRules.CutterSpawnY - 1, piece.OriginY);
        Assert.Equal(0, piece.OriginX);
    }

    [Fact]
    public void One_cell_steps_stop_on_the_same_top_face_as_a_straight_drop()
    {
        var straight = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        var walking = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        straight.DropToFirstContact(new Board());

        var touch = DescendUntilLock(walking, new Board());

        Assert.Equal(straight.OriginX, walking.OriginX);
        Assert.Equal(straight.OriginY, walking.OriginY);
        Assert.Equal(walking.Cells.Length, touch.Carved.Length);
        foreach (var cell in touch.Carved)
        {
            Assert.Equal(PilotRules.InitialRockRows - 1, cell.Y);
            Assert.NotEqual(PilotRules.CoreY - 1, cell.Y);
        }
    }

    [Fact]
    public void Falling_down_a_cleared_shaft_stops_on_the_core_and_leaves_the_rock_beneath()
    {
        var board = new Board();
        var column = PilotRules.CoreX;
        var under = PilotRules.CoreY - 1;
        for (var y = PilotRules.CoreY + 1; y < PilotRules.InitialRockRows; y++)
        {
            Assert.True(board.TryClear(column, y));
        }

        var piece = new CutterPiece(CutterShape.I, column, PilotRules.CutterSpawnY, 1);
        var touch = DescendUntilLock(piece, board);
        board.CarveSolids(0, 0, touch.Carved);

        Assert.False(touch.Moved);
        Assert.True(touch.Locked);
        Assert.DoesNotContain(new CellOffset(column, under), touch.Carved);
        Assert.DoesNotContain(new CellOffset(column, PilotRules.CoreY), touch.Carved);
        foreach (var cell in touch.Carved)
        {
            Assert.True(cell.Y > PilotRules.CoreY);
        }

        Assert.Equal(BlockType.SolidRock, board.Get(column, under));
        Assert.Equal(BlockType.CoreGem, board.Get(column, PilotRules.CoreY));
        var lowest = piece.OriginY;
        foreach (var cell in piece.Cells)
        {
            if (piece.OriginY + cell.Y < lowest)
            {
                lowest = piece.OriginY + cell.Y;
            }
        }

        Assert.Equal(PilotRules.CoreY + 1, lowest);
    }

    [Fact]
    public void Sidestep_carves_the_rock_beside_the_core_when_that_side_touches_first()
    {
        var board = new Board();
        var y = PilotRules.CoreY - 1;
        var piece = new CutterPiece(CutterShape.I, PilotRules.CoreX + 1, y);
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, y));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));

        Assert.True(board.TryClear(PilotRules.CoreX + 1, y));
        Assert.True(board.TryClear(PilotRules.CoreX + 2, y));
        Assert.True(board.TryClear(PilotRules.CoreX + 3, y));

        var touch = piece.TrySidestep(board, -1);
        board.CarveSolids(0, 0, touch.Carved);

        Assert.True(touch.Moved);
        Assert.True(touch.Locked);
        Assert.Equal(new[] { new CellOffset(PilotRules.CoreX, y) }, touch.Carved);
        Assert.DoesNotContain(new CellOffset(PilotRules.CoreX, PilotRules.CoreY), touch.Carved);
        Assert.Equal(BlockType.Empty, board.Get(PilotRules.CoreX, y));
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
        Assert.Equal(BlockType.SolidRock, board.Get(PilotRules.CoreX, y - 1));
        Assert.Equal(PilotRules.CoreX, piece.OriginX);
    }

    [Fact]
    public void Sidestep_into_the_core_does_not_move_or_carve()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.O, PilotRules.CoreX + 1, PilotRules.CoreY);
        var before = board.CopyCells();

        var touch = piece.TrySidestep(board, -1);

        Assert.False(touch.Moved);
        Assert.False(touch.Locked);
        Assert.Empty(touch.Carved);
        Assert.Equal(PilotRules.CoreX + 1, piece.OriginX);
        Assert.Equal(PilotRules.CoreY, piece.OriginY);
        Assert.Equal(before, board.CopyCells());
    }

    [Fact]
    public void Descend_blocked_by_the_core_carves_the_touched_rocks_only()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.O, PilotRules.CoreX, PilotRules.CoreY + 1);

        var touch = piece.TryDescend(board);

        Assert.False(touch.Moved);
        Assert.True(touch.Locked);
        Assert.Equal(PilotRules.CoreY + 1, piece.OriginY);
        Assert.Contains(new CellOffset(PilotRules.CoreX + 1, PilotRules.CoreY), touch.Carved);
        Assert.DoesNotContain(new CellOffset(PilotRules.CoreX, PilotRules.CoreY), touch.Carved);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
    }

    [Fact]
    public void Sidestep_through_empty_air_does_not_lock()
    {
        var board = new Board();
        var piece = new CutterPiece(CutterShape.O, 0, PilotRules.CutterSpawnY);

        var touch = piece.TrySidestep(board, 1);

        Assert.True(touch.Moved);
        Assert.False(touch.Locked);
        Assert.Empty(touch.Carved);
        Assert.Equal(1, piece.OriginX);
    }

    [Fact]
    public void Rotation_into_rock_stays_put_and_open_air_still_turns()
    {
        var board = new Board();
        var buried = new CutterPiece(CutterShape.I, 0, PilotRules.InitialRockRows - 1);
        Assert.False(buried.TryRotateClockwise(board));
        Assert.Equal(0, buried.Rotation);
        Assert.Equal(PilotRules.InitialRockRows - 1, buried.OriginY);
        Assert.Equal(BlockType.SolidRock, board.Get(0, PilotRules.InitialRockRows - 1));

        var open = new CutterPiece(CutterShape.I, 0, PilotRules.CutterSpawnY);
        Assert.True(open.TryRotateClockwise(board));
        Assert.Equal(1, open.Rotation);
    }

    private static CutterTouch DescendUntilLock(CutterPiece piece, Board board)
    {
        CutterTouch touch;
        var steps = 0;
        do
        {
            var beforeY = piece.OriginY;
            touch = piece.TryDescend(board);
            steps++;
            Assert.True(steps <= PilotRules.Height);
            if (!touch.Locked)
            {
                Assert.Equal(beforeY - 1, piece.OriginY);
                Assert.Empty(touch.Carved);
            }
        }
        while (!touch.Locked);

        return touch;
    }
}
