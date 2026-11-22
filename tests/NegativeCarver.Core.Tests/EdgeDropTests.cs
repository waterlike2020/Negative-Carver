using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class EdgeDropTests
{
    public static IEnumerable<object[]> EveryPose()
    {
        foreach (CutterShape shape in Enum.GetValues<CutterShape>())
        {
            for (var turn = 0; turn < 4; turn++)
            {
                yield return new object[] { shape, turn };
            }
        }
    }

    [Fact]
    public void The_pilot_has_seven_cutters()
    {
        Assert.Equal(7, Enum.GetValues<CutterShape>().Length);
    }

    [Theory]
    [MemberData(nameof(EveryPose))]
    public void Left_edge_drop_stays_on_the_board(CutterShape shape, int rotation)
    {
        DropAt(shape, rotation, LeftOrigin, static piece => Assert.Equal(0, MinWorldX(piece)));
        var board = new Board();
        var piece = Spawn(shape, rotation, LeftOrigin(CutterShapes.Cells(shape, rotation)));
        var pushed = piece.TrySidestep(board, -1);
        Assert.False(pushed.Moved);
        Assert.False(pushed.Locked);
        Assert.Empty(pushed.Carved);
        Assert.False(piece.TryMove(-1));
        AssertInside(board, piece);
    }

    [Theory]
    [MemberData(nameof(EveryPose))]
    public void Right_edge_drop_stays_on_the_board(CutterShape shape, int rotation)
    {
        DropAt(shape, rotation, RightOrigin, static piece => Assert.Equal(PilotRules.Width - 1, MaxWorldX(piece)));
        var board = new Board();
        var piece = Spawn(shape, rotation, RightOrigin(CutterShapes.Cells(shape, rotation)));
        var pushed = piece.TrySidestep(board, 1);
        Assert.False(pushed.Moved);
        Assert.False(pushed.Locked);
        Assert.Empty(pushed.Carved);
        Assert.False(piece.TryMove(1));
        AssertInside(board, piece);
    }

    [Theory]
    [MemberData(nameof(EveryPose))]
    public void Drop_above_the_core_stays_on_the_board(CutterShape shape, int rotation)
    {
        var cells = CutterShapes.Cells(shape, rotation);
        DropAt(shape, rotation, _ => CoreOrigin(cells), static piece => Assert.True(CoversColumn(piece, PilotRules.CoreX)));
        var board = new Board();
        var piece = Spawn(shape, rotation, CoreOrigin(cells));
        Assert.True(CoversColumn(piece, PilotRules.CoreX));
        var left = piece.TrySidestep(board, -1);
        AssertInside(board, piece);
        Read(board, left.Carved);
        if (left.Moved)
        {
            piece.TrySidestep(board, 1);
        }

        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
    }

    private static void DropAt(CutterShape shape, int rotation, Func<CellOffset[], int> originX, Action<CutterPiece> assertPose)
    {
        var board = new Board();
        var cells = CutterShapes.Cells(shape, rotation);
        var piece = Spawn(shape, rotation, originX(cells));
        var spun = Spawn(shape, rotation, originX(cells));
        Assert.Equal(((rotation % 4) + 4) % 4, piece.Rotation);
        assertPose(piece);
        AssertInside(board, piece);
        spun.TryRotateClockwise(board);
        AssertInside(board, spun);

        var steps = 0;
        CutterTouch touch;
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
            else if (touch.Moved)
            {
                Assert.Equal(beforeY - 1, piece.OriginY);
            }
            else
            {
                Assert.Equal(beforeY, piece.OriginY);
            }

            assertPose(piece);
            AssertInside(board, piece);
            Read(board, touch.Carved);
        }
        while (!touch.Locked);

        board.CarveSolids(0, 0, touch.Carved);
        Assert.Equal(BlockType.CoreGem, board.Get(PilotRules.CoreX, PilotRules.CoreY));
    }

    private static CutterPiece Spawn(CutterShape shape, int rotation, int originX)
    {
        var cells = CutterShapes.Cells(shape, rotation);
        var minY = cells[0].Y;
        var maxY = cells[0].Y;
        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i].Y < minY)
            {
                minY = cells[i].Y;
            }

            if (cells[i].Y > maxY)
            {
                maxY = cells[i].Y;
            }
        }

        var originY = PilotRules.Height - 1 - maxY;
        if (originY + minY < 0)
        {
            originY = -minY;
        }

        Assert.True(CutterShapes.Fits(originX, originY, shape, rotation));
        return new CutterPiece(shape, originX, originY, rotation);
    }

    private static int LeftOrigin(CellOffset[] cells)
    {
        var minX = cells[0].X;
        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i].X < minX)
            {
                minX = cells[i].X;
            }
        }

        return -minX;
    }

    private static int RightOrigin(CellOffset[] cells)
    {
        var maxX = cells[0].X;
        for (var i = 1; i < cells.Length; i++)
        {
            if (cells[i].X > maxX)
            {
                maxX = cells[i].X;
            }
        }

        return PilotRules.Width - 1 - maxX;
    }

    private static int CoreOrigin(CellOffset[] cells)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            var originX = PilotRules.CoreX - cells[i].X;
            var minX = originX + cells[0].X;
            var maxX = originX + cells[0].X;
            for (var cell = 1; cell < cells.Length; cell++)
            {
                var x = originX + cells[cell].X;
                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }
            }

            if (minX >= 0 && maxX < PilotRules.Width)
            {
                return originX;
            }
        }

        throw new InvalidOperationException("No origin keeps this cutter above the core.");
    }

    private static int MinWorldX(CutterPiece piece)
    {
        var cells = piece.Cells;
        var minX = piece.OriginX + cells[0].X;
        for (var i = 1; i < cells.Length; i++)
        {
            var x = piece.OriginX + cells[i].X;
            if (x < minX)
            {
                minX = x;
            }
        }

        return minX;
    }

    private static int MaxWorldX(CutterPiece piece)
    {
        var cells = piece.Cells;
        var maxX = piece.OriginX + cells[0].X;
        for (var i = 1; i < cells.Length; i++)
        {
            var x = piece.OriginX + cells[i].X;
            if (x > maxX)
            {
                maxX = x;
            }
        }

        return maxX;
    }

    private static bool CoversColumn(CutterPiece piece, int column)
    {
        var cells = piece.Cells;
        for (var i = 0; i < cells.Length; i++)
        {
            if (piece.OriginX + cells[i].X == column)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertInside(Board board, CutterPiece piece)
    {
        var cells = piece.Cells;
        for (var i = 0; i < cells.Length; i++)
        {
            var x = piece.OriginX + cells[i].X;
            var y = piece.OriginY + cells[i].Y;
            Assert.True(board.Contains(x, y));
            board.Get(x, y);
        }
    }

    private static void Read(Board board, CellOffset[] cells)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            Assert.True(board.Contains(cells[i].X, cells[i].Y));
            board.Get(cells[i].X, cells[i].Y);
        }
    }
}
