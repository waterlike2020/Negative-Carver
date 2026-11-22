using NegativeCarver.Core;

namespace NegativeCarver.Core.Tests;

public sealed class CutterQueueTests
{
    [Fact]
    public void Each_shape_spawns_inside_the_board_with_its_bottom_on_the_spawn_row()
    {
        foreach (CutterShape shape in Enum.GetValues<CutterShape>())
        {
            var piece = CutterSpawns.AtTop(shape);
            Assert.Equal(shape, piece.Shape);
            Assert.Equal(0, piece.Rotation);
            Assert.True(CutterShapes.Fits(piece.OriginX, piece.OriginY, piece.Shape, piece.Rotation));

            var minX = piece.Cells[0].X;
            var maxX = piece.Cells[0].X;
            var minY = piece.Cells[0].Y;
            for (var i = 1; i < piece.Cells.Length; i++)
            {
                if (piece.Cells[i].X < minX)
                {
                    minX = piece.Cells[i].X;
                }

                if (piece.Cells[i].X > maxX)
                {
                    maxX = piece.Cells[i].X;
                }

                if (piece.Cells[i].Y < minY)
                {
                    minY = piece.Cells[i].Y;
                }
            }

            var span = maxX - minX + 1;
            Assert.Equal(PilotRules.CutterSpawnY, piece.OriginY + minY);
            Assert.Equal((PilotRules.Width - span) / 2, piece.OriginX + minX);
        }
    }

    [Fact]
    public void Queue_deals_every_shape_then_returns_to_the_first()
    {
        var queue = new CutterQueue(1);
        var first = Deal(queue);
        AssertBag(first);
        Assert.Equal(first[0], queue.Next().Shape);

        var again = new CutterQueue(1);
        Assert.Equal(first, Deal(again));

        queue.Reset();
        var second = Deal(queue);
        AssertBag(second);
        Assert.False(first.SequenceEqual(second));
    }

    private static CutterShape[] Deal(CutterQueue queue)
    {
        var dealt = new CutterShape[7];
        for (var i = 0; i < dealt.Length; i++)
        {
            dealt[i] = queue.Next().Shape;
        }

        return dealt;
    }

    private static void AssertBag(CutterShape[] dealt)
    {
        Assert.Equal(7, dealt.Distinct().Count());
        foreach (CutterShape shape in Enum.GetValues<CutterShape>())
        {
            Assert.Contains(shape, dealt);
        }
    }

    private static void AssertSpawn(CutterPiece piece, CutterShape shape)
    {
        Assert.Equal(shape, piece.Shape);
        Assert.Equal(0, piece.Rotation);
        Assert.True(CutterShapes.Fits(piece.OriginX, piece.OriginY, piece.Shape, piece.Rotation));
        Assert.Equal(PilotRules.CutterSpawnY, BottomY(piece));

        var minX = piece.Cells[0].X;
        var maxX = piece.Cells[0].X;
        for (var i = 1; i < piece.Cells.Length; i++)
        {
            if (piece.Cells[i].X < minX)
            {
                minX = piece.Cells[i].X;
            }

            if (piece.Cells[i].X > maxX)
            {
                maxX = piece.Cells[i].X;
            }
        }

        var span = maxX - minX + 1;
        Assert.Equal((PilotRules.Width - span) / 2, piece.OriginX + minX);
    }

    private static int BottomY(CutterPiece piece)
    {
        var minY = piece.Cells[0].Y;
        for (var i = 1; i < piece.Cells.Length; i++)
        {
            if (piece.Cells[i].Y < minY)
            {
                minY = piece.Cells[i].Y;
            }
        }

        return piece.OriginY + minY;
    }
}
